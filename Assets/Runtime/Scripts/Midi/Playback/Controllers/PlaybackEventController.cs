using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Midi.Core;
using TemperaMental.Core;
using TemperaMental.Frames;
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


        public void ActionOnBpmChanged(int newBpm)
        {
            playbackManager.ChangeBpm(newBpm);
        }

        public void ActionOnDeviceRemoved()
        {
            playbackManager.Reset();
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
            playbackManager.Reset();
        }

        public void OnClickPause()
        {
            playbackManager.Pause();
        }

        public void OnClickPlay()
        {
            playbackManager.Play(midiManager.FromFramesToMidiFile(frameManager.GetFrames()));
        }

        public void OnClickPlayPosition()
        {
            int startingFrameNumber = frameManager.GetCurrentFrameNumber();
            playbackManager.Play(midiManager.FromFramesToMidiFile(frameManager.GetFramesFromCurrentPosition(), startingFrameNumber));
        }

        public void ActionOnPlaybackUiEvent(PlaybackUIEvent eventType)
        {
            switch (eventType)
            {
                case PlaybackUIEvent.PlayPosition:
                    int startingFrameNumber = frameManager.GetCurrentFrameNumber();
                    playbackManager.Play(midiManager.FromFramesToMidiFile(frameManager.GetFramesFromCurrentPosition(), startingFrameNumber));
                    break;

                case PlaybackUIEvent.Play:
                    playbackManager.Play(midiManager.FromFramesToMidiFile(frameManager.GetFrames()));
                    break;

                case PlaybackUIEvent.Pause:
                    playbackManager.Pause();              
                    break;

                case PlaybackUIEvent.Stop:
                    playbackManager.Reset();
                    break;
            }
        }
    }
}
