using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using TemperaMental.Applications.Config;
using TemperaMental.Logs;

namespace TemperaMental.Utils
{
    public static class MidiUtils
    {
        public static int GetBpmFromMidiFile(MidiFile midiFile)
        {
            try
            {
                var tempoMap = midiFile.GetTempoMap();
                var tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(0));
                int bpm = (int)Math.Round(tempo.BeatsPerMinute);
                return bpm;
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Unable to get BPM: {ex}");
                return ConfigRegistry.Midi.DefaultBpm;
            }
        }

        public static int GetTotalFrames(MidiFile midiFile)
        {
            short ticksPerQuarterNote = ((TicksPerQuarterNoteTimeDivision)midiFile.TimeDivision).TicksPerQuarterNote;

            MidiTimeSpan duration = midiFile.GetDuration<MidiTimeSpan>();
            long lastTick = (long)duration;

            return (int)(lastTick / ticksPerQuarterNote);
        }
    }
}
