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
        const int ACTIVATE_CC = 10;
        const int PLACE_CC = 11;
        const int REMOVE_CC = 12;
        const byte BLUE = 0;
        const byte RED = 1;
        const byte YELLOW = 2;
        const byte GREEN = 3;
        const int TICKS_PER_FRAME = 480;

        const string FRAME_NO = "FRAME_NO_";
        const string END_OF_FRAME = "FRAME_END";

        int bpm = 400;

        public MidiFile FromFramesToMidiFile(IReadOnlyList<Frame> sourceFrames, int startFrame = 1)
        {
            MidiFile midiFile = new MidiFile();
            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)TICKS_PER_FRAME);

            TrackChunk trackChunk = new TrackChunk();

            long microsecondsPerQuarterNote = MICROSECONDS_PER_MINUTE / bpm;
            trackChunk.Events.Add(new SetTempoEvent(microsecondsPerQuarterNote));

            using (TimedObjectsManager<TimedEvent> manager = trackChunk.ManageTimedEvents())
            {
                // Track previous frame state, used to leave emitters that havent changed
                Dictionary<byte, HashSet<byte>> previousFrameGroups = new Dictionary<byte, HashSet<byte>>()
                {
                    { BLUE, new HashSet<byte>() },
                    { RED, new HashSet<byte>() },
                    { YELLOW, new HashSet<byte>() },
                    { GREEN, new HashSet<byte>() }
                };

                // frame loop
                for (int i = 0; i < sourceFrames.Count; i++)
                {
             //       LogMan.Log("Frame: " + i);

                    long frameTick = i * TICKS_PER_FRAME;
                    long frameEndTick = (i + 1) * TICKS_PER_FRAME;

                    // add a start of new frame marker into midi so we can detect this on playback to switch UI frames
                    manager.Objects.Add(new TimedEvent(new MarkerEvent($"{FRAME_NO}{startFrame + i}"), frameTick++));

                    // clear any placed emitters before first frame drawing
                    if (i == 0)
                    {
                        frameTick = ClearAllEmitters(manager, frameTick);
                    }

                    // build current frame emitter positions
                    Dictionary<byte, HashSet<byte>> currentFrameGroups = GroupByEmitter(sourceFrames[i]);

                    // remove emitters no longer active
                    for (byte emitter = BLUE; emitter <= GREEN; emitter++)
                    {
                        HashSet<byte> prevPixels = previousFrameGroups[emitter];
                        HashSet<byte> currPixels = currentFrameGroups[emitter];

                        List<byte> toRemove = prevPixels.Except(currPixels).ToList();
                        if (toRemove.Count == 0) continue;

                        // select emitter once
                        manager.Objects.Add(new TimedEvent(
                            new ControlChangeEvent((SevenBitNumber)ACTIVATE_CC, (SevenBitNumber)emitter), frameTick++));

                        foreach (var pos in toRemove)
                        {
                            manager.Objects.Add(new TimedEvent(
                                new ControlChangeEvent((SevenBitNumber)REMOVE_CC, (SevenBitNumber)pos), frameTick++));
                        }
                    }

                    // place new or changed emitters
                    for (byte emitter = BLUE; emitter <= GREEN; emitter++)
                    {
                        HashSet<byte> prevPixels = previousFrameGroups[emitter];
                        HashSet<byte> currPixels = currentFrameGroups[emitter];

                        List<byte> toAdd = currPixels.Except(prevPixels).ToList();
                        if (toAdd.Count == 0) continue;

                        // Select emitter once
                        manager.Objects.Add(new TimedEvent(
                            new ControlChangeEvent((SevenBitNumber)ACTIVATE_CC, (SevenBitNumber)emitter), frameTick++));

                 //       LogMan.Log("Active Emitter Tick: " + (frameTick - 1));

                        foreach (var pos in toAdd)
                        {
                            manager.Objects.Add(new TimedEvent(
                                new ControlChangeEvent((SevenBitNumber)PLACE_CC, (SevenBitNumber)pos), frameTick++));

              //              LogMan.Log($"Place Emitter Pos: {pos} Tick: " + (frameTick - 1));
                        }
                    }

                    manager.Objects.Add(new TimedEvent(new MarkerEvent(END_OF_FRAME), frameEndTick));

                    // set current frame as previous frame ready for next iteration
                    previousFrameGroups = currentFrameGroups.ToDictionary(kvp => kvp.Key, kvp => new HashSet<byte>(kvp.Value));
                }
            }

            midiFile.Chunks.Add(trackChunk);
      //      midiFile.ReplaceTempoMap(TempoMap.Create(Tempo.FromBeatsPerMinute(bpm)));

            return midiFile;
        }

        private long ClearAllEmitters(TimedObjectsManager<TimedEvent> manager, long frameTickPosition)
        {
            for (int i = BLUE; i <= GREEN; i++)
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
            // key is emitter id (0-3), value is set of grid indexes (0-63)
            Dictionary<byte, HashSet<byte>> groups = new Dictionary<byte, HashSet<byte>>();

            // initialize empty sets for all 4 emitters
            for (byte emitterId = BLUE; emitterId <= GREEN; emitterId++)
                groups[emitterId] = new HashSet<byte>();

            foreach (var emitterDetail in frame.Matrix.Values)
            {
                byte id = (byte)emitterDetail.EmitterId;

                // adjust position for correct cc value
                // todo might be better to do this on creation rather than tilemap convenience as it is now
                int flippedY = 7 - (int)emitterDetail.Position.y;
                byte index = (byte)((emitterDetail.Position.x * 8) + flippedY);

                groups[id].Add(index); 
            }

            return groups;
        }


        public List<Frame> FromMidiFileToFrames(MidiFile midiFile)
        {
            List<Frame> frames = new List<Frame>();

            // Get all timed events and group them by their "frame" time
            ICollection<TimedEvent> timedEvents = midiFile.GetTimedEvents();

            short tpqn = (midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision)?.TicksPerQuarterNote ?? TICKS_PER_FRAME;

            LogMan.Log("TPQN: " + tpqn);

            // Compute ticks per frame (adjust 1 quarter note per frame here if needed)
            long foundTicksPerFrame = tpqn * 1;

            // We group events by their start tick (e.g., 0, 96, 192...)
            var eventsByFrame = timedEvents.GroupBy(e => e.Time / foundTicksPerFrame).OrderBy(g => g.Key);

            Dictionary<Vector2Int, EmitterDetail> frameBuffer = new Dictionary<Vector2Int, EmitterDetail>();

            foreach (var group in eventsByFrame)
            {
                LogMan.Log("NEW FRAME");

                Frame frame = new Frame();
                byte currentEmitterId = 0;

                foreach (var timedEvent in group)
                {
                    if (timedEvent.Event is ControlChangeEvent cc)
                    {
                        switch (cc.ControlNumber)
                        {
                            case ACTIVATE_CC: // select emitter
                                currentEmitterId = (byte)cc.ControlValue;

                                LogMan.Log("ACTIVATE_CC: " + cc.ControlValue);
                                break;

                            case PLACE_CC: // position index
                                byte index = (byte)cc.ControlValue;

                                // reverse math
                                int x = index / 8;
                                int y = 7 - (index % 8);

                                // create the emitter data
                                var emitter = new EmitterDetail
                                {
                                    Position = new Vector2Int(x, y),
                                    EmitterId = currentEmitterId
                                };

                                frameBuffer[new Vector2Int(x, y)] = emitter;

                                LogMan.Log("PLACE_CC: " + emitter.Position);
                                break;

                            case REMOVE_CC: // CLEAR
                                byte removeIndex = (byte)cc.ControlValue;
                                int rx = removeIndex / 8;
                                int ry = 7 - (removeIndex % 8);
                                var removePos = new Vector2Int(rx, ry);

                                frameBuffer.Remove(removePos);

                                LogMan.Log("REMOVE_CC: " + removePos);
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
