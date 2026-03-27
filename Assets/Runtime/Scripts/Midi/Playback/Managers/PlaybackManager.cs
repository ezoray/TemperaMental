using System;
using System.Collections.Concurrent;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Playbacks
{
    public class PlaybackManager : MonoBehaviour
    {
        string frameNoPrefix;

        OutputDevice outputDevice;
        private Playback playback;

        volatile bool isPlaybackFinished;
        bool isLooping;

        int midiFileBpm;
        int startingFrame;

        private ConcurrentQueue<int> frameQueue;

        [SerializeField] UnityEvent<int> onPlaybackFrameChanged;
        [SerializeField] UnityEvent<PlaybackState> onPlaybackStateChanged;
        [SerializeField] UnityEvent<bool> onLoopStateChanged;

        private void OnEnable()
        {
            frameQueue = new ConcurrentQueue<int>();

            frameNoPrefix = ConfigRegistry.Midi.FrameNumberPrefix;
        }

        private void Update()
        {
            if (isPlaybackFinished)
            {
                isPlaybackFinished = false;

                playback.Finished -= OnPlaybackFinished;
                playback.EventPlayed -= OnEventPlayed;
                ResetPlayback();

                LogMan.Log("Playback finished");

                onPlaybackStateChanged?.Invoke(PlaybackState.Idle);
            }
            else
            {
                while (frameQueue.TryDequeue(out int frameNumber))
                {
                    onPlaybackFrameChanged?.Invoke(frameNumber);
                }
            }
        }

        // when playing from position not at start we need to check that user doesn't try to move to a frame before that start position
        // as playback doesn't have it (due to playback handling looping it would restart from frame 1 if it had all frames)
        // move to start position instead
        public void SeekToFrame(int seekFrame)
        {
            if (GetPlaybackState() == PlaybackState.Idle) return;

            if (seekFrame < startingFrame) seekFrame = startingFrame;

            long ticks = (seekFrame - startingFrame) * ConfigRegistry.Midi.TicksPerFrame;
            playback.MoveToTime(new MidiTimeSpan(ticks));
        }

        // To stop playback is reset (playback.Stop pauses playback)
        public void Stop()
        {
            try
            {
                PlaybackState state = GetPlaybackState();

                if (state != PlaybackState.Playing && state != PlaybackState.Paused)
                {
          //          LogMan.LogWarning("Stop playback, wrong state: " + state);
                    return;
                }

                ResetPlayback();

                LogMan.Log("Playback stopped");

                onPlaybackStateChanged?.Invoke(PlaybackState.Idle);

            }
            catch (Exception ex)
            {
                LogMan.LogError("Stop failed: " + ex);
            }
        }

        public void Pause()
        {
            try
            {
                if (GetPlaybackState() != PlaybackState.Playing)
                {
          //          LogMan.LogWarning   ("Pause playback, wrong state: " + GetPlaybackState());
                    return;
                }

                // calling stop pauses playback
                playback.Stop();

                LogMan.Log("Playback paused");

                onPlaybackStateChanged?.Invoke(PlaybackState.Paused);
            }

            catch (Exception ex)
            {
                LogMan.LogError("Pause failed: " + ex); 
            }              
        }

        // todo currently playback will happily run when output device is null, at least for cc's, but it probably shouldn't be relied upon
        public void Play(MidiFile midiFile, int startingFrame)
        {
            try
            {
                PlaybackState playbackState = GetPlaybackState();

                if (playbackState != PlaybackState.Paused)
                {
                    ResetPlayback();

                    playback = midiFile.GetPlayback(new PlaybackSettings
                    {
                        ClockSettings = new MidiClockSettings
                        {
                            CreateTickGeneratorCallback = () => new RegularPrecisionTickGenerator()
                        }
                    });

                    this.startingFrame = startingFrame;

                    LogMan.Log("StartingFrame: " + startingFrame);

                    playback.OutputDevice = outputDevice;

                    playback.Loop = isLooping;

                    playback.ErrorOccurred += OnPlaybackError;
                    playback.Finished += OnPlaybackFinished;
                    playback.EventPlayed += OnEventPlayed;

                    midiFileBpm = MidiUtils.GetBpmFromMidiFile(midiFile);
                }

                playback.Start();

                LogMan.Log("Playing...");

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

        // todo interface is disabled on playback but do check for playing
        public void SetOutputDevice(OutputDevice outputDevice)
        {
            this.outputDevice = outputDevice;
        }

        private void OnEventPlayed(object sender, MidiEventPlayedEventArgs eventArgs)
        {
            if (eventArgs.Event is MarkerEvent marker)
            {
                string text = marker.Text;

                if (text.StartsWith(frameNoPrefix))
                {
                    string numberPart = text.Replace(frameNoPrefix, "");

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
            // todo log this - cannot log here in callback
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

        public void ChangeBpm(int newBpm)
        {
            if (GetPlaybackState() == PlaybackState.Playing || GetPlaybackState() == PlaybackState.Paused)
            {
                playback.Speed = (float)newBpm / midiFileBpm;
            }
        }
    }
}