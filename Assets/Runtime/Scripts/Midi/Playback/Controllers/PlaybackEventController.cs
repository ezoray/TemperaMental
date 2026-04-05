using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Midi.Core;
using TemperaMental.Midi.Devices;
using UnityEngine;

namespace TemperaMental.Midi.Playbacks
{
    public class PlaybackEventController : MonoBehaviour
    {
        [SerializeField] DeviceManager deviceManager;
        [SerializeField] PlaybackManager playbackManager;
        [SerializeField] MidiManager midiManager;
        [SerializeField] FrameManager frameManager;

        // frame slider
        public void ActionOnSelectedFrameChanged(float selectedFrame) => playbackManager.SeekToFrame(Mathf.RoundToInt(selectedFrame));

        public void ActionOnBpmChanged(int newBpm) => playbackManager.ChangeBpm(newBpm);

        public void ActionOnCurrentDeviceRemoved() => playbackManager.ClearOutputDevice();

        public void ActionOnDeviceChanged(OutputDevice device) => playbackManager.SetOutputDevice(device);

        public void OnClickToggleReverse() => playbackManager.ToggleReverse();

        public void OnClickChangeLoopState() => playbackManager.ToggleLooping();

        public void OnClickStop() => playbackManager.Stop();

        public void OnClickPause()
        {
            MidiFileDetail midiFileDetail = midiManager.FromFramesToMidiFiles(frameManager.GetFrames());
            int initialFrame = frameManager.GetCurrentFrameNumber();

            playbackManager.TogglePlayPause(midiFileDetail, initialFrame);
        }

        public void OnClickPlay()
        {
            MidiFileDetail midiFileDetail = midiManager.FromFramesToMidiFiles(frameManager.GetFrames());
            int initialFrame = frameManager.GetCurrentFrameNumber();

            playbackManager.Play(midiFileDetail, initialFrame);
        }
    }
}
