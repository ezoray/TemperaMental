using System;
using System.Diagnostics;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Midi.Clock;
using TemperaMental.Midi.Core;
using TemperaMental.Midi.Output;
using TemperaMental.Recording;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace TemperaMental.Midi.Playbacks
{
    // Sequencer: plays one frame per beat of the ClockEngine.
    // Play, resume and seek restart the clock so the chosen frame is sent straight away.
    // Each frame's start groups are sent on its beat, then its events at their ticks: the sequencer asks the clock for
    // a step at the next event's tick. Reverse plays the frames backwards, but each frame's events forwards.
    // Note events are played on their channels. At each frame's start the notes that should be sounding there (from the
    // frames before it) are compared with those sounding, and corrected, so starting part way through, seeking, looping
    // and reverse leave the right notes on; pausing and stopping release them. A note being held live on the Tempera
    // isn't turned on again (e.g. while overdubbing it), and a Note Off is only sent for a note the sequencer turned on,
    // so a note played live is never cut.
    // HandleBeat and HandleTick run on the clock thread. State shared with the main thread is guarded by
    // playbackStateLock, and UI notifications are raised from Update using volatile flags.
    //
    // Recording (driven by MidiRecorder): a recording belongs to one playback epoch. It starts on the transport's
    // start when armed for it, or at a given moment (punching in when already playing, otherwise starting playback with
    // the clock's tick 0 at that moment). While a Replace recording runs nothing is sent to the device and playback
    // moves forward one frame per beat past the end of the frames, ignoring loop and reverse. A recording ends when
    // asked, after its length, when playback stops or pauses, or when the epoch changes (seek, Song Position).
    public class FrameSequenceManager : MonoBehaviour
    {
        [SerializeField] FrameManager frameManager;
        [SerializeField, FormerlySerializedAs("playbackManager")] MidiOutputScheduler outputScheduler;
        [SerializeField] MidiImmediateManager immediateManager;
        [SerializeField] ClockEngine clockEngine;

        volatile bool isNewPlayFrame;
        volatile bool isPlaybackFinished;
        volatile bool isPlaybackStateChanged;
        volatile bool isLooped;
        volatile bool isReversed;

        int anchorFrame;
        volatile int playFrame;

        // epoch of the clock restart that began the current playback; beats from other epochs are ignored
        long playEpoch = -1;

        // the frame being played and how far through its events it is (clock thread, under playbackStateLock)
        FrameSnapshot playingFrame;
        long playingFrameTick;
        int nextEventIndex;
        ulong[] playingGroups;

        // what the grid should show once a frame's events have played, posted to Update
        sealed class PlaybackView
        {
            public readonly int FrameNumber;
            public readonly ulong[] Groups;

            public PlaybackView(int frameNumber, ulong[] groups)
            {
                FrameNumber = frameNumber;
                Groups = groups;
            }
        }

        volatile PlaybackView pendingView;
        PlaybackView raisedView;

        // recording, under playbackStateLock
        bool isArmedForStart;
        RecordMode armedMode;
        int armedLength;
        bool isRecording;
        RecordMode recordMode;
        long recordStartBeat;
        int recordLength;
        int lastSessionId;
        volatile RecordingSession recordingSession;

        // notes the sequencer has turned on (channel * 128 + note), under playbackStateLock
        const int NoteSlots = 16 * 128;
        readonly ulong[] soundingNotes = new ulong[NoteSlots / 64];

        // notes being held on the Tempera right now, from the recorder; replaced, never changed in place
        volatile ulong[] liveNotes = new ulong[NoteSlots / 64];

        // the notes that should be sounding at the start of frame heldIndex (0-based) of heldSequence, worked out from
        // the frames before it; playing forward only adds one frame's notes each beat
        readonly ulong[] heldNotes = new ulong[NoteSlots / 64];
        readonly byte[] heldVelocities = new byte[NoteSlots];
        FrameSnapshot[] heldSequence;
        int heldIndex;

        // which frame each recent beat played, so the recorder can place what arrives slightly late
        const int BeatHistorySize = 64;
        readonly object historyLock = new object();
        readonly long[] historyEpochs = new long[BeatHistorySize];
        readonly long[] historyBeats = new long[BeatHistorySize];
        readonly int[] historyFrames = new int[BeatHistorySize];

        volatile PlaybackState playbackState;

        readonly object playbackStateLock = new object();

        [SerializeField] UnityEvent<int> onPlaybackFrameChanged;
        [SerializeField] UnityEvent<PlaybackState> onPlaybackStateChanged;
        [SerializeField] UnityEvent<bool> onLoopStateChanged;
        [SerializeField] UnityEvent<bool> onReverseStateChanged;

        // the emitters shown as a frame's events play (the frame's start is shown through onPlaybackFrameChanged)
        [SerializeField] UnityEvent<FrameDetail> onPlaybackEmittersChanged = new UnityEvent<FrameDetail>();


        private void Awake()
        {
            playbackState = PlaybackState.Reset;

            for (int i = 0; i < BeatHistorySize; i++) historyEpochs[i] = -1;
        }

        private void OnEnable()
        {
            clockEngine.OnBeat += HandleBeat;
            clockEngine.OnTick += HandleTick;
            clockEngine.OnExternalTransport += HandleExternalTransport;
        }

        private void Start()
        {
            onLoopStateChanged?.Invoke(isLooped);
            onReverseStateChanged?.Invoke(isReversed);
            onPlaybackStateChanged?.Invoke(playbackState);
        }

        private void Update()
        {
            if (isNewPlayFrame)
            {
                isNewPlayFrame = false;
                onPlaybackFrameChanged?.Invoke(playFrame);
            }

            // after the frame change, so the events are drawn over the frame's start; a view left over from an earlier
            // frame, or arriving once playback has stopped, is dropped
            PlaybackView view = pendingView;

            if (view != null && view != raisedView)
            {
                raisedView = view;

                if (playbackState == PlaybackState.Playing && view.FrameNumber == playFrame)
                {
                    onPlaybackEmittersChanged?.Invoke(new FrameDetail(view.FrameNumber, FrameCount, view.Groups));
                }
            }

            if (isPlaybackStateChanged)
            {
                LogMan.Log($"Playback {playbackState}");

                isPlaybackStateChanged = false;
                onPlaybackStateChanged?.Invoke(playbackState);
            }

            if (isPlaybackFinished)
            {
                isPlaybackFinished = false;
                LogMan.Log("Playback Finished");
            }
        }

        public void TogglePlayPause(int initialFrame)
        {
            lock (playbackStateLock)
            {
                switch (playbackState)
                {
                    case PlaybackState.Playing:
                        SetPlaybackState(PlaybackState.Paused);
                        clockEngine.SendTransport(ClockTransport.Stop);
                        break;

                    case PlaybackState.Paused:
                        StartPlayback(ClockTransport.Continue);
                        break;

                    case PlaybackState.Reset:
                    case PlaybackState.Stopped:
                        SetAnchorFrame(initialFrame);
                        SetPlayFrame(initialFrame);
                        StartPlayback(ClockTransport.Start);

                        if (isArmedForStart)
                            BeginRecording(armedMode, armedLength, 0, anchorFrame, 0, Stopwatch.GetTimestamp(), false);
                        break;
                }
            }
        }

        public void Stop()
        {
            lock (playbackStateLock)
            {
                switch (playbackState)
                {
                    case PlaybackState.Playing:
                        SetPlaybackState(PlaybackState.Stopped);
                        clockEngine.SendTransport(ClockTransport.Stop);
                        break;

                    case PlaybackState.Paused:
                    case PlaybackState.Stopped:
                        SetPlayFrame(anchorFrame);
                        SetPlaybackState(PlaybackState.Reset);
                        break;

                    case PlaybackState.Reset:
                        if (playFrame != anchorFrame)
                        {
                            SetPlayFrame(anchorFrame);
                            SetPlaybackState(PlaybackState.Reset);
                        }
                        break;
                }
            }
        }

        public void SeekToFrame(int newSeekFrame)
        {
            lock (playbackStateLock)
            {
                switch (playbackState)
                {
                    case PlaybackState.Playing:
                        // restart the clock so the new frame plays immediately for a full beat;
                        // Start realigns the receiving device's beat with the restart
                        EndRecording(TickNow());
                        SetPlayFrame(newSeekFrame);
                        playEpoch = clockEngine.Restart(ClockTransport.Start);
                        break;

                    case PlaybackState.Paused:
                    case PlaybackState.Stopped:
                    case PlaybackState.Reset:
                        SetAnchorFrame(newSeekFrame);
                        SetPlayFrame(newSeekFrame);
                        break;
                }
            }
        }

        public void ToggleReverse()
        {
            lock (playbackStateLock)
            {
                isReversed = !isReversed;

                SetAnchorFrame(playFrame);
            }

            onReverseStateChanged?.Invoke(isReversed);
        }

        public void ToggleLooping()
        {
            isLooped = !isLooped;

            onLoopStateChanged?.Invoke(isLooped);
        }

        // clock thread
        private void HandleBeat(ClockBeat beat)
        {
            lock (playbackStateLock)
            {
                if (playbackState != PlaybackState.Playing || beat.Epoch != playEpoch) return;

                // beat 0 is the restart that started (or resumed) playback: play the current frame
                if (beat.Beat == 0)
                {
                    PlayCurrentFrame(0);
                }
                else
                {
                    // events the clock couldn't step to before the beat (a pulse late when following clock) still play,
                    // before the next frame
                    PlayDueEvents(long.MaxValue);

                    if (isRecording && recordLength > 0 && beat.Beat - recordStartBeat >= recordLength)
                        EndRecording(beat.Beat * clockEngine.TicksPerBeat);

                    AdvancePlayback(beat.Beat * clockEngine.TicksPerBeat);
                }
            }
        }

        // clock thread
        private void HandleTick(ClockTick tick)
        {
            lock (playbackStateLock)
            {
                if (playbackState != PlaybackState.Playing || tick.Epoch != playEpoch) return;

                PlayDueEvents(tick.Tick - playingFrameTick);
            }
        }

        // clock thread: transport from the clock source while following external clock.
        // Start plays from the current frame (or restarts from the anchor if already playing), Stop pauses,
        // Continue resumes, Song Position moves to the frame for that beat. The clock is already running,
        // so nothing here restarts it; the epoch says which beats belong to this playback
        private void HandleExternalTransport(ClockTransport transport, long epoch, int songBeat)
        {
            lock (playbackStateLock)
            {
                switch (transport)
                {
                    case ClockTransport.Start:
                        EndRecording(TickNow());

                        int startFrame = playbackState == PlaybackState.Playing ? anchorFrame : frameManager.GetCurrentFrameNumber();
                        SetAnchorFrame(startFrame);
                        SetPlayFrame(startFrame);
                        SetPlaybackState(PlaybackState.Playing);
                        playEpoch = epoch;

                        // the first pulse after Start is frame 1's tick 0
                        if (isArmedForStart)
                            BeginRecording(armedMode, armedLength, 0, anchorFrame, 0, Stopwatch.GetTimestamp(), false);
                        break;

                    case ClockTransport.Continue:
                        // carries on from the sequencer's position: where it paused, or where a Song Position put it
                        if (playbackState != PlaybackState.Playing && playbackState != PlaybackState.Paused)
                        {
                            SetAnchorFrame(playFrame);
                        }

                        SetPlaybackState(PlaybackState.Playing);
                        playEpoch = epoch;
                        break;

                    case ClockTransport.Stop:
                        if (playbackState == PlaybackState.Playing)
                            SetPlaybackState(PlaybackState.Paused);
                        break;

                    case ClockTransport.SongPosition:
                        EndRecording(TickNow());

                        if (playbackState != PlaybackState.Playing)
                            SetAnchorFrame(songBeat + 1);

                        SetPlayFrame(songBeat + 1);
                        playEpoch = epoch;
                        break;
                }
            }
        }

        private void StartPlayback(ClockTransport transport)
        {
            SetPlaybackState(PlaybackState.Playing);
            playEpoch = clockEngine.Restart(transport);
        }

        // --- recording ---

        // the published recording: a new instance when one starts and when it ends; null before the first
        public RecordingSession RecordingSession => recordingSession;

        // the next start of the transport (Play from stopped, or the clock source's Start) begins a recording
        public void ArmRecording(RecordMode mode, int lengthBeats)
        {
            lock (playbackStateLock)
            {
                isArmedForStart = true;
                armedMode = mode;
                armedLength = lengthBeats;
            }
        }

        public void DisarmRecording()
        {
            lock (playbackStateLock)
            {
                isArmedForStart = false;
            }
        }

        // starts recording at a moment (a touch on the Tempera, or now). When playing it punches in at that moment;
        // otherwise playback starts from the given frame, with the clock's tick 0 at that moment
        public RecordingSession StartRecordingAt(RecordMode mode, int lengthBeats, long timestamp, int currentFrame)
        {
            lock (playbackStateLock)
            {
                isArmedForStart = false;
                EndRecording(TickNow());

                if (playbackState == PlaybackState.Playing)
                {
                    int ticksPerBeat = clockEngine.TicksPerBeat;

                    // the beat the moment falls in, or the start of the current beat if the clock can't say
                    if (!clockEngine.TryGetTickAt(timestamp, out long epoch, out long tick) || epoch != playEpoch || tick < 0)
                        tick = LastBeat * ticksPerBeat;

                    long beat = tick / ticksPerBeat;
                    int frame = TryGetFrameAtBeat(playEpoch, beat, out int beatFrame) ? beatFrame : playFrame;

                    BeginRecording(mode, lengthBeats, beat, frame, (int)(tick - beat * ticksPerBeat), timestamp, true);
                }
                else
                {
                    SetAnchorFrame(currentFrame);
                    SetPlayFrame(currentFrame);
                    SetPlaybackState(PlaybackState.Playing);
                    playEpoch = clockEngine.Restart(ClockTransport.Start, timestamp);

                    BeginRecording(mode, lengthBeats, 0, anchorFrame, 0, timestamp, false);
                }

                return recordingSession;
            }
        }

        public void StopRecording()
        {
            lock (playbackStateLock)
            {
                EndRecording(TickNow());
            }
        }

        // the frame a recent beat of an epoch played
        public bool TryGetFrameAtBeat(long epoch, long beat, out int frame)
        {
            lock (historyLock)
            {
                int index = (int)(beat % BeatHistorySize);
                bool isKnown = historyEpochs[index] == epoch && historyBeats[index] == beat;

                frame = isKnown ? historyFrames[index] : 0;
                return isKnown;
            }
        }

        public int PlayFrame => playFrame;

        private bool IsRecordingReplace => isRecording && recordMode == RecordMode.Replace;

        // the latest beat played in this playback's epoch
        private long LastBeat => playingFrameTick / Math.Max(1, clockEngine.TicksPerBeat);

        private long TickNow()
        {
            return clockEngine.TryGetTickAt(Stopwatch.GetTimestamp(), out long epoch, out long tick) && epoch == playEpoch
                ? tick
                : long.MaxValue;
        }

        private void BeginRecording(RecordMode mode, int lengthBeats, long startBeat, int startFrame, int startTick, long timestamp,
            bool isPunchIn)
        {
            isArmedForStart = false;
            isRecording = true;
            recordMode = mode;
            recordStartBeat = startBeat;
            recordLength = lengthBeats;

            recordingSession = new RecordingSession(++lastSessionId, mode, playEpoch, startBeat, startFrame, startTick,
                timestamp, isPunchIn, lengthBeats);

            // the Tempera is played live while a Replace recording runs
            if (mode == RecordMode.Replace) ReleaseNotes();
        }

        private void EndRecording(long endTick)
        {
            if (!isRecording) return;

            isRecording = false;
            recordingSession = recordingSession.Ended(endTick);
        }

        private void RememberBeat(long beat, int frame)
        {
            lock (historyLock)
            {
                int index = (int)(beat % BeatHistorySize);
                historyEpochs[index] = playEpoch;
                historyBeats[index] = beat;
                historyFrames[index] = frame;
            }
        }

        private void FinishPlayback()
        {
            SetPlaybackState(PlaybackState.Reset);
            clockEngine.SendTransport(ClockTransport.Stop);
            isPlaybackFinished = true;
        }

        // read on the clock thread by the transform manager, so it's always current
        public PlaybackState State => playbackState;

        // sends the frame's start groups, then any events on its first tick, and asks the clock for the next event.
        // A Replace recording sends nothing: the frame is being recorded from the Tempera
        private void PlayCurrentFrame(long frameTick)
        {
            playingFrameTick = frameTick;
            nextEventIndex = 0;
            RememberBeat(frameTick / clockEngine.TicksPerBeat, playFrame);

            if (IsRecordingReplace)
            {
                playingFrame = null;
                return;
            }

            FrameSnapshot[] sequence = frameManager.PublishedSequence;
            int index = Mathf.Clamp(playFrame, 1, sequence.Length) - 1;
            FrameSnapshot frame = sequence[index];

            outputScheduler.EnqueueFrame(frame.StartGroups);
            CorrectSoundingNotes(sequence, index);

            playingFrame = frame;

            if (!frame.HasEvents) return;

            playingGroups = (ulong[])frame.StartGroups.Clone();
            PlayDueEvents(0);
        }

        // plays the current frame's events up to this tick into its beat
        private void PlayDueEvents(long tickInFrame)
        {
            // a Replace recording punched in part way through this frame: the rest of it is recorded instead
            if (playingFrame == null || IsRecordingReplace) return;

            FrameEvent[] events = playingFrame.Events;
            int firstIndex = nextEventIndex;

            while (nextEventIndex < events.Length && events[nextEventIndex].Tick <= tickInFrame)
            {
                FrameEvent frameEvent = events[nextEventIndex++];

                SendEvent(frameEvent);
                frameEvent.ApplyTo(playingGroups);
            }

            if (nextEventIndex > firstIndex)
            {
                pendingView = new PlaybackView(playFrame, (ulong[])playingGroups.Clone());
            }

            if (nextEventIndex < events.Length)
            {
                clockEngine.ScheduleTick(playingFrameTick + events[nextEventIndex].Tick);
            }
        }

        private void SendEvent(FrameEvent frameEvent)
        {
            switch (frameEvent.Type)
            {
                case FrameEventType.Select:
                    outputScheduler.EnqueueSelectEmitter(frameEvent.EmitterId);
                    break;

                case FrameEventType.Place:
                    outputScheduler.EnqueueAddEmitter(frameEvent.EmitterId, frameEvent.Cell);
                    break;

                case FrameEventType.Remove:
                    outputScheduler.EnqueueRemoveEmitter(frameEvent.Cell);
                    break;

                case FrameEventType.ClearAll:
                    outputScheduler.EnqueueClearAll();
                    break;

                case FrameEventType.NoteOn:
                    if (IsBitSet(liveNotes, NoteSlot(frameEvent))) break;
                    outputScheduler.EnqueueNoteOn(frameEvent.Channel, frameEvent.Note, frameEvent.Velocity);
                    SetBit(soundingNotes, NoteSlot(frameEvent), true);
                    break;

                case FrameEventType.NoteOff:
                    if (!IsBitSet(soundingNotes, NoteSlot(frameEvent))) break;
                    outputScheduler.EnqueueNoteOff(frameEvent.Channel, frameEvent.Note);
                    SetBit(soundingNotes, NoteSlot(frameEvent), false);
                    break;
            }
        }

        // turns off the notes that shouldn't be sounding at the start of this frame and turns on those that should
        private void CorrectSoundingNotes(FrameSnapshot[] sequence, int index)
        {
            if (!ReferenceEquals(sequence, heldSequence) || index < heldIndex)
            {
                Array.Clear(heldNotes, 0, heldNotes.Length);
                heldSequence = sequence;
                heldIndex = 0;
            }

            for (; heldIndex < index; heldIndex++)
            {
                foreach (FrameEvent frameEvent in sequence[heldIndex].Events)
                {
                    if (!frameEvent.IsNote) continue;

                    int slot = NoteSlot(frameEvent);
                    bool isOn = frameEvent.Type == FrameEventType.NoteOn;

                    SetBit(heldNotes, slot, isOn);
                    if (isOn) heldVelocities[slot] = (byte)frameEvent.Velocity;
                }
            }

            ulong[] live = liveNotes;

            for (int word = 0; word < soundingNotes.Length; word++)
            {
                ulong toStop = soundingNotes[word] & ~heldNotes[word];
                ulong toStart = heldNotes[word] & ~soundingNotes[word] & ~live[word];
                ulong started = toStart;

                for (int bit = 0; (toStop | toStart) != 0 && bit < 64; bit++)
                {
                    ulong mask = 1UL << bit;
                    int slot = word * 64 + bit;

                    if ((toStop & mask) != 0) outputScheduler.EnqueueNoteOff(slot >> 7, slot & 127);
                    else if ((toStart & mask) != 0) outputScheduler.EnqueueNoteOn(slot >> 7, slot & 127, heldVelocities[slot]);

                    toStop &= ~mask;
                    toStart &= ~mask;
                }

                soundingNotes[word] = (soundingNotes[word] & heldNotes[word]) | started;
            }
        }

        // any thread: the notes held on the Tempera now (channel * 128 + note bits); the array mustn't change afterwards
        public void SetLiveNotes(ulong[] notes)
        {
            liveNotes = notes;
        }

        private static bool IsBitSet(ulong[] bits, int slot) => (bits[slot >> 6] & (1UL << (slot & 63))) != 0;

        // turns off every note the sequencer turned on
        private void ReleaseNotes()
        {
            for (int word = 0; word < soundingNotes.Length; word++)
            {
                ulong sounding = soundingNotes[word];

                for (int bit = 0; sounding != 0 && bit < 64; bit++)
                {
                    if ((sounding & (1UL << bit)) == 0) continue;

                    int slot = word * 64 + bit;
                    outputScheduler.EnqueueNoteOff(slot >> 7, slot & 127);
                    sounding &= ~(1UL << bit);
                }

                soundingNotes[word] = 0;
            }
        }

        private static int NoteSlot(FrameEvent frameEvent) => (frameEvent.Channel & 15) * 128 + (frameEvent.Note & 127);

        private static void SetBit(ulong[] bits, int slot, bool isSet)
        {
            if (isSet) bits[slot >> 6] |= 1UL << (slot & 63);
            else bits[slot >> 6] &= ~(1UL << (slot & 63));
        }

        private int FrameCount => frameManager.PublishedSequence.Length;

        private void SetAnchorFrame(int frame)
        {
            anchorFrame = Mathf.Clamp(frame, 1, FrameCount);
        }

        // a Replace recording can run past the last frame; the recorder adds the frames
        private void SetPlayFrame(int frame, bool isPastEndAllowed = false)
        {
            playFrame = isPastEndAllowed ? Math.Max(1, frame) : Mathf.Clamp(frame, 1, FrameCount);
            isNewPlayFrame = true;
        }

        private void SetPlaybackState(PlaybackState state)
        {
            if (playbackState != state)
            {
                playbackState = state;
                immediateManager.SetPlaybackState(state);

                // a frame's remaining events don't carry over a pause or stop, nor do notes or a recording
                if (state != PlaybackState.Playing)
                {
                    playingFrame = null;
                    ReleaseNotes();
                    EndRecording(TickNow());
                }

                isPlaybackStateChanged = true;
            }
        }

        private void AdvancePlayback(long frameTick)
        {
            if (IsRecordingReplace)
            {
                SetPlayFrame(playFrame + 1, isPastEndAllowed: true);
                PlayCurrentFrame(frameTick);
                return;
            }

            int frameCount = FrameCount;
            int nextFrame = playFrame + (isReversed ? -1 : 1);

            bool reachedEnd = isReversed ? nextFrame < 1 : nextFrame > frameCount;

            if (reachedEnd)
            {
                if (isLooped)
                {
                    // check anchor frame isn't the same as final frame, prevents looping over single frame
                    // no emitter data would be sent anyway and static playback could be confusing
                    if (frameCount == 1 || (isReversed ? anchorFrame == 1 : anchorFrame == frameCount))
                    {
                        FinishPlayback();
                        return;
                    }
                    SetPlayFrame(anchorFrame);
                }
                else
                {
                    FinishPlayback();
                    return;
                }
            }
            else
            {
                SetPlayFrame(nextFrame);
            }

            PlayCurrentFrame(frameTick);
        }

        private void OnDisable()
        {
            clockEngine.OnBeat -= HandleBeat;
            clockEngine.OnTick -= HandleTick;
            clockEngine.OnExternalTransport -= HandleExternalTransport;
        }
    }
}
