namespace TemperaMental.Midi.Input
{
    // what a note on the Tempera's Control MIDI channel means
    public enum TemperaControlKind
    {
        SelectEmitter,  // notes 96–99
        PlaceEmitter,   // Note On 0–91 (track * 12 + row)
        RemoveEmitter,  // Note Off 0–91
        ArmTrack        // notes 100–107
    }
}
