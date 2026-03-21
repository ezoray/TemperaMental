using Melanchall.DryWetMidi.Multimedia;
using Tempera.Mental.Core;
using Tempera.Mental.Frames;
using Tempera.Mental.Midi.Devices;
using Tempera.Mental.Midi.Transforms;
using UnityEngine;

namespace Tempera.Mental.Midi.Playbacks
{
    public class PlaybackEventController : MonoBehaviour
    {
        [SerializeField] DeviceManager deviceManager;
        [SerializeField] PlaybackManager playbackManager;
        [SerializeField] TransformService transformService;
        [SerializeField] FrameManager frameManager;

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
            playbackManager.Play(transformService.FromFramesToMidiFile(frameManager.GetFrames()));
        }

        public void OnClickPlayPosition()
        {
            int startingFrameNumber = frameManager.GetCurrentFrameNumber();
            playbackManager.Play(transformService.FromFramesToMidiFile(frameManager.GetFramesFromCurrentPosition(), startingFrameNumber));
        }

        public void ActionOnPlaybackUiEvent(PlaybackUIEvent eventType)
        {
            switch (eventType)
            {
                case PlaybackUIEvent.PlayPosition:
                    int startingFrameNumber = frameManager.GetCurrentFrameNumber();
                    playbackManager.Play(transformService.FromFramesToMidiFile(frameManager.GetFramesFromCurrentPosition(), startingFrameNumber));
                    break;

                case PlaybackUIEvent.Play:
                    playbackManager.Play(transformService.FromFramesToMidiFile(frameManager.GetFrames()));
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
