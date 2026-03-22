using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using TemperaMental.Applications.Config;
using TemperaMental.Frames;
using TemperaMental.Logs;
using UnityEngine;

namespace TemperaMental.Midi.Transforms
{
    public class TransformService : MonoBehaviour
    {
        const string END_OF_SEQUENCE = "SEQ_END";
        const int MICROSECONDS_PER_MINUTE = 60_000_000;

        string frameNo;
        int bpm;
        short ticksPerFrame;
        int activateCC;
        int placeCC;
        int removeCC;

        byte blueId;
        byte redId;
        byte yellowId;
        byte greenId;

        readonly List<EmitterDetail> activeEmitters = new List<EmitterDetail>();


        private void OnEnable()
        {
            frameNo = ConfigRegistry.Midi.FrameNumberPrefix;
            ticksPerFrame = ConfigRegistry.Midi.TicksPerFrame;
            bpm = ConfigRegistry.Midi.Bpm;

            activateCC = ConfigRegistry.Midi.ActivateCC;
            placeCC = ConfigRegistry.Midi.PlaceCC;
            removeCC = ConfigRegistry.Midi.RemoveCC;

            blueId = ConfigRegistry.Grid.BlueEmitterId;
            redId = ConfigRegistry.Grid.RedEmitterId;
            yellowId = ConfigRegistry.Grid.YellowEmitterId;
            greenId = ConfigRegistry.Grid.GreenEmitterId;
        }

        public void SetBpm(int bpm)
        {
            this.bpm = bpm;
        }

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
            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)ticksPerFrame);
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
                long frameTick = i * ticksPerFrame;

                frameTick = WriteFrameStart(manager, frameTick, startFrame + i);

                if (i == 0)
                    frameTick = ClearAllEmitters(manager, frameTick);

                Dictionary<byte, HashSet<byte>> currentFrameGroups = GroupByEmitter(sourceFrames[i]);

                frameTick = WriteRemovals(manager, frameTick, previousFrameGroups, currentFrameGroups);
                WriteAdditions(manager, frameTick, previousFrameGroups, currentFrameGroups);

                previousFrameGroups = CopyGroups(currentFrameGroups);
            }

            // set marker on last tick of sequence to force playback to read to the end, otherwise looping starts too early
            manager.Objects.Add(new TimedEvent(new MarkerEvent(END_OF_SEQUENCE), sourceFrames.Count * ticksPerFrame));
        }

        private long WriteFrameStart(TimedObjectsManager<TimedEvent> manager, long tick, int frameNumber)
        {
            manager.Objects.Add(new TimedEvent(new MarkerEvent($"{frameNo}{frameNumber}"), tick++));
            return tick;
        }

        private long WriteRemovals(TimedObjectsManager<TimedEvent> manager, long tick,
                                    Dictionary<byte, HashSet<byte>> previous,
                                    Dictionary<byte, HashSet<byte>> current)
        {
            for (byte emitter = blueId; emitter <= greenId; emitter++)
            {
                List<byte> toRemove = previous[emitter].Except(current[emitter]).ToList();
                if (toRemove.Count == 0) continue;

                tick = WriteActivateEmitter(manager, tick, emitter);

                foreach (byte pos in toRemove)
                    tick = WriteEmitterEvent(manager, tick, removeCC, pos);
            }
            return tick;
        }

        private void WriteAdditions(TimedObjectsManager<TimedEvent> manager, long tick,
                                     Dictionary<byte, HashSet<byte>> previous,
                                     Dictionary<byte, HashSet<byte>> current)
        {
            for (byte emitter = blueId; emitter <= greenId; emitter++)
            {
                List<byte> toAdd = current[emitter].Except(previous[emitter]).ToList();
                if (toAdd.Count == 0) continue;

                tick = WriteActivateEmitter(manager, tick, emitter);

                foreach (byte pos in toAdd)
                    tick = WriteEmitterEvent(manager, tick, placeCC, pos);
            }
            return;
        }

        private long WriteActivateEmitter(TimedObjectsManager<TimedEvent> manager, long tick, byte emitter)
        {
            manager.Objects.Add(new TimedEvent(
                new ControlChangeEvent((SevenBitNumber)activateCC, (SevenBitNumber)emitter), tick++));
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
            for (int i = blueId; i <= greenId; i++)
            {
                tick = WriteActivateEmitter(manager, tick, (byte)i);
                tick = WriteEmitterEvent(manager, tick, removeCC, 64);
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
            short tpqn = (midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision)?.TicksPerQuarterNote ?? ticksPerFrame;
            LogMan.Log("TPQN: " + tpqn);
            return tpqn;
        }

        private Frame BuildFrameFromEvents(IGrouping<long, TimedEvent> group,
                                            Dictionary<Vector2Int, EmitterDetail> frameBuffer)
        {
            Frame frame = new Frame(ConfigRegistry.Grid.GridWidth, ConfigRegistry.Grid.GridHeight);
            byte currentEmitterId = 0;

            foreach (var timedEvent in group)
            {
                if (timedEvent.Event is not ControlChangeEvent cc) continue;

                int controlNumber = (int)cc.ControlNumber;

                if (controlNumber == activateCC)
                {
                    currentEmitterId = (byte)cc.ControlValue;
                }
                else if (controlNumber == placeCC)
                {
                    HandlePlaceEvent(cc, currentEmitterId, frameBuffer);
                }
                else if (controlNumber == removeCC)
                {
                    HandleRemoveEvent(cc, frameBuffer);
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
                { blueId,   new HashSet<byte>() },
                { redId,    new HashSet<byte>() },
                { yellowId, new HashSet<byte>() },
                { greenId,  new HashSet<byte>() }
            };
        }

        private Dictionary<byte, HashSet<byte>> CopyGroups(Dictionary<byte, HashSet<byte>> source)
        {
            return source.ToDictionary(kvp => kvp.Key, kvp => new HashSet<byte>(kvp.Value));
        }
    }
}