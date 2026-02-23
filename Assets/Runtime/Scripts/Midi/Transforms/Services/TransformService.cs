using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Tempera.Mental.Frames;
using UnityEngine;

namespace Tempera.Mental.Midi.Transforms
{
    public class TransformService : MonoBehaviour
    {
        const int ACTIVATE_CC = 10;
        const int PLACE_CC = 11;
        const int REMOVE_CC = 12;

        int bpm = 400;
        int ticksPerFrame = 480;


        public MidiFile FromFramesToMidiFile(List<Frame> sourceFrames)
        {
            var midiFile = new MidiFile();
            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)ticksPerFrame);

            var trackChunk = new TrackChunk();

            using (var manager = trackChunk.ManageTimedEvents())
            {
                long initialTick = 0;

                // --- 0. INITIAL FULL CLEAR ---
                initialTick = ClearAllEmitters(manager, initialTick);

                // Track previous frame state: emitter -> positions
                var previousFrameGroups = new Dictionary<byte, HashSet<byte>>()
                {
                    { 0, new HashSet<byte>() },
                    { 1, new HashSet<byte>() },
                    { 2, new HashSet<byte>() },
                    { 3, new HashSet<byte>() }
                };

                // --- 1. FRAME LOOP ---
                for (int i = 0; i < sourceFrames.Count; i++)
                {
                    Debug.Log("Frame: " + i);

                    long frameTick = i * ticksPerFrame;

                    if (i == 0)
                        frameTick += initialTick; // first frame starts after initial clear

                    // Build current frame emitter -> positions
                    var currentFrameGroups = GroupByEmitter(sourceFrames[i]);

                    // --- 1A. OFF PASS: remove pixels that were on but are no longer active ---
                    for (byte emitter = 0; emitter < 4; emitter++)
                    {
                        var prevPixels = previousFrameGroups[emitter];
                        var currPixels = currentFrameGroups[emitter];

                        var toRemove = prevPixels.Except(currPixels).ToList();
                        if (toRemove.Count == 0) continue;

                        // Select emitter once
                        manager.Objects.Add(new TimedEvent(
                            new ControlChangeEvent((SevenBitNumber)ACTIVATE_CC, (SevenBitNumber)emitter), frameTick++));

                        foreach (var pos in toRemove)
                        {
                            manager.Objects.Add(new TimedEvent(
                                new ControlChangeEvent((SevenBitNumber)REMOVE_CC, (SevenBitNumber)pos), frameTick++));
                        }
                    }

                    // --- 1B. ON/UPDATE PASS: place new or changed pixels ---
                    for (byte emitter = 0; emitter < 4; emitter++)
                    {
                        var prevPixels = previousFrameGroups[emitter];
                        var currPixels = currentFrameGroups[emitter];

                        var toAdd = currPixels.Except(prevPixels).ToList();
                        if (toAdd.Count == 0) continue;

                        // Select emitter once
                        manager.Objects.Add(new TimedEvent(
                            new ControlChangeEvent((SevenBitNumber)ACTIVATE_CC, (SevenBitNumber)emitter), frameTick++));

                        Debug.Log("Active Emitter Tick: " + (frameTick - 1));

                        foreach (var pos in toAdd)
                        {
                            manager.Objects.Add(new TimedEvent(
                                new ControlChangeEvent((SevenBitNumber)PLACE_CC, (SevenBitNumber)pos), frameTick++));

                            Debug.Log($"Place Emitter Pos: {pos} Tick: " + (frameTick - 1));
                        }
                    }

                    // --- 2. UPDATE previous frame for next iteration ---
                    previousFrameGroups = currentFrameGroups.ToDictionary(kvp => kvp.Key, kvp => new HashSet<byte>(kvp.Value));
                }
            }

            midiFile.Chunks.Add(trackChunk);
            midiFile.ReplaceTempoMap(TempoMap.Create(Tempo.FromBeatsPerMinute(bpm)));

            return midiFile;
        }

        private long ClearAllEmitters(TimedObjectsManager<TimedEvent> manager, long frameTickPosition)
        {
            for (int i = 0; i < 4; i++)
            {
                manager.Objects.Add(new TimedEvent(
                        new ControlChangeEvent((SevenBitNumber)ACTIVATE_CC, (SevenBitNumber)i),
                        frameTickPosition++));

                manager.Objects.Add(new TimedEvent(
                        new ControlChangeEvent((SevenBitNumber)REMOVE_CC, (SevenBitNumber)64),
                        frameTickPosition++));
            }

            return frameTickPosition;
        }

        private Dictionary<byte, HashSet<byte>> GroupByEmitter(Frame frame)
        {
            // Key: EmitterID (0-3), Value: Set of Grid Indices (0-63)
            var groups = new Dictionary<byte, HashSet<byte>>();

            // initialize empty sets for all 4 emitters
            for (byte emitterId = 0; emitterId < 4; emitterId++)
                groups[emitterId] = new HashSet<byte>();

            foreach (var emitterDetail in frame.Matrix.Values)
            {
                byte id = (byte)emitterDetail.EmitterId;

                int flippedY = 7 - (int)emitterDetail.Position.y;
                byte index = (byte)((emitterDetail.Position.x * 8) + flippedY);

                groups[id].Add(index); // automatically no duplicates because HashSet
            }

            return groups;
        }


        public List<Frame> FromMidiFileToFrames(MidiFile midiFile)
        {
            var frames = new List<Frame>();
            // Get all timed events and group them by their "Frame" time
            var timedEvents = midiFile.GetTimedEvents();

            var tpqn = (midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision)?.TicksPerQuarterNote ?? 480;

            Debug.Log("TPQN: " + tpqn);

            // 2️⃣ Compute ticks per frame (adjust 1 quarter note per frame here if needed)
            long foundTicksPerFrame = tpqn * 1;

            // We group events by their start tick (e.g., 0, 96, 192...)
            var eventsByFrame = timedEvents.GroupBy(e => e.Time / foundTicksPerFrame)
                                           .OrderBy(g => g.Key);

            Dictionary<Vector3Int, EmitterDetail> frameBuffer = new Dictionary<Vector3Int, EmitterDetail>();

            foreach (var group in eventsByFrame)
            {
                Debug.Log("NEW FRAME");

                Frame frame = new Frame();
                byte currentEmitterId = 0;

                foreach (var timedEvent in group)
                {
                    if (timedEvent.Event is ControlChangeEvent cc)
                    {
                        switch (cc.ControlNumber)
                        {
                            case ACTIVATE_CC: // SELECT EMITTER
                                currentEmitterId = (byte)cc.ControlValue;

                                Debug.Log("ACTIVATE_CC: " + cc.ControlValue);
                                break;

                            case PLACE_CC: // POSITION INDEX
                                byte index = (byte)cc.ControlValue;

                                // REVERSE MATH:
                                int x = index / 8;
                                int y = 7 - (index % 8);

                                // Create the emitter data
                                var emitter = new EmitterDetail
                                {
                                    Position = new Vector3Int(x, y),
                                    EmitterId = currentEmitterId
                                };

                                frameBuffer[new Vector3Int(x, y)] = emitter;

                                Debug.Log("PLACE_CC: " + emitter.Position);
                                break;

                            case REMOVE_CC: // CLEAR
                                byte removeIndex = (byte)cc.ControlValue;
                                int rx = removeIndex / 8;
                                int ry = 7 - (removeIndex % 8);
                                var removePos = new Vector3Int(rx, ry);

                                frameBuffer.Remove(removePos);

                                Debug.Log("REMOVE_CC: " + removePos);
                                break;
                        }
                    }
                }

                foreach (var kvp in frameBuffer)
                {
                    frame.Matrix[kvp.Key] = kvp.Value;
                }

                frames.Add(frame);
            }

            return frames;
        }


        public void SetBpm(int bpm)
        {
            this.bpm = bpm;
        }
    }
}
