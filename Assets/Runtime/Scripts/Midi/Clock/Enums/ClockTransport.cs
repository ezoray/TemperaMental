namespace TemperaMental.Midi.Clock
{
    // MIDI transport message sent alongside a clock restart or on its own, or received from a clock source
    public enum ClockTransport
    {
        None = 0,
        Start,
        Continue,
        Stop,
        SongPosition
    }
}
