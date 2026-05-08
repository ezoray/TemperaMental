using Melanchall.DryWetMidi.Multimedia;
using UnityEngine;

namespace TemperaMental.Midi.Playbacks
{
    public class PlaybackManagerEventController : MonoBehaviour
    {
        [SerializeField] PlaybackManager playbackManager;

        public void ActionOnCurrentDeviceRemoved() => playbackManager.ClearOutputDevice();

        public void ActionOnDeviceChanged(OutputDevice device) => playbackManager.SetOutputDevice(device);
    }
}
