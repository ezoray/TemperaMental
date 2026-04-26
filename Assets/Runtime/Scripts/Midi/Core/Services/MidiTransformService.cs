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
            for (int i = 0; i < sourceFrames.Count; i++)
            {
                long frameTick = i * ticksPerFrame;

                int frameNumber = isReversed ? sourceFrames.Count - i : i + 1;

                frameTick = WriteFrameStart(manager, frameTick, frameNumber);

                // clear all emitters at the start of every frame so each frame is absolute
                // this allows seeking to any frame without depending upon previous frame state
                frameTick = ClearAllEmitters(manager, frameTick);

                WriteAllEmitters(manager, frameTick, sourceFrames[i]);
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

        // write emitters in turn by type
        private void WriteAllEmitters(TimedObjectsManager<TimedEvent> manager, long tick, Frame frame)
        {
            ulong[] emitterGroups = frame.GetEmitterGroups();

            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                ulong group = emitterGroups[emitterId];

                if (group == 0) continue;

                tick = WriteActivateEmitter(manager, tick, emitterId);

                for (byte pos = 0; pos < gridSize; pos++)
                {
                    if ((group & (1UL << pos)) != 0)
                    {
                        tick = WriteEmitterEvent(manager, tick, placeCC, pos);
                    }
                }
            }
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

                    // each frame is now absolute so start fresh rather than copying previous
                    currentFrame = new Frame(gridWidth, gridHeight);

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
                    currentFrame.AddEmitter(new EmitterDetail(EmitterUtils.IndexToPosition((byte)cc.ControlValue), currentEmitterId));
                }
                else if (controlNumber == removeCC)
                {
                    // clear messages can be ignored on load as each frame starts fresh
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