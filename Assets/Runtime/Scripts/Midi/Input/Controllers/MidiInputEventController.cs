using Melanchall.DryWetMidi.Multimedia;
using UnityEngine;

namespace TemperaMental.Midi.Input
{
    public class MidiInputEventController : MonoBehaviour
    {
        [SerializeField] MidiInputRouter inputRouter;

        public void ActionOnInputDeviceChanged(InputDevice device) => inputRouter.SetInputDevice(device);

        public void ActionOnCurrentInputDeviceRemoved() => inputRouter.ClearInputDevice();
    }
}
