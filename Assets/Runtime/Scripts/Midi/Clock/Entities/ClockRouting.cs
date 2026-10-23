using Melanchall.DryWetMidi.Multimedia;

namespace TemperaMental.Midi.Clock
{
    // which devices clock comes from and goes to, worked out by the DeviceManager from the Clock from / Clock to choices
    public readonly struct ClockRouting
    {
        // a separately opened Clock from device; null when clock comes from the In device
        public readonly InputDevice SeparateInput;

        public readonly ClockDestination Destination;

        // a separately opened Clock to device, when Destination is Separate
        public readonly OutputDevice SeparateOutput;

        // false when clock would be forwarded back to the device it comes from
        public readonly bool IsForwardingAllowed;

        public ClockRouting(InputDevice separateInput, ClockDestination destination, OutputDevice separateOutput, bool isForwardingAllowed)
        {
            SeparateInput = separateInput;
            Destination = destination;
            SeparateOutput = separateOutput;
            IsForwardingAllowed = isForwardingAllowed;
        }
    }
}
