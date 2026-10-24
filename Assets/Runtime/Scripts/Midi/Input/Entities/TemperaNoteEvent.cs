namespace TemperaMental.Midi.Input
{
    // a note played on the Tempera (its Overlay Keyboard, with Send MIDI notes on), on any channel but the Control channel
    public readonly struct TemperaNoteEvent
    {
        public readonly bool IsOn;
        public readonly int Channel;    // 0–15
        public readonly int Note;       // 0–127
        public readonly int Velocity;   // 1–127 for Note On

        // Stopwatch timestamp of the note's arrival
        public readonly long Timestamp;

        public TemperaNoteEvent(bool isOn, int channel, int note, int velocity, long timestamp)
        {
            IsOn = isOn;
            Channel = channel;
            Note = note;
            Velocity = velocity;
            Timestamp = timestamp;
        }
    }
}
