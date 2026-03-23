using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using TemperaMental.Applications.Config;
using TemperaMental.Logs;

namespace TemperaMental.Utils
{
    public static class MidiUtils
    {
        const int MICROSECONDS_PER_MINUTE = 60_000_000;

        public static int GetBpmFromMidiFile(MidiFile midiFile)
        {
            try
            {
                var tempoMap = midiFile.GetTempoMap();
                var tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(0));
                int bpm = (int)Math.Round(tempo.BeatsPerMinute);
                LogMan.Log($"BPM: {bpm}");
                return bpm;
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Unable to get BPM: {ex}");
                return ConfigRegistry.Midi.DefaultBpm;
            }
        }
    }
}
