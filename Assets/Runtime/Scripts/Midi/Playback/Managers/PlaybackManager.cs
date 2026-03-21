using System;
using System.Collections.Concurrent;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Tempera.Mental.Core;
using Tempera.Mental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace Tempera.Mental.Midi.Playbacks
{
    public class PlaybackManager : MonoBehaviour
    {
        const string FRAME_NO_PREFIX = "FRAME_NO_";

        OutputDevice outputDevice;
        string outputDeviceName;
        private Playback playback;

        volatile bool isPlaybackFinished;
        bool isLooping;

        private ConcurrentQueue<int> frameQueue;

        [SerializeField] UnityEvent<int> onFrameChanged;
        [SerializeField] UnityEvent<PlaybackState> onPlaybackStateChanged;
        [SerializeField] UnityEvent<bool> onLoopStateChanged;

        private void OnEnable()
        {
            frameQueue = new ConcurrentQueue<int>();
        }

        private void Update()
        {
            if (isPlaybackFinished)
            {
                isPlaybackFinished = false;

                playback.Finished -= OnPlaybackFinished;
                playback.EventPlayed -= OnEventPlayed;
                ResetPlayback();

                onPlaybackStateChanged?.Invoke(PlaybackState.Idle);
            }
            else
            {
                while (frameQueue.TryDequeue(out int frameNumber))
                {
                    onFrameChanged?.Invoke(frameNumber);
                }
            }
        }

        public void Reset()
        {
            try
            {
                PlaybackState state = GetPlaybackState();

                if (state != PlaybackState.Playing && state != PlaybackState.Paused)
                {
                    LogMan.LogWarning("Reset playback, wrong state: " + state);
                    return;
                }

                ResetPlayback();

                onPlaybackStateChanged?.Invoke(PlaybackState.Idle);

            }
            catch (Exception ex)
            {
                LogMan.LogError("Reset failed: " + ex);
            }
        }

        public void Pause()
        {
            try
            {
                if (GetPlaybackState() != PlaybackState.Playing)
                {
                    LogMan.LogError("Pause playback, wrong state: " + GetPlaybackState());
                    return;
                }

                // calling stop pauses playback
                playback.Stop();

                onPlaybackStateChanged?.Invoke(PlaybackState.Paused);
            }

            catch (Exception ex)
            {
                LogMan.LogError("Pause failed: " + ex); 
            }              
        }

        public void Play(MidiFile midiFile)
        {
            try
            {
                PlaybackState playbackState = GetPlaybackState();

                if (playbackState != PlaybackState.Paused)
                {
                    ResetPlayback();

                    playback = midiFile.GetPlayback();
                    playback.OutputDevice = outputDevice;

                    playback.Loop = isLooping;

                    playback.ErrorOccurred += OnPlaybackError;
                    playback.Finished += OnPlaybackFinished;
                    playback.EventPlayed += OnEventPlayed;
                }

                playback.Start();

                onPlaybackStateChanged?.Invoke(PlaybackState.Playing);
            }
            catch (Exception ex)
            {
                LogMan.LogError("Play failed: " + ex);
                ResetPlayback();
            }
        }

        public void ChangeLoopState()
        {
            isLooping = !isLooping;

            if (GetPlaybackState() != PlaybackState.Idle)
            {
                playback.Loop = isLooping;
            }

            onLoopStateChanged?.Invoke(isLooping);
        }

        public void SetOutputDevice(OutputDevice outputDevice)
        {
            this.outputDevice = outputDevice;
            outputDeviceName = outputDevice.Name;
            outputDevice.PrepareForEventsSending();
        }

        private void OnEventPlayed(object sender, MidiEventPlayedEventArgs eventArgs)
        {
            if (eventArgs.Event is MarkerEvent marker)
            {
                string text = marker.Text;

                if (text.StartsWith(FRAME_NO_PREFIX))
                {
                    string numberPart = text.Replace(FRAME_NO_PREFIX, "");

                    if (int.TryParse(numberPart, out var frameNumber))
                    {
                        frameQueue.Enqueue(frameNumber);
                    }
                }
            }
        }

        private void OnPlaybackFinished(object sender, EventArgs e)
        {
            isPlaybackFinished = true;
        }

        private void OnPlaybackError(object sender, PlaybackErrorOccurredEventArgs e)
        {
            LogMan.LogError($"Playback error: {e.Site}, {e.Exception.Message}");

            isPlaybackFinished = true;
        }

        private PlaybackState GetPlaybackState()
        {
            if (playback == null || !playback.IsRunning)
            {
                long tick = playback?.GetCurrentTime<MidiTimeSpan>().TimeSpan ?? 0;

                return tick > 0 ? PlaybackState.Paused : PlaybackState.Idle;
            }

            return PlaybackState.Playing;
        }

        private void ResetPlayback()
        {
            if (playback != null)
            {
                playback.Stop();
                playback.Dispose();
                playback = null;
            }
        }

        private void OnDestroy()
        {
            ResetPlayback();
        }

        public string OutputDeviceName { get => outputDeviceName; }
    }
}