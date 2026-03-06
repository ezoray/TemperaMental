using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Tempera.Mental.Logs;

namespace Tempera.Mental.Utils
{
    public static class MidiUtils
    {

        public static int GetBpmFromMidiFile(MidiFile midiFile)
        {
            try
            {
                var tempoMap = midiFile.GetTempoMap();
                var tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(0));

                LogMan.Log($"BPM: {60000000.0 / tempo.MicrosecondsPerQuarterNote}");
                LogMan.Log("BeatsPerMinute: " + tempo.BeatsPerMinute);

                return (int)(60000000.0 / tempo.MicrosecondsPerQuarterNote);

            }
            catch(Exception ex)
            {
                LogMan.LogError($"Unable to get BPM: {ex}");
                return 0;
            }
        }
    }
}
