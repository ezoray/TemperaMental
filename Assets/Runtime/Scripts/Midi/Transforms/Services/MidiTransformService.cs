using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Tempera.Mental.Frames;
using UnityEngine;

namespace Tempera.Mental.Midi.Transforms
{
    public class MidiTransformService : MonoBehaviour
    {
        int bpm = 60;
        int ticksPerFrame = 96;

        public void SetBpm(int bpm)
        {
            this.bpm = bpm;
        }

        public MidiFile FromFramesToMidiFile(List<Frame> sourceFrames)
        {
            var midiFile = new MidiFile();
            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)ticksPerFrame);

            var trackChunk = new TrackChunk();
            using (var manager = trackChunk.ManageTimedEvents())
            {
                for (int i = 0; i < sourceFrames.Count; i++)
                {
                    long frameStartTick = (long)i * ticksPerFrame;
                    var frame = sourceFrames[i];
                    var groups = GroupByEmitter(frame);

                    // 1. CLEAR (CC 12) - Value 127
                    // Happens at the very start of the frame block
                    manager.Objects.Add(new TimedEvent(
                        new ControlChangeEvent((SevenBitNumber)12, (SevenBitNumber)127),
                        frameStartTick));

                    int staggerOffset = 1;

                    foreach (var kvp in groups)
                    {
                        // 2. SELECT EMITTER (CC 10) - 1 tick after Clear
                        manager.Objects.Add(new TimedEvent(
                            new ControlChangeEvent((SevenBitNumber)10, (SevenBitNumber)kvp.Key),
                            frameStartTick + staggerOffset));

                        // 3. SET POSITIONS (CC 11) - 1 tick after Select
                        // This ensures the Emitter Select is "locked in" first
                        foreach (var pos in kvp.Value)
                        {
                            manager.Objects.Add(new TimedEvent(
                                new ControlChangeEvent((SevenBitNumber)11, (SevenBitNumber)pos),
                                frameStartTick + staggerOffset + 1));
                        }

                        // Increase offset for the next emitter group in this frame
                        staggerOffset += 2;
                    }
                }
            }

            midiFile.Chunks.Add(trackChunk);
            midiFile.ReplaceTempoMap(TempoMap.Create(Tempo.FromBeatsPerMinute(bpm)));

            return midiFile;
        }

        private Dictionary<byte, List<byte>> GroupByEmitter(Frame frame)
        {
            // Key: EmitterID (0-3), Value: List of Grid Indices (0-63)
            var groups = new Dictionary<byte, List<byte>>();

            foreach (var emitter in frame.Matrix.Values)
            {
                // 1. Get the ID (0-3)
                byte id = (byte)emitter.EmitterId;

                int flippedY = 7 - (int)emitter.Position.y;
                byte index = (byte)((emitter.Position.x * 8) + flippedY);

                // 3. Add to the dictionary
                if (!groups.ContainsKey(id)) groups[id] = new List<byte>();
                groups[id].Add(index);
            }

            return groups;
        }

        public List<Frame> FromMidiFileToFrames(MidiFile midiFile)
        {
            var frames = new List<Frame>();
            // Get all timed events and group them by their "Frame" time
            var timedEvents = midiFile.GetTimedEvents();

            // We group events by their start tick (e.g., 0, 96, 192...)
            var eventsByFrame = timedEvents.GroupBy(e => e.Time / ticksPerFrame)
                                           .OrderBy(g => g.Key);

            foreach (var group in eventsByFrame)
            {
                Frame frame = new Frame();
                byte currentEmitterId = 0;

                foreach (var timedEvent in group)
                {
                    if (timedEvent.Event is ControlChangeEvent cc)
                    {
                        switch (cc.ControlNumber)
                        {
                            case 10: // SELECT EMITTER
                                currentEmitterId = (byte)cc.ControlValue;
                                break;

                            case 11: // POSITION INDEX
                                byte index = (byte)cc.ControlValue;

                                // REVERSE MATH:
                                int x = index / 8;
                                int y = 7 - (index % 8);

                                // Create the emitter data
                                var emitter = new EmitterDetail
                                {
                                    Position = new Vector2Int(x, y),
                                    EmitterId = currentEmitterId
                                };

                                // Add to frame (using x_y as key to match your dictionary structure)
                                frame.Matrix[new Vector2Int(x, y)] = emitter;
                                break;

                            case 12: // CLEAR
                                     // We handle this implicitly by starting with a new Frame object
                                break;
                        }
                    }
                }
                frames.Add(frame);
            }

            return frames;
        }
    }
}
