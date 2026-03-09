using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Tempera.Mental.Core;
using Tempera.Mental.Frames;
using Tempera.Mental.Logs;
using Tempera.Mental.Midi.Devices;
using Tempera.Mental.Midi.Transforms;
using Tempera.Mental.Ui.Playbacks;
using UnityEngine;
using UnityEngine.Events;

namespace Tempera.Mental.Midi.Playbacks
{
    public class PlaybackEventController : MonoBehaviour
    {
        [SerializeField] PlaybackUiManager playbackUiManager;
        [SerializeField] DeviceManager deviceManager;
        [SerializeField] PlaybackManager playbackManager;
        [SerializeField] TransformService transformService;
        [SerializeField] FrameManager frameManager;

        [SerializeField] UnityEvent<int> onFrameChanged;


        public void OnDeviceRemoved(string deviceName)
        {
            // todo check device removed isn't the one being used
            if(deviceName.Equals(playbackManager.OutputDeviceName))
            {
                playbackManager.TryStop();
            }
        }

        public void OnDeviceChanged(string deviceName)
        {
            LogMan.Log("OnDeviceChanged: " + deviceName);

            if(deviceManager.TryGetOutputDevice(deviceName, out var outputDevice))
            {
                playbackManager.SetOutputDevice(outputDevice as OutputDevice);
            }
        }

        public void ActionOnFrameChanged(int frame)
        {
            LogMan.Log("ActionOnFrameChanged: " + frame);

            onFrameChanged?.Invoke(frame);
        }

        public void ActionOnLoopStateChanged(bool isLooping)
        {
            playbackManager.SetLoopState(isLooping);
        }

        public void ActionOnPlaybackFinished()
        {
            LogMan.Log("ActionOnPlaybackFinished");
            playbackUiManager.SetPlaybackUiState(PlaybackFlags.Stopped);
        }

        public void ActionOnPlaybackUiEvent(PlaybackEventType eventType)
        {
            PlaybackState playbackState = playbackManager.GetPlaybackState();

            LogMan.Log($"ActionOnPlaybackUiEvent eventType: {eventType} playbackState: {playbackState}");

            switch (eventType)
            {
                case PlaybackEventType.PlayPosition:
                    if (playbackState == PlaybackState.Reset)
                    {
                        int startingFrameNumber = frameManager.GetCurrentFrameNumber();
                        MidiFile midiFile = transformService.FromFramesToMidiFile(frameManager.GetFramesFromCurrentPosition(), startingFrameNumber);
                        playbackManager.TryPlay(midiFile);
                        playbackUiManager.SetPlaybackUiState(PlaybackFlags.Playing);
                    }
                    else if (playbackState == PlaybackState.Paused)
                    {
                        if (playbackManager.TryResumePlay())
                        {
                            playbackUiManager.SetPlaybackUiState(PlaybackFlags.Playing);
                        }
                    }
                    else
                    {
                        playbackUiManager.SetPlaybackUiState(PlaybackFlags.Stopped);
                    }
                    break;

                case PlaybackEventType.Play:
                    if(playbackState == PlaybackState.Reset)
                    {
                        MidiFile midiFile = transformService.FromFramesToMidiFile(frameManager.GetFrames());
                        playbackManager.TryPlay(midiFile);
                        playbackUiManager.SetPlaybackUiState(PlaybackFlags.Playing);
                    }
                    else if(playbackState == PlaybackState.Paused)
                    {
                        if(playbackManager.TryResumePlay())
                        {
                            playbackUiManager.SetPlaybackUiState(PlaybackFlags.Playing);
                        }
                    }
                    else
                    {
                        playbackUiManager.SetPlaybackUiState(PlaybackFlags.Stopped);
                    }
                    break;

                case PlaybackEventType.Pause:
                    if (playbackManager.TryPause())
                    {
                        playbackUiManager.SetPlaybackUiState(PlaybackFlags.Paused);
                    }
                    else
                    {
                        playbackUiManager.SetPlaybackUiState(PlaybackFlags.Stopped);
                    }
                    break;

                case PlaybackEventType.Stop:
                    playbackManager.TryStop();

                    playbackUiManager.SetPlaybackUiState(PlaybackFlags.Stopped);
                    break;
            }
        }

        public void OnSelectBpm(int bpm)
        {

        }
    }
}
