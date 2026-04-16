using Melanchall.DryWetMidi.Multimedia;
using UnityEngine;

namespace TemperaMental.Midi.Playbacks
{
    public class FrameMidiPlayerEventService : MonoBehaviour
    {
        [SerializeField] FrameMidiPlayer midiPlayer;


        public void ActionOnCurrentDeviceRemoved() => midiPlayer.ClearOutputDevice();

        public void ActionOnDeviceChanged(OutputDevice device) => midiPlayer.SetOutputDevice(device);
    }
}
