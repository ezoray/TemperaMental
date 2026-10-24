using Melanchall.DryWetMidi.Multimedia;
using UnityEngine;

namespace TemperaMental.Midi.Input
{
    public class MidiInputEventController : MonoBehaviour
    {
        [SerializeField] MidiInputRouter inputRouter;
        [SerializeField] TemperaControlDecoder controlDecoder;

        public void ActionOnInputDeviceChanged(InputDevice device) => inputRouter.SetInputDevice(device);

        public void ActionOnCurrentInputDeviceRemoved() => inputRouter.ClearInputDevice();

        public void ActionOnControlChannelChanged(int channel) => controlDecoder.SetControlChannel(channel);
    }
}
