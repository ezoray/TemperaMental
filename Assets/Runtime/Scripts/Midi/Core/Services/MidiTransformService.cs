using System.Collections.Generic;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Utils;
using UnityEngine;

namespace TemperaMental.Midi.Transforms
{
    // Converts frames to and from MIDI files.
    //
    // Format 2 (written, as a single-track file): TicksPerBeat (96) ticks per quarter note, one frame per quarter note.
    // A format marker at time 0, then for each frame, all at the frame's time: its START_FRAME_NO_n marker, a clear of
    // all emitters and the frame's start groups (CC10 colour, CC11 cells), so every frame is absolute and seekable. If
    // the frame has events, a FRAME_EVENTS marker follows, then each event at its tick: Select = CC10,
    // Place = CC10 + CC11, Remove = CC12 cell, ClearAll = CC12 64, and notes as Note On / Note Off on their channels.
    // A Program Change at time 0 recalls the Tempera canvas. A DAW playing the file therefore drives the Tempera as
    // the app would.
    // Reading relies on the order of events on the same tick. If the markers and CCs are in different tracks (a DAW
    // may save it that way), that order is lost, so the CCs on a frame's beat are all read as its start groups: events
    // on the frame's first tick are folded into them.
    //
    // Format 1 (older files, or any file without the format marker): read as before, grouping CCs into frames by
    // quarter note and applying placements only.
    public class MidiTransformService : MonoBehaviour
    {
        const int MICROSECONDS_PER_MINUTE = 60_000_000;

        short ticksPerFrame;
        int activateCC;
        int placeCC;
        int removeCC;
        byte clearEmittersValue;

        string frameStartPrefix;
        string seqEndMarker;
        string formatMarker;
        string eventsMarker;

        int gridWidth;
        int gridHeight;
        int gridSize;
        int emitterCount;

        private void OnEnable()
        {
            frameStartPrefix = ConfigRegistry.Midi.FrameStartPrefix;
            seqEndMarker = ConfigRegistry.Midi.SeqEndMarker;
            formatMarker = ConfigRegistry.Midi.FileFormatMarker;
            eventsMarker = ConfigRegistry.Midi.FrameEventsMarker;

            ticksPerFrame = (short)ConfigRegistry.Midi.TicksPerBeat;

            activateCC = ConfigRegistry.Midi.ActivateCC;
            placeCC = ConfigRegistry.Midi.PlaceCC;
            removeCC = ConfigRegistry.Midi.RemoveCC;
            clearEmittersValue = ConfigRegistry.Midi.ClearEmittersValue;

            emitterCount = ConfigRegistry.Grid.EmitterCount;

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;
            gridSize = gridWidth * gridHeight;
        }

        // --- writing (format 2) ---

        // canvas: Program Change 0–127 at the start (the Tempera loads that canvas), or -1 for none
        public MidiFile FromFramesToMidiFile(IReadOnlyList<Frame> sourceFrames, int bpm, int canvas = -1)
        {
            MidiFile midiFile = BuildMidiFile();
            TrackChunk trackChunk = BuildTrackChunk(bpm);

            var events = new List<TimedEvent> { new TimedEvent(new MarkerEvent(formatMarker), 0) };

            if (canvas >= 0 && canvas <= 127)
                events.Add(new TimedEvent(new ProgramChangeEvent((SevenBitNumber)canvas), 0));

            WriteFrames(events, sourceFrames);

            // the events are built in time order; adding them with delta times keeps the order of events on the same
            // tick, which the reader relies on
            long previousTime = 0;

            foreach (TimedEvent timedEvent in events)
            {
                MidiEvent midiEvent = timedEvent.Event;
                midiEvent.DeltaTime = timedEvent.Time - previousTime;
                previousTime = timedEvent.Time;

                trackChunk.Events.Add(midiEvent);
            }

            midiFile.Chunks.Add(trackChunk);

            return midiFile;
        }

        private MidiFile BuildMidiFile()
        {
            var midiFile = new MidiFile();
            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision(ticksPerFrame);

            return midiFile;
        }

        private TrackChunk BuildTrackChunk(int bpm)
        {
            var trackChunk = new TrackChunk();
            long microsecondsPerQuarterNote = MICROSECONDS_PER_MINUTE / bpm;
            trackChunk.Events.Add(new SetTempoEvent(microsecondsPerQuarterNote));

            return trackChunk;
        }

        private void WriteFrames(List<TimedEvent> events, IReadOnlyList<Frame> sourceFrames)
        {
            for (int i = 0; i < sourceFrames.Count; i++)
            {
                long frameTick = i * ticksPerFrame;
                Frame frame = sourceFrames[i];

                // set a marker to track frame changes on playback
                events.Add(new TimedEvent(new MarkerEvent($"{frameStartPrefix}{i + 1}"), frameTick));

                // clear all emitters at the start of every frame so each frame is absolute
                // this allows seeking to any frame without depending upon previous frame state
                for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
                {
                    AddControlChange(events, frameTick, activateCC, emitterId);
                    AddControlChange(events, frameTick, removeCC, clearEmittersValue);
                }

                WriteStartGroups(events, frameTick, frame.GetEmitterGroups());

                if (!frame.HasEvents) continue;

                events.Add(new TimedEvent(new MarkerEvent(eventsMarker), frameTick));

                foreach (FrameEvent frameEvent in frame.Events)
                {
                    WriteEvent(events, frameTick + frameEvent.Tick, frameEvent);
                }
            }

            // set marker at end of last frame/quarter note to force playback to play to the end before looping
            events.Add(new TimedEvent(new MarkerEvent(seqEndMarker), sourceFrames.Count * ticksPerFrame));
        }

        // write emitters in turn by type
        private void WriteStartGroups(List<TimedEvent> events, long tick, ulong[] emitterGroups)
        {
            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                ulong group = emitterGroups[emitterId];

                if (group == 0) continue;

                AddControlChange(events, tick, activateCC, emitterId);

                for (byte pos = 0; pos < gridSize; pos++)
                {
                    if ((group & (1UL << pos)) != 0)
                    {
                        AddControlChange(events, tick, placeCC, pos);
                    }
                }
            }
        }

        private void WriteEvent(List<TimedEvent> events, long tick, FrameEvent frameEvent)
        {
            switch (frameEvent.Type)
            {
                case FrameEventType.Select:
                    AddControlChange(events, tick, activateCC, (byte)frameEvent.EmitterId);
                    break;

                case FrameEventType.Place:
                    AddControlChange(events, tick, activateCC, (byte)frameEvent.EmitterId);
                    AddControlChange(events, tick, placeCC, (byte)frameEvent.Cell);
                    break;

                case FrameEventType.Remove:
                    AddControlChange(events, tick, removeCC, (byte)frameEvent.Cell);
                    break;

                case FrameEventType.ClearAll:
                    AddControlChange(events, tick, removeCC, clearEmittersValue);
                    break;

                case FrameEventType.NoteOn:
                    events.Add(new TimedEvent(new NoteOnEvent((SevenBitNumber)frameEvent.Note, (SevenBitNumber)frameEvent.Velocity)
                        { Channel = (FourBitNumber)frameEvent.Channel }, tick));
                    break;

                case FrameEventType.NoteOff:
                    events.Add(new TimedEvent(new NoteOffEvent((SevenBitNumber)frameEvent.Note, (SevenBitNumber)0)
                        { Channel = (FourBitNumber)frameEvent.Channel }, tick));
                    break;
            }
        }

        private void AddControlChange(List<TimedEvent> events, long tick, int cc, byte value)
        {
            events.Add(new TimedEvent(new ControlChangeEvent((SevenBitNumber)cc, (SevenBitNumber)value), tick));
        }

        // --- reading ---

        public List<Frame> FromMidiFileToFrames(MidiFile midiFile)
        {
            ICollection<TimedEvent> timedEvents = midiFile.GetTimedEvents();

            return IsFormat2(timedEvents)
                ? ReadFormat2(timedEvents, GetTicksPerFrame(midiFile), IsOrderKept(midiFile))
                : ReadFormat1(timedEvents, GetTicksPerFrame(midiFile));
        }

        // whether the markers and the CCs share a track, so their order on the same tick is the order they were written in
        private static bool IsOrderKept(MidiFile midiFile)
        {
            int tracksWithEvents = 0;

            foreach (TrackChunk trackChunk in midiFile.GetTrackChunks())
            {
                bool hasMarker = false;
                bool hasControlChange = false;

                foreach (MidiEvent midiEvent in trackChunk.Events)
                {
                    hasMarker |= midiEvent is MarkerEvent;
                    hasControlChange |= midiEvent is ControlChangeEvent;
                }

                if (hasControlChange && !hasMarker) return false;
                if (hasMarker || hasControlChange) tracksWithEvents++;
            }

            return tracksWithEvents <= 1;
        }

        private bool IsFormat2(ICollection<TimedEvent> timedEvents)
        {
            foreach (var timedEvent in timedEvents)
            {
                if (timedEvent.Event is MarkerEvent marker && marker.Text == formatMarker) return true;
            }

            return false;
        }

        // frames start at their START_FRAME_NO_ markers. The start groups are rebuilt from the CCs that follow,
        // until the frame's FRAME_EVENTS marker; CCs after that are events, timed from the frame's start.
        // A CC10 directly followed on the same tick by a CC11 is one placement; a CC10 on its own is a colour select
        private List<Frame> ReadFormat2(ICollection<TimedEvent> timedEvents, long fileTicksPerFrame, bool isOrderKept)
        {
            var frames = new List<Frame>();
            var reader = new Format2FrameReader(emitterCount, ticksPerFrame, fileTicksPerFrame, activateCC, placeCC, removeCC,
                clearEmittersValue, isOrderKept);

            foreach (var timedEvent in timedEvents)
            {
                switch (timedEvent.Event)
                {
                    case MarkerEvent marker when marker.Text.StartsWith(frameStartPrefix, System.StringComparison.Ordinal):
                        reader.Finish(frames);
                        reader.Begin(timedEvent.Time);
                        break;

                    case MarkerEvent marker when marker.Text == eventsMarker:
                        reader.BeginEvents();
                        break;

                    case MarkerEvent marker when marker.Text == seqEndMarker:
                        reader.Finish(frames);
                        break;

                    case ControlChangeEvent cc:
                        reader.Read(timedEvent.Time, cc.ControlNumber, cc.ControlValue);
                        break;

                    // a Note On with velocity 0 is a Note Off
                    case NoteOnEvent noteOn:
                        reader.ReadNote(timedEvent.Time, noteOn.Channel, noteOn.NoteNumber, noteOn.Velocity);
                        break;

                    case NoteOffEvent noteOff:
                        reader.ReadNote(timedEvent.Time, noteOff.Channel, noteOff.NoteNumber, 0);
                        break;
                }
            }

            reader.Finish(frames);

            return frames;
        }

        // reads one frame at a time for ReadFormat2
        sealed class Format2FrameReader
        {
            readonly int emitterCount;
            readonly int ticksPerBeat;
            readonly long fileTicksPerFrame;
            readonly int activateCC;
            readonly int placeCC;
            readonly int removeCC;
            readonly byte clearValue;

            // otherwise the FRAME_EVENTS marker can't be placed among the CCs, and only CCs after the frame's first
            // tick are events
            readonly bool isOrderKept;

            readonly List<FrameEvent> events = new List<FrameEvent>();
            ulong[] startGroups;
            long frameTime;
            bool isReadingEvents;
            int currentEmitter;

            // a CC10 in the events that may yet turn out to be the first half of a placement
            int pendingSelect = -1;
            long pendingSelectTime;

            public Format2FrameReader(int emitterCount, int ticksPerBeat, long fileTicksPerFrame, int activateCC, int placeCC,
                int removeCC, byte clearValue, bool isOrderKept)
            {
                this.isOrderKept = isOrderKept;
                this.emitterCount = emitterCount;
                this.ticksPerBeat = ticksPerBeat;
                this.fileTicksPerFrame = fileTicksPerFrame;
                this.activateCC = activateCC;
                this.placeCC = placeCC;
                this.removeCC = removeCC;
                this.clearValue = clearValue;
            }

            public void Begin(long time)
            {
                startGroups = new ulong[emitterCount];
                events.Clear();
                frameTime = time;
                isReadingEvents = false;
                pendingSelect = -1;
            }

            public void BeginEvents()
            {
                if (startGroups != null) isReadingEvents = true;
            }

            public void Read(long time, int controlNumber, int value)
            {
                // CCs before the first frame marker belong to no frame
                if (startGroups == null) return;

                bool isEvent = isOrderKept ? isReadingEvents : time > frameTime;

                if (isEvent)
                    ReadEvent(time, controlNumber, value);
                else
                    ReadStart(controlNumber, value);
            }

            // notes are always events: they're never part of a frame's start
            public void ReadNote(long time, int channel, int note, int velocity)
            {
                if (startGroups == null) return;

                FlushPendingSelect();

                int tick = ToTick(time);
                events.Add(velocity > 0 ? FrameEvent.NoteOn(tick, channel, note, velocity) : FrameEvent.NoteOff(tick, channel, note));
            }

            private void ReadStart(int controlNumber, int value)
            {
                if (controlNumber == activateCC)
                {
                    if (value < emitterCount) currentEmitter = value;
                }
                else if (controlNumber == placeCC)
                {
                    if (value < 64) Place(startGroups, currentEmitter, value);
                }
                else if (controlNumber == removeCC)
                {
                    if (value == clearValue)
                        System.Array.Clear(startGroups, 0, startGroups.Length);
                    else if (value < 64)
                        Remove(startGroups, value);
                }
            }

            private void ReadEvent(long time, int controlNumber, int value)
            {
                int tick = ToTick(time);

                if (controlNumber == activateCC)
                {
                    FlushPendingSelect();

                    if (value >= emitterCount) return;

                    currentEmitter = value;
                    pendingSelect = value;
                    pendingSelectTime = time;
                }
                else if (controlNumber == placeCC)
                {
                    bool isPlacementPair = pendingSelect >= 0 && pendingSelectTime == time;

                    if (!isPlacementPair) FlushPendingSelect();
                    pendingSelect = -1;

                    if (value < 64) events.Add(FrameEvent.Place(tick, currentEmitter, value));
                }
                else if (controlNumber == removeCC)
                {
                    FlushPendingSelect();

                    if (value == clearValue)
                        events.Add(FrameEvent.ClearAll(tick));
                    else if (value < 64)
                        events.Add(FrameEvent.Remove(tick, value));
                }
            }

            private void FlushPendingSelect()
            {
                if (pendingSelect < 0) return;

                events.Add(FrameEvent.Select(ToTick(pendingSelectTime), pendingSelect));
                pendingSelect = -1;
            }

            // file ticks from the frame's start, scaled to the app's ticks per beat
            private int ToTick(long time)
            {
                long offset = time - frameTime;

                return (int)(offset * ticksPerBeat / fileTicksPerFrame);
            }

            public void Finish(List<Frame> frames)
            {
                if (startGroups == null) return;

                FlushPendingSelect();

                var frame = new Frame(startGroups);
                frame.SetEvents(events);
                frames.Add(frame);

                startGroups = null;
            }

            private static void Place(ulong[] groups, int emitterId, int cell)
            {
                Remove(groups, cell);
                groups[emitterId] |= 1UL << cell;
            }

            private static void Remove(ulong[] groups, int cell)
            {
                for (int i = 0; i < groups.Length; i++)
                    groups[i] &= ~(1UL << cell);
            }
        }

        // format 1: CC events grouped into frames by quarter note; only placements are applied, markers are ignored
        private List<Frame> ReadFormat1(ICollection<TimedEvent> timedEvents, long fileTicksPerFrame)
        {
            List<Frame> frames = new List<Frame>();

            long currentFrameTick = -1;
            Frame currentFrame = null;
            byte currentEmitterId = 0;

            // add events to frame based on the calculation of what events belong in each frame
            foreach (var timedEvent in timedEvents)
            {
                // skip non-CC events entirely — markers should not trigger frame boundaries
                if (timedEvent.Event is not ControlChangeEvent cc) continue;

                long frameTick = (timedEvent.Time / fileTicksPerFrame) * fileTicksPerFrame;

                if (frameTick != currentFrameTick)
                {
                    if (currentFrame != null)
                    {
                        frames.Add(currentFrame);
                    }

                    currentFrame = new Frame();
                    currentFrameTick = frameTick;
                    currentEmitterId = 0;
                }

                int controlNumber = (int)cc.ControlNumber;

                if (controlNumber == activateCC)
                {
                    currentEmitterId = (byte)cc.ControlValue;
                }
                else if (controlNumber == placeCC)
                {
                    currentFrame.AddEmitter(EmitterUtils.IndexToPosition((byte)cc.ControlValue), currentEmitterId, enforceTwoLane: false);
                }
            }

            if (currentFrame != null)
            {
                frames.Add(currentFrame);
            }

            return frames;
        }

        private long GetTicksPerFrame(MidiFile midiFile)
        {
            short tpqn = (midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision)?.TicksPerQuarterNote ?? ticksPerFrame;

            return tpqn;
        }
    }
}
