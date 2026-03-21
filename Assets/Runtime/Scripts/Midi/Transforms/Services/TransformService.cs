using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Tempera.Mental.Frames;
using Tempera.Mental.Logs;
using UnityEngine;

namespace Tempera.Mental.Midi.Transforms
{
    public class TransformService : MonoBehaviour
    {
        const int MICROSECONDS_PER_MINUTE = 60_000_000;
        const int TICKS_PER_FRAME = 480;

        const int CC_ACTIVATE = 10;
        const int CC_PLACE = 11;
        const int CC_REMOVE = 12;

        const byte BLUE = 0;
        const byte RED = 1;
        const byte YELLOW = 2;
        const byte GREEN = 3;

        const string FRAME_NO = "FRAME_NO_";

        int bpm = 400;

        readonly List<EmitterDetail> activeEmitters = new List<EmitterDetail>();

        public void SetBpm(int bpm) => this.bpm = bpm;

        public MidiFile FromFramesToMidiFile(IReadOnlyList<Frame> sourceFrames, int startFrame = 1)
        {
            MidiFile midiFile = BuildMidiFile();
            TrackChunk trackChunk = BuildTrackChunk();

            using (var manager = trackChunk.ManageTimedEvents())
            {
                WriteFrames(manager, sourceFrames, startFrame);
            }

            midiFile.Chunks.Add(trackChunk);
            return midiFile;
        }

        private MidiFile BuildMidiFile()
        {
            var midiFile = new MidiFile();
            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)TICKS_PER_FRAME);
            return midiFile;
        }

        private TrackChunk BuildTrackChunk()
        {
            var trackChunk = new TrackChunk();
            long microsecondsPerQuarterNote = MICROSECONDS_PER_MINUTE / bpm;
            trackChunk.Events.Add(new SetTempoEvent(microsecondsPerQuarterNote));
            return trackChunk;
        }

        private void WriteFrames(TimedObjectsManager<TimedEvent> manager, IReadOnlyList<Frame> sourceFrames, int startFrame)
        {
            Dictionary<byte, HashSet<byte>> previousFrameGroups = CreateEmptyGroups();

            for (int i = 0; i < sourceFrames.Count; i++)
            {
                long frameTick = i * TICKS_PER_FRAME;

                frameTick = WriteFrameStart(manager, frameTick, startFrame + i);

                if (i == 0)
                    frameTick = ClearAllEmitters(manager, frameTick);

                Dictionary<byte, HashSet<byte>> currentFrameGroups = GroupByEmitter(sourceFrames[i]);

                frameTick = WriteRemovals(manager, frameTick, previousFrameGroups, currentFrameGroups);
                WriteAdditions(manager, frameTick, previousFrameGroups, currentFrameGroups);

                previousFrameGroups = CopyGroups(currentFrameGroups);
            }
        }

        private long WriteFrameStart(TimedObjectsManager<TimedEvent> manager, long tick, int frameNumber)
        {
            manager.Objects.Add(new TimedEvent(new MarkerEvent($"{FRAME_NO}{frameNumber}"), tick++));
            return tick;
        }

        private long WriteRemovals(TimedObjectsManager<TimedEvent> manager, long tick,
                                    Dictionary<byte, HashSet<byte>> previous,
                                    Dictionary<byte, HashSet<byte>> current)
        {
            for (byte emitter = BLUE; emitter <= GREEN; emitter++)
            {
                List<byte> toRemove = previous[emitter].Except(current[emitter]).ToList();
                if (toRemove.Count == 0) continue;

                tick = WriteActivateEmitter(manager, tick, emitter);

                foreach (byte pos in toRemove)
                    tick = WriteEmitterEvent(manager, tick, CC_REMOVE, pos);
            }
            return tick;
        }

        private void WriteAdditions(TimedObjectsManager<TimedEvent> manager, long tick,
                                     Dictionary<byte, HashSet<byte>> previous,
                                     Dictionary<byte, HashSet<byte>> current)
        {
            for (byte emitter = BLUE; emitter <= GREEN; emitter++)
            {
                List<byte> toAdd = current[emitter].Except(previous[emitter]).ToList();
                if (toAdd.Count == 0) continue;

                tick = WriteActivateEmitter(manager, tick, emitter);

                foreach (byte pos in toAdd)
                    tick = WriteEmitterEvent(manager, tick, CC_PLACE, pos);
            }
            return;
        }

        private long WriteActivateEmitter(TimedObjectsManager<TimedEvent> manager, long tick, byte emitter)
        {
            manager.Objects.Add(new TimedEvent(
                new ControlChangeEvent((SevenBitNumber)CC_ACTIVATE, (SevenBitNumber)emitter), tick++));
            return tick;
        }

        private long WriteEmitterEvent(TimedObjectsManager<TimedEvent> manager, long tick, int cc, byte value)
        {
            manager.Objects.Add(new TimedEvent(
                new ControlChangeEvent((SevenBitNumber)cc, (SevenBitNumber)value), tick++));
            return tick;
        }

        private long ClearAllEmitters(TimedObjectsManager<TimedEvent> manager, long tick)
        {
            for (int i = BLUE; i <= GREEN; i++)
            {
                tick = WriteActivateEmitter(manager, tick, (byte)i);
                tick = WriteEmitterEvent(manager, tick, CC_REMOVE, 64);
            }
            return tick;
        }

        public List<Frame> FromMidiFileToFrames(MidiFile midiFile)
        {
            List<Frame> frames = new List<Frame>();

            long ticksPerFrame = GetTicksPerFrame(midiFile);
            ICollection<TimedEvent> timedEvents = midiFile.GetTimedEvents();

            var eventsByFrame = timedEvents.GroupBy(e => e.Time / ticksPerFrame).OrderBy(g => g.Key);

            Dictionary<Vector2Int, EmitterDetail> frameBuffer = new Dictionary<Vector2Int, EmitterDetail>();

            foreach (var group in eventsByFrame)
            {
                Frame frame = BuildFrameFromEvents(group, frameBuffer);
                frames.Add(frame);
            }

            return frames;
        }

        private long GetTicksPerFrame(MidiFile midiFile)
        {
            short tpqn = (midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision)
                         ?.TicksPerQuarterNote ?? TICKS_PER_FRAME;
            LogMan.Log("TPQN: " + tpqn);
            return tpqn;
        }

        private Frame BuildFrameFromEvents(IGrouping<long, TimedEvent> group,
                                            Dictionary<Vector2Int, EmitterDetail> frameBuffer)
        {
            Frame frame = new Frame();
            byte currentEmitterId = 0;

            foreach (var timedEvent in group)
            {
                if (timedEvent.Event is not ControlChangeEvent cc) continue;

                switch ((int)cc.ControlNumber)
                {
                    case CC_ACTIVATE:
                        currentEmitterId = (byte)cc.ControlValue;
                        break;

                    case CC_PLACE:
                        HandlePlaceEvent(cc, currentEmitterId, frameBuffer);
                        break;

                    case CC_REMOVE:
                        HandleRemoveEvent(cc, frameBuffer);
                        break;
                }
            }

            PopulateFrame(frame, frameBuffer);
            return frame;
        }

        private void HandlePlaceEvent(ControlChangeEvent cc, byte emitterId,
                                       Dictionary<Vector2Int, EmitterDetail> frameBuffer)
        {
            Vector2Int pos = IndexToPosition((byte)cc.ControlValue);
            frameBuffer[pos] = new EmitterDetail { Position = pos, EmitterId = emitterId };
        }

        private void HandleRemoveEvent(ControlChangeEvent cc,
                                        Dictionary<Vector2Int, EmitterDetail> frameBuffer)
        {
            Vector2Int pos = IndexToPosition((byte)cc.ControlValue);
            frameBuffer.Remove(pos);
        }

        private void PopulateFrame(Frame frame, Dictionary<Vector2Int, EmitterDetail> frameBuffer)
        {
            foreach (var kvp in frameBuffer)
                frame.AddEmitter(kvp.Value);
        }

        private Dictionary<byte, HashSet<byte>> GroupByEmitter(Frame frame)
        {
            Dictionary<byte, HashSet<byte>> groups = CreateEmptyGroups();

            frame.ListActiveEmitters(activeEmitters);

            foreach (var emitterDetail in activeEmitters)
            {
                byte id = (byte)emitterDetail.EmitterId;
                byte index = PositionToIndex(emitterDetail.Position);
                groups[id].Add(index);
            }

            return groups;
        }

        private byte PositionToIndex(Vector2Int pos)
        {
            int flippedY = 7 - pos.y;
            return (byte)(pos.x * 8 + flippedY);
        }

        private Vector2Int IndexToPosition(byte index)
        {
            int x = index / 8;
            int y = 7 - (index % 8);
            return new Vector2Int(x, y);
        }

        private Dictionary<byte, HashSet<byte>> CreateEmptyGroups()
        {
            return new Dictionary<byte, HashSet<byte>>
            {
                { BLUE,   new HashSet<byte>() },
                { RED,    new HashSet<byte>() },
                { YELLOW, new HashSet<byte>() },
                { GREEN,  new HashSet<byte>() }
            };
        }

        private Dictionary<byte, HashSet<byte>> CopyGroups(Dictionary<byte, HashSet<byte>> source)
        {
            return source.ToDictionary(kvp => kvp.Key, kvp => new HashSet<byte>(kvp.Value));
        }
    }
}