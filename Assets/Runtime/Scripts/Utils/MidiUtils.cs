using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
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

                LogMan.Log($"BPM: {MICROSECONDS_PER_MINUTE / tempo.MicrosecondsPerQuarterNote}");
                LogMan.Log("BeatsPerMinute: " + tempo.BeatsPerMinute);

                return (int)(MICROSECONDS_PER_MINUTE / tempo.MicrosecondsPerQuarterNote);

            }
            catch(Exception ex)
            {
                LogMan.LogError($"Unable to get BPM: {ex}");
                return 0;
            }
        }
    }
}
