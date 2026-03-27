using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
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

        public void ActionOnBpmChanged(int newBpm)
        {
            playbackManager.ChangeBpm(newBpm);
        }

        public void ActionOnDeviceRemoved()
        {
            playbackManager.Stop();
        }

        public void ActionOnDeviceChanged(OutputDevice device)
        {
            playbackManager.SetOutputDevice(device);
        }

        public void OnClickChangeLoopState()    
        {
            playbackManager.ChangeLoopState();
        }

        public void OnClickStop()
        {
            playbackManager.Stop();
        }

        public void OnClickPause()
        {
            playbackManager.Pause();
        }

        public void OnClickPlay()
        {
            playbackManager.Play(midiManager.FromFramesToMidiFile(frameManager.GetFrames()),1);
        }

        public void OnClickPlayPosition()
        {
            int startingFrame = frameManager.GetCurrentFrameNumber();
            MidiFile midiFile = midiManager.FromFramesToMidiFile(frameManager.GetFramesFromCurrentPosition(), startingFrame);

            playbackManager.Play(midiFile, startingFrame);
        }
    }
}
