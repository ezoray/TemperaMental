namespace TemperaMental.Midi.Clock
{
    // where MIDI clock is sent (in Send mode) or forwarded (in Receive mode)
    public enum ClockDestination
    {
        SameAsOut = 0,
        Separate,
        None
    }
}
