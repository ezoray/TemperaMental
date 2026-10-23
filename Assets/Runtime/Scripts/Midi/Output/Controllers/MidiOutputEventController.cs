using Melanchall.DryWetMidi.Multimedia;
using UnityEngine;
using UnityEngine.Serialization;

namespace TemperaMental.Midi.Output
{
    public class MidiOutputEventController : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("playbackManager")] MidiOutputScheduler outputScheduler;


        public void ActionOnMidiChannelChanged(int channel) => outputScheduler.SetMidiChannel(channel);

        public void ActionOnCurrentDeviceRemoved() => outputScheduler.ClearOutputDevice();

        public void ActionOnDeviceChanged(OutputDevice device) => outputScheduler.SetOutputDevice(device);
    }
}
