using System.Collections.Generic;
using System.Diagnostics;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Midi.Clock;
using TemperaMental.Midi.Input;
using TemperaMental.Midi.Output;
using TemperaMental.Midi.Playbacks;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Recording
{
    // Records what's played on the Tempera into frames as timed events (main thread).
    //
    // Record arms; the trigger starts the recording: the transport starting (Play, or the clock source's MIDI Start),
    // the first placement on the Tempera, either of those, or straight away. The sequencer runs the recording
    // (RecordingSession): it starts playback or punches in, and ends it. This class turns the Tempera's decoded notes
    // into frame events: the clock converts each note's arrival time to a transport tick, which gives its frame and its
    // tick in that frame.
    //
    // Replace: frames are rebuilt as the playhead reaches them, each starting from what the Tempera shows at that beat,
    //   then what's played during it. Frames past the end are added. A Replace recording begun from stopped starts from
    //   the starting state chosen in the Record settings (applied when armed).
    // Overdub: the frames play as usual, and placements and removals played are added to the frame playing at the
    //   time (colour selects aren't: playback sends its own).
    // In both, a note that changes nothing (an echo of what the app sent, or a repeat) isn't recorded.
    //
    // With Source set to Transforms, the Record button toggles FrameManager's recording of transform results instead.
    [DefaultExecutionOrder(-10)] // before the sequencer's Update, so a recorded frame exists when playback moves to it
    public class MidiRecorder : MonoBehaviour
    {
        // pending notes the clock can't place yet are dropped after this long (e.g. following a clock that has stopped)
        const double PendingTimeoutSeconds = 2;

        [SerializeField] FrameSequenceManager sequencer;
        [SerializeField] FrameManager frameManager;
        [SerializeField] ClockEngine clockEngine;
        [SerializeField] MidiOutputScheduler outputScheduler;

        [SerializeField] UnityEvent<RecordState> onRecordStateChanged = new UnityEvent<RecordState>();

        // what the Tempera shows while recording, for the grid
        [SerializeField] UnityEvent<FrameDetail> onRecordingEmittersChanged = new UnityEvent<FrameDetail>();

        enum Outcome { Recorded, Ignored, NotReady }

        sealed class RecordedFrame
        {
            public ulong[] Start;
            public readonly List<FrameEvent> Events = new List<FrameEvent>();
            public bool IsDirty = true;
        }

        int ticksPerBeat;
        int emitterCount;
        long pendingTimeoutTicks;

        RecordSource source;
        RecordTrigger trigger;
        RecordMode mode;
        RecordStartState startState;
        int lengthBeats;

        RecordState state;

        // armed: what the Tempera showed when armed, and what's been played on it since
        ulong[] armedStart;
        readonly List<TemperaControlEvent> armedEvents = new List<TemperaControlEvent>();

        // recording
        RecordingSession session;
        int lastSessionId;
        ulong[] initialGroups;
        readonly List<TemperaControlEvent> pending = new List<TemperaControlEvent>();
        readonly List<RecordedFrame> recorded = new List<RecordedFrame>();
        int lastSelected;
        int recordedCount;

        // what to show on the grid after recording something
        bool isViewChanged;
        int viewFrame;
        ulong[] viewGroups;

        private void Awake()
        {
            ticksPerBeat = ConfigRegistry.Midi.TicksPerBeat;
            emitterCount = ConfigRegistry.Grid.EmitterCount;
            pendingTimeoutTicks = (long)(Stopwatch.Frequency * PendingTimeoutSeconds);
        }

        // --- settings (Record settings view) ---

        public void SetSource(RecordSource newSource)
        {
            if (source == newSource) return;

            // the button now does the other kind of recording
            if (newSource == RecordSource.Transforms) StopTempera();
            else frameManager.StopTransformRecording();

            source = newSource;
        }

        // the others apply from the next time Record is armed
        public void SetTrigger(RecordTrigger newTrigger) => trigger = newTrigger;

        public void SetMode(RecordMode newMode) => mode = newMode;

        public void SetStartState(RecordStartState newStartState) => startState = newStartState;

        public void SetLength(int beats) => lengthBeats = Mathf.Max(0, beats);

        public RecordState State => state;

        // --- Record button ---

        public void ToggleRecord()
        {
            if (source == RecordSource.Transforms)
            {
                // transform results are only recorded while stopped
                if (sequencer.State == PlaybackState.Playing)
                {
                    LogMan.LogTemp("Stop playback to record transforms");
                    return;
                }

                frameManager.ToggleRecording();
                return;
            }

            switch (state)
            {
                case RecordState.Idle:
                    Arm();
                    break;

                case RecordState.Armed:
                    Disarm();
                    break;

                case RecordState.Recording:
                    sequencer.StopRecording();
                    break;
            }
        }

        private void StopTempera()
        {
            if (state == RecordState.Armed) Disarm();
            else if (state == RecordState.Recording) sequencer.StopRecording();
        }

        private void Arm()
        {
            bool isPlaying = sequencer.State == PlaybackState.Playing;
            FrameSnapshot current = frameManager.GetCurrentFrameSnapshot();

            // the starting state is for a Replace recording begun from stopped; otherwise playback decides what's shown
            if (mode == RecordMode.Replace && !isPlaying)
            {
                switch (startState)
                {
                    case RecordStartState.Clear:
                        outputScheduler.EnqueueClearAll();
                        armedStart = new ulong[emitterCount];
                        break;

                    case RecordStartState.Send:
                        outputScheduler.EnqueueClearAll();
                        outputScheduler.EnqueueFrame(current.StartGroups);
                        armedStart = (ulong[])current.StartGroups.Clone();
                        break;

                    default:
                        armedStart = outputScheduler.GetDeviceGroups();
                        break;
                }
            }
            else
            {
                armedStart = outputScheduler.GetDeviceGroups();
            }

            armedEvents.Clear();

            if (trigger == RecordTrigger.Now)
            {
                StartAt(Stopwatch.GetTimestamp());
                return;
            }

            if (trigger == RecordTrigger.Start || trigger == RecordTrigger.Either)
                sequencer.ArmRecording(mode, lengthBeats);

            SetState(RecordState.Armed);
            LogMan.Log($"Recording armed ({trigger}, {mode})");
        }

        private void Disarm()
        {
            sequencer.DisarmRecording();
            armedEvents.Clear();

            SetState(RecordState.Idle);
            LogMan.Log("Recording disarmed");
        }

        // --- notes from the Tempera (after the device record and Follow Tempera have seen them) ---

        public void ActionOnTemperaControlEvent(TemperaControlEvent controlEvent)
        {
            if (controlEvent.Kind == TemperaControlKind.ArmTrack) return;

            if (state == RecordState.Armed)
            {
                // a placement that changes what the Tempera shows is a touch; selecting a colour isn't
                bool isTouch = (trigger == RecordTrigger.Touch || trigger == RecordTrigger.Either) &&
                               controlEvent.Kind == TemperaControlKind.PlaceEmitter &&
                               IsChange(Replay(armedStart, armedEvents), ToFrameEvent(controlEvent, 0));

                armedEvents.Add(controlEvent);

                if (isTouch) StartAt(controlEvent.Timestamp);
            }
            else if (state == RecordState.Recording)
            {
                pending.Add(controlEvent);
                ProcessPending();
                FlushFrames();
                RaiseView();
            }
        }

        // a touch, or Now: the sequencer starts playback (or punches in) with the recording beginning at this moment
        private void StartAt(long timestamp)
        {
            RecordingSession started = sequencer.StartRecordingAt(mode, lengthBeats, timestamp, frameManager.GetCurrentFrameNumber());

            BeginSession(started);
        }

        private void Update()
        {
            RecordingSession published = sequencer.RecordingSession;

            // armed for the transport's start, and it has started
            if (state == RecordState.Armed && published != null && published.Id > lastSessionId)
            {
                BeginSession(published);
            }

            if (state != RecordState.Recording || session == null) return;

            if (published != null && published.Id == session.Id && published.IsEnded)
                session = published;

            ProcessPending();

            if (session.Mode == RecordMode.Replace)
                AdvanceReplaceFrames();

            FlushFrames();
            RaiseView();

            if (session.IsEnded) FinishSession();
        }

        private void BeginSession(RecordingSession started)
        {
            session = started;
            lastSessionId = started.Id;
            lastSelected = -1;
            recordedCount = 0;
            recorded.Clear();
            pending.Clear();

            // what was played on the Tempera before the trigger is where the recording starts from; the trigger and
            // anything after it are recorded
            var before = new List<TemperaControlEvent>();

            foreach (TemperaControlEvent armedEvent in armedEvents)
            {
                if (armedEvent.Timestamp < started.StartTimestamp) before.Add(armedEvent);
                else pending.Add(armedEvent);
            }

            initialGroups = Replay(armedStart ?? new ulong[emitterCount], before);
            armedEvents.Clear();

            if (started.Mode == RecordMode.Replace) StartReplace();

            SetState(RecordState.Recording);
            LogMan.Log("Recording");

            ProcessPending();
            FlushFrames();
            RaiseView();
        }

        // the first frame: punched in, it keeps what it held before the punch-in; otherwise it starts from what the
        // Tempera showed at the trigger
        private void StartReplace()
        {
            var first = new RecordedFrame();
            FrameSnapshot original = frameManager.GetFrameSnapshot(session.StartFrame);

            if (session.IsPunchIn && original != null)
            {
                first.Start = (ulong[])original.StartGroups.Clone();

                foreach (FrameEvent frameEvent in original.Events)
                {
                    if (frameEvent.Tick < session.StartTick) first.Events.Add(frameEvent);
                }
            }
            else
            {
                first.Start = initialGroups;
            }

            recorded.Add(first);
            ShowLiveState();
        }

        private void FinishSession()
        {
            int frameCount = recorded.Count;
            RecordMode finishedMode = session.Mode;

            session = null;
            pending.Clear();
            recorded.Clear();

            SetState(RecordState.Idle);

            LogMan.Log(finishedMode == RecordMode.Replace
                ? $"Recorded {frameCount} frames, {recordedCount} events"
                : $"Overdubbed {recordedCount} events");
        }

        // --- placing notes ---

        private void ProcessPending()
        {
            if (session == null) return;

            long now = Stopwatch.GetTimestamp();

            // in arrival order; once one can't be placed yet, neither can the ones after it
            while (pending.Count > 0)
            {
                TemperaControlEvent controlEvent = pending[0];
                Outcome outcome = Record(controlEvent);

                if (outcome == Outcome.NotReady && now - controlEvent.Timestamp < pendingTimeoutTicks) break;

                pending.RemoveAt(0);
            }
        }

        private Outcome Record(TemperaControlEvent controlEvent)
        {
            if (!clockEngine.TryGetTickAt(controlEvent.Timestamp, out long epoch, out long tick)) return Outcome.NotReady;

            // the clock hasn't reached the recording's epoch yet (following clock, it starts on a pulse)
            if (epoch < session.Epoch) return Outcome.NotReady;

            // a later epoch only comes after the recording has ended
            if (epoch > session.Epoch) return Outcome.Ignored;

            // between the trigger and the clock's first step it belongs at the start
            long start = session.StartPosition(ticksPerBeat);
            if (tick < start) tick = start;

            if (tick >= session.EndTick) return Outcome.Ignored;

            long beat = tick / ticksPerBeat;
            int tickInFrame = (int)(tick - beat * ticksPerBeat);
            FrameEvent frameEvent = ToFrameEvent(controlEvent, tickInFrame);

            return session.Mode == RecordMode.Replace
                ? RecordReplace((int)(beat - session.StartBeat), frameEvent)
                : RecordOverdub(epoch, beat, frameEvent);
        }

        private Outcome RecordReplace(int frameIndex, FrameEvent frameEvent)
        {
            if (frameIndex < 0) return Outcome.Ignored;
            if (session.LengthBeats > 0 && frameIndex >= session.LengthBeats) return Outcome.Ignored;

            EnsureFrames(frameIndex);
            RecordedFrame frame = recorded[frameIndex];

            if (!IsNew(frameEvent, StateAt(frame.Start, frame.Events, frameEvent.Tick))) return Outcome.Ignored;

            Insert(frame.Events, frameEvent);
            frame.IsDirty = true;

            // a note that arrived after playback moved on: the frames after it start from where it leaves things
            for (int i = frameIndex + 1; i < recorded.Count; i++)
            {
                recorded[i].Start = EndOf(recorded[i - 1]);
                recorded[i].IsDirty = true;
            }

            recordedCount++;
            ShowLiveState();

            return Outcome.Recorded;
        }

        private Outcome RecordOverdub(long epoch, long beat, FrameEvent frameEvent)
        {
            // playback sends a colour select before every placement, which the Tempera may echo; placements carry their
            // own colour, so selects aren't overdubbed
            if (frameEvent.Type == FrameEventType.Select) return Outcome.Ignored;

            if (!sequencer.TryGetFrameAtBeat(epoch, beat, out int frameNumber)) return Outcome.NotReady;

            if (session.LengthBeats > 0 && beat - session.StartBeat >= session.LengthBeats) return Outcome.Ignored;

            FrameSnapshot frame = frameManager.GetFrameSnapshot(frameNumber);

            if (frame == null) return Outcome.Ignored;

            var events = new List<FrameEvent>(frame.Events);

            if (!IsNew(frameEvent, StateAt(frame.StartGroups, events, frameEvent.Tick))) return Outcome.Ignored;

            frameManager.AddRecordedEvent(frameNumber, frameEvent);

            Insert(events, frameEvent);
            recordedCount++;

            ShowView(frameNumber, StateAt(frame.StartGroups, events, frameEvent.Tick));

            return Outcome.Recorded;
        }

        // a colour select that's already selected, a placement already there or a removal of nothing changes nothing:
        // a repeat, or the Tempera echoing what the app sent
        private bool IsNew(FrameEvent frameEvent, ulong[] groupsAtTick)
        {
            if (frameEvent.Type != FrameEventType.Select) return IsChange(groupsAtTick, frameEvent);

            if (frameEvent.EmitterId == lastSelected) return false;

            lastSelected = frameEvent.EmitterId;
            return true;
        }

        private static bool IsChange(ulong[] groups, FrameEvent frameEvent)
        {
            ulong bit = 1UL << frameEvent.Cell;

            switch (frameEvent.Type)
            {
                case FrameEventType.Place:
                    return (groups[frameEvent.EmitterId] & bit) == 0;

                case FrameEventType.Remove:
                    for (int i = 0; i < groups.Length; i++)
                    {
                        if ((groups[i] & bit) != 0) return true;
                    }
                    return false;

                default:
                    return true;
            }
        }

        // adds frames, each starting from where the one before ends, as the playhead reaches them
        private void AdvanceReplaceFrames()
        {
            long limit;

            if (session.IsEnded)
            {
                if (session.EndTick == long.MaxValue) return;
                limit = session.EndTick - 1;
            }
            else
            {
                if (!clockEngine.TryGetTickAt(Stopwatch.GetTimestamp(), out long epoch, out long tick) || epoch != session.Epoch) return;
                limit = tick;
            }

            int frameIndex = (int)(limit / ticksPerBeat - session.StartBeat);

            if (session.LengthBeats > 0) frameIndex = Mathf.Min(frameIndex, session.LengthBeats - 1);

            if (frameIndex >= recorded.Count)
            {
                EnsureFrames(frameIndex);
                ShowLiveState();
            }
        }

        private void EnsureFrames(int frameIndex)
        {
            while (recorded.Count <= frameIndex)
            {
                recorded.Add(new RecordedFrame { Start = EndOf(recorded[recorded.Count - 1]) });
            }
        }

        private void FlushFrames()
        {
            if (session == null || session.Mode != RecordMode.Replace) return;

            bool isAnyWritten = false;

            for (int i = 0; i < recorded.Count; i++)
            {
                RecordedFrame frame = recorded[i];

                if (!frame.IsDirty) continue;

                frameManager.SetRecordedFrame(session.StartFrame + i, new FrameSnapshot((ulong[])frame.Start.Clone(), frame.Events.ToArray()));
                frame.IsDirty = false;
                isAnyWritten = true;
            }

            // playback may have moved past the last frame before this one was added
            int playFrame = sequencer.PlayFrame;

            if (isAnyWritten && playFrame == session.StartFrame + recorded.Count - 1 &&
                frameManager.GetCurrentFrameNumber() != playFrame && sequencer.State == PlaybackState.Playing)
            {
                frameManager.GoToPlaybackFrame(playFrame);
            }
        }

        // Replace: the grid shows the end of the newest frame, which is what the Tempera shows
        private void ShowLiveState()
        {
            if (recorded.Count == 0) return;

            ShowView(session.StartFrame + recorded.Count - 1, EndOf(recorded[recorded.Count - 1]));
        }

        private void ShowView(int frameNumber, ulong[] groups)
        {
            viewFrame = frameNumber;
            viewGroups = groups;
            isViewChanged = true;
        }

        // after any frames have been written, which redraw the grid with the frame's start
        private void RaiseView()
        {
            if (!isViewChanged) return;

            isViewChanged = false;

            if (frameManager.GetCurrentFrameNumber() != viewFrame) return;

            onRecordingEmittersChanged?.Invoke(new FrameDetail(viewFrame, frameManager.GetFrames().Count, viewGroups));
        }

        // --- helpers ---

        private FrameEvent ToFrameEvent(TemperaControlEvent controlEvent, int tick)
        {
            return controlEvent.Kind switch
            {
                TemperaControlKind.SelectEmitter => FrameEvent.Select(tick, controlEvent.EmitterId),
                TemperaControlKind.PlaceEmitter => FrameEvent.Place(tick, controlEvent.EmitterId, controlEvent.Cell),
                _ => FrameEvent.Remove(tick, controlEvent.Cell)
            };
        }

        private ulong[] Replay(ulong[] start, List<TemperaControlEvent> events)
        {
            ulong[] groups = (ulong[])start.Clone();

            foreach (TemperaControlEvent controlEvent in events)
            {
                ToFrameEvent(controlEvent, 0).ApplyTo(groups);
            }

            return groups;
        }

        private static ulong[] StateAt(ulong[] start, List<FrameEvent> events, int tick)
        {
            ulong[] groups = (ulong[])start.Clone();

            foreach (FrameEvent frameEvent in events)
            {
                if (frameEvent.Tick > tick) break;
                frameEvent.ApplyTo(groups);
            }

            return groups;
        }

        private static ulong[] EndOf(RecordedFrame frame) => StateAt(frame.Start, frame.Events, int.MaxValue);

        // after any events already on the same tick
        private static void Insert(List<FrameEvent> events, FrameEvent frameEvent)
        {
            int index = events.Count;

            while (index > 0 && events[index - 1].Tick > frameEvent.Tick) index--;

            events.Insert(index, frameEvent);
        }

        private void SetState(RecordState newState)
        {
            if (state == newState) return;

            state = newState;
            frameManager.SetTemperaRecordState(state);

            onRecordStateChanged?.Invoke(state);
        }
    }
}
