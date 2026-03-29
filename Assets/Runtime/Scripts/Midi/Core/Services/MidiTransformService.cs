using System.Collections.Generic;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using UnityEngine;

namespace TemperaMental.Midi.Transforms
{
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

        int gridWidth;
        int gridHeight;
        int gridSize;
        int emitterCount;

        // reusable bitmask buffers, one ulong per emitter (bits 0–63 map to grid positions)
        ulong[] previousGroups;
        ulong[] currentGroups;

        private void OnEnable()
        {
            frameStartPrefix = ConfigRegistry.Midi.FrameStartPrefix;
            seqEndMarker = ConfigRegistry.Midi.SeqEndMarker;

            ticksPerFrame = ConfigRegistry.Midi.TicksPerFrame;

            activateCC = ConfigRegistry.Midi.ActivateCC;
            placeCC = ConfigRegistry.Midi.PlaceCC;
            removeCC = ConfigRegistry.Midi.RemoveCC;
            clearEmittersValue = ConfigRegistry.Midi.ClearEmittersValue;
            emitterCount = ConfigRegistry.Grid.EmitterCount;

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;
            gridSize = gridWidth * gridHeight;


            previousGroups = new ulong[emitterCount];
            currentGroups = new ulong[emitterCount];
        }

        public MidiFile FromFramesToMidiFileReversed(IReadOnlyList<Frame> sourceFrames, int bpm)
        {
            List<Frame> reversedFrames = GetReversedList<Frame>(sourceFrames);

            return FromFramesToMidiFile(reversedFrames, bpm, true);
        }

        private List<T> GetReversedList<T>(IReadOnlyList<T> original)
        {
            List<T> reversed = new List<T>(original.Count);

            for (int i = original.Count - 1; i >= 0; i--)
            {
                reversed.Add(original[i]);
            }

            return reversed;
        }


        public MidiFile FromFramesToMidiFile(IReadOnlyList<Frame> sourceFrames, int bpm, bool isReversed)
        {
            MidiFile midiFile = BuildMidiFile();
            TrackChunk trackChunk = BuildTrackChunk(bpm);

            using (var manager = trackChunk.ManageTimedEvents())
            {
                WriteFrames(manager, sourceFrames, isReversed);
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

        private void WriteFrames(TimedObjectsManager<TimedEvent> manager, IReadOnlyList<Frame> sourceFrames, bool isReversed)
        {
            ClearGroups(previousGroups);

            for (int i = 0; i < sourceFrames.Count; i++)
            {
                long frameTick = i * ticksPerFrame;

                int frameNumber = isReversed ? sourceFrames.Count - i : i + 1;

                frameTick = WriteFrameStart(manager, frameTick, frameNumber);

                if (i == 0)
                {
                    frameTick = ClearAllEmitters(manager, frameTick);
                }

                FillGroups(currentGroups, sourceFrames[i]);

                frameTick = WriteRemovals(manager, frameTick);
                WriteAdditions(manager, frameTick);

                CopyGroups(currentGroups, previousGroups);
            }

            // set marker at end of last frame/quarter note to force playback to play to the end before looping
            // todo there is still slight drift (can't see an easy way to fix)
            manager.Objects.Add(new TimedEvent(new MarkerEvent(seqEndMarker), sourceFrames.Count * ticksPerFrame));
        }

        // set a marker to track frame changes on playback
        private long WriteFrameStart(TimedObjectsManager<TimedEvent> manager, long tick, int frameNumber)
        {
            manager.Objects.Add(new TimedEvent(new MarkerEvent($"{frameStartPrefix}{frameNumber}"), tick++));

            return tick;
        }

        // remove any emitters that exist in the previous frame but not in the current frame
        private long WriteRemovals(TimedObjectsManager<TimedEvent> manager, long tick)
        {
            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                ulong toRemove = previousGroups[emitterId] & ~currentGroups[emitterId];

                if (toRemove == 0) continue;

                for (byte pos = 0; pos < gridSize; pos++)
                {
                    if ((toRemove & (1UL << pos)) != 0)
                    {
                        tick = WriteEmitterEvent(manager, tick, removeCC, pos);
                    }
                }
            }
            return tick;
        }

        // write emitters that exist in the current frame that don't exist in the previous frame
        private long WriteAdditions(TimedObjectsManager<TimedEvent> manager, long tick)
        {
            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                ulong toAdd = currentGroups[emitterId] & ~previousGroups[emitterId];
                if (toAdd == 0) continue;

                tick = WriteActivateEmitter(manager, tick, emitterId);

                for (byte pos = 0; pos < gridSize; pos++)
                {
                    if ((toAdd & (1UL << pos)) != 0)
                        tick = WriteEmitterEvent(manager, tick, placeCC, pos);
                }
            }
            return tick;
        }

        private long WriteActivateEmitter(TimedObjectsManager<TimedEvent> manager, long tick, byte emitter)
        {
            manager.Objects.Add(new TimedEvent(new ControlChangeEvent((SevenBitNumber)activateCC, (SevenBitNumber)emitter), tick++));

            return tick;
        }

        private long WriteEmitterEvent(TimedObjectsManager<TimedEvent> manager, long tick, int cc, byte value)
        {
            manager.Objects.Add(new TimedEvent(new ControlChangeEvent((SevenBitNumber)cc, (SevenBitNumber)value), tick++));

            return tick;
        }

        private long ClearAllEmitters(TimedObjectsManager<TimedEvent> manager, long tick)
        {
            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                tick = WriteActivateEmitter(manager, tick, emitterId);
                tick = WriteEmitterEvent(manager, tick, removeCC, clearEmittersValue);
            }
            return tick;
        }


        public List<Frame> FromMidiFileToFrames(MidiFile midiFile)
        {
            List<Frame> frames = new List<Frame>();

            long ticksPerFrame = GetTicksPerFrame(midiFile);
            ICollection<TimedEvent> timedEvents = midiFile.GetTimedEvents();

            long currentFrameTick = -1;
            Frame currentFrame = null;
            byte currentEmitterId = 0;

            // add events to frame based on the calculation of what events belong in each frame
            foreach (var timedEvent in timedEvents)
            {
                long frameTick = (timedEvent.Time / ticksPerFrame) * ticksPerFrame;

                if (frameTick != currentFrameTick)
                {
                    if (currentFrame != null)
                    {
                        frames.Add(currentFrame);
                    }

                    // use the last frame as the basis for the new frame as events represent the difference between the two
                    currentFrame = currentFrame != null ? new Frame(currentFrame): new Frame(gridWidth, gridHeight);

                    currentFrameTick = frameTick;
                    currentEmitterId = 0;
                }

                if (timedEvent.Event is not ControlChangeEvent cc) continue;

                int controlNumber = (int)cc.ControlNumber;

                if (controlNumber == activateCC)
                {
                    currentEmitterId = (byte)cc.ControlValue;
                }
                else if (controlNumber == placeCC)
                {
                    currentFrame.AddEmitter(new EmitterDetail(IndexToPosition((byte)cc.ControlValue), currentEmitterId));
                }
                else if (controlNumber == removeCC)
                {
                    byte value = (byte)cc.ControlValue;

                    if (value == clearEmittersValue)
                    {
                        currentFrame.ClearEmitters();
                    }
                    else
                    {
                        currentFrame.TryRemoveEmitter(IndexToPosition(value));
                    }
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

        // populate groups by frame's active emitters
        private void FillGroups(ulong[] groups, Frame frame)
        {
            ClearGroups(groups);

            // instead of getting a list of active emitters from frame, pass in the method that acts upon them
            frame.ActionActiveEmitters(AddEmitterToGroup);
        }

        // sets the bit position that represents an active emitter for that colour
        private void AddEmitterToGroup(EmitterDetail emitterDetail)
        {
            byte pos = PositionToIndex(emitterDetail.Position);
            currentGroups[emitterDetail.EmitterId] |= 1UL << pos;
        }

        private static void ClearGroups(ulong[] groups)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                groups[i] = 0;
            }
        }

        private static void CopyGroups(ulong[] source, ulong[] dest)
        {
            for (int i = 0; i < source.Length; i++)
            {
                dest[i] = source[i];
            }
        }

        // change tilemap-based position to cc value index
        private byte PositionToIndex(Vector2Int pos)
        {
            int flippedY = (gridHeight -1) - pos.y;

            return (byte)(pos.x * gridHeight + flippedY);
        }

        // change cc value index to tilemap-based position
        private Vector2Int IndexToPosition(byte index)
        {
            int x = index / gridHeight;
            int y = (gridHeight -1) - (index % gridHeight);

            return new Vector2Int(x, y);
        }
    }
}