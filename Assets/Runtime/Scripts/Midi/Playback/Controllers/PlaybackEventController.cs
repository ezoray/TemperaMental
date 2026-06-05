using Melanchall.DryWetMidi.Multimedia;
using UnityEngine;

namespace TemperaMental.Midi.Playbacks
{
    public class PlaybackManagerEventController : MonoBehaviour
    {
        [SerializeField] PlaybackManager playbackManager;


        public void ActionOnMidiChannelChanged(int channel) => playbackManager.SetMidiChannel(channel);

        public void ActionOnCurrentDeviceRemoved() => playbackManager.ClearOutputDevice();

        public void ActionOnDeviceChanged(OutputDevice device) => playbackManager.SetOutputDevice(device);
    }
}
