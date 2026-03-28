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
        Playback playback;

        volatile bool isPlaybackFinished;
        bool isLooping;

        int playFrame;
        long totalFrames;
        short ticksPerFrame;
        int midiFileBpm;

        private PlaybackState playbackState;
        private ConcurrentQueue<int> frameQueue;

        [SerializeField] UnityEvent<int> onPlaybackFrameChanged;
        [SerializeField] UnityEvent<PlaybackState> onPlaybackStateChanged;
        [SerializeField] UnityEvent<bool> onLoopStateChanged;

        private void OnEnable()
        {
            frameQueue = new ConcurrentQueue<int>();
            frameNoPrefix = ConfigRegistry.Midi.FrameNumberPrefix;
            playbackState = PlaybackState.Idle;
        }

        private void Update()
        {
            if (isPlaybackFinished)
            {
                isPlaybackFinished = false;
                ResetPlayback();
                LogMan.Log("Playback finished");
                SetPlaybackState(PlaybackState.Idle);
            }
            else
            {
                while (frameQueue.TryDequeue(out int frameNumber))
                {
                    onPlaybackFrameChanged?.Invoke(frameNumber);
                }
            }
        }

        public void SeekToFrame(int seekFrame)
        {
            if (playbackState == PlaybackState.Idle) return;

            playFrame = Mathf.Clamp(seekFrame, 1, (int)totalFrames);

            if (playbackState == PlaybackState.Playing)
            {
                LogMan.Log($"Playing from frame {playFrame}");
            }

            long ticks = (playFrame - 1) * ticksPerFrame;
            playback.MoveToTime(new MidiTimeSpan(ticks));
        }

        public void ChangeLoopState()
        {
            isLooping = !isLooping;

            if (playbackState != PlaybackState.Idle)
            {
                playback.Loop = isLooping;
            }

            onLoopStateChanged?.Invoke(isLooping);
        }

        public void ChangeBpm(int newBpm)
        {
            if (playbackState == PlaybackState.Playing || playbackState == PlaybackState.Paused)
            {
                playback.Speed = (float)newBpm / midiFileBpm;
            }
        }

        public void Stop()
        {
            try
            {
                if (playbackState != PlaybackState.Playing && playbackState != PlaybackState.Paused) return;

                ResetPlayback();
                LogMan.Log("Playback stopped");
                SetPlaybackState(PlaybackState.Idle);
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
                if (playbackState != PlaybackState.Playing) return;

                playback.Stop();
                LogMan.Log("Playback paused");
                SetPlaybackState(PlaybackState.Paused);
            }
            catch (Exception ex)
            {
                LogMan.LogError("Pause failed: " + ex);
            }
        }

        public void SetOutputDevice(OutputDevice outputDevice)
        {
            this.outputDevice = outputDevice;
        }

        public void Play(MidiFile midiFile, int startFrame)
        {
            try
            {
                InitPlayback(midiFile, startFrame);

                midiFileBpm = MidiUtils.GetBpmFromMidiFile(midiFile);

                LogMan.Log("Total frames " + totalFrames);

                playback.Start();
                LogMan.Log("Playing from frame " + playFrame);
                SetPlaybackState(PlaybackState.Playing);
            }
            catch (Exception ex)
            {
                LogMan.LogError("Play failed: " + ex);
                ResetPlayback();
            }
        }

        private void InitPlayback(MidiFile midiFile, int startFrame)
        {
            if (playbackState != PlaybackState.Idle)
            {
                ResetPlayback();
            }

            playback = midiFile.GetPlayback(new PlaybackSettings
            {
                ClockSettings = new MidiClockSettings
                {
                    CreateTickGeneratorCallback = () => new RegularPrecisionTickGenerator()
                }
            });

            playback.ErrorOccurred += OnPlaybackError;
            playback.Finished += OnPlaybackFinished;
            playback.EventPlayed += OnEventPlayed;
            playback.RepeatStarted += OnRepeatStarted;

            ticksPerFrame = ((TicksPerQuarterNoteTimeDivision)midiFile.TimeDivision).TicksPerQuarterNote;

            LogMan.Log("ticksPerFrame: " + ticksPerFrame);

            totalFrames = MidiUtils.GetTotalFrames(midiFile);
            playFrame = Mathf.Clamp(startFrame, 1, (int)totalFrames);

            LogMan.Log("PlayFrame: " + playFrame);

            long playTick = (playFrame - 1) * ticksPerFrame;
            playback.MoveToTime(new MidiTimeSpan(playTick));

            playback.OutputDevice = outputDevice;
            playback.Loop = isLooping;
        }

        private void OnRepeatStarted(object sender, EventArgs e)
        {
            long ticks = (playFrame - 1) * ticksPerFrame;
            playback.MoveToTime(new MidiTimeSpan(ticks));
        }

        private void OnEventPlayed(object sender, MidiEventPlayedEventArgs eventArgs)
        {
            if (eventArgs.Event is MarkerEvent marker)
            {
                string markerText = marker.Text;

                if (markerText.StartsWith(frameNoPrefix))
                {
                    string numberPart = markerText.Replace(frameNoPrefix, "");

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
            isPlaybackFinished = true;
        }

        private void SetPlaybackState(PlaybackState state)
        {
            playbackState = state;
            onPlaybackStateChanged?.Invoke(state);
        }

        private void ResetPlayback()
        {
            if (playback != null)
            {
                playback.Stop();
                playback.ErrorOccurred -= OnPlaybackError;
                playback.Finished -= OnPlaybackFinished;
                playback.EventPlayed -= OnEventPlayed;
                playback.RepeatStarted -= OnRepeatStarted;
                playback.Dispose();
                playback = null;
            }

            SetPlaybackState(PlaybackState.Idle);
        }

        private void OnDestroy()
        {
            ResetPlayback();
        }
    }
}