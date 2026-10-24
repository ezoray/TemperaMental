namespace TemperaMental.Midi.Input
{
    // one decoded action from the Tempera's Control MIDI channel
    public readonly struct TemperaControlEvent
    {
        public readonly TemperaControlKind Kind;

        // emitter colour 0–3: the one selected, or the one placed (the decoder's remembered selection)
        public readonly int EmitterId;

        // grid cell 0–63 for place and remove, -1 otherwise
        public readonly int Cell;

        // track 1–8 for ArmTrack, 0 otherwise
        public readonly int Track;

        // Stopwatch timestamp of the note's arrival
        public readonly long Timestamp;

        public TemperaControlEvent(TemperaControlKind kind, int emitterId, int cell, int track, long timestamp)
        {
            Kind = kind;
            EmitterId = emitterId;
            Cell = cell;
            Track = track;
            Timestamp = timestamp;
        }
    }
}
