namespace TemperaMental.Core
{
    public enum ClockMode
    {
        // internal clock, no MIDI clock sent
        Internal = 0,

        // internal clock, MIDI clock sent to the output device
        Send = 1,

        // follow MIDI clock from an input device (not implemented yet)
        Receive = 2
    }
}
