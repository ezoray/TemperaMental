using System;
using System.Collections.Concurrent;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using TemperaMental.Midi.Core;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Playbacks
{
    public class PlaybackManager : MonoBehaviour
    {
        string frameNoPrefix;

        OutputDevice outputDevice;
        Playback forwardPlayback;
        Playback reversePlayback;

        volatile bool isPlaybackFinished;
        bool isLooped;
        bool isReversed;

        int anchorFrame;
        volatile int playFrame;
        int totalFrames;
        short ticksPerFrame;
        int midiFileBpm;

        PlaybackState playbackState;
        ConcurrentQueue<int> frameQueue;

        [SerializeField] UnityEvent<int> onPlaybackFrameChanged;
        [SerializeField] UnityEvent<PlaybackState> onPlaybackStateChanged;
        [SerializeField] UnityEvent<bool> onLoopStateChanged;
        [SerializeField] UnityEvent<bool> onReverseStateChanged;

        Playback ActivePlayback => isReversed ? reversePlayback : forwardPlayback;

        private void OnEnable()
        {
            frameQueue = new ConcurrentQueue<int>();
            frameNoPrefix = ConfigRegistry.Midi.FrameStartPrefix;
            playbackState = PlaybackState.Idle;

            isPlaybackFinished = false;
            isLooped = false;
            isReversed = false;
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

            while (frameQueue.TryDequeue(out int frameNumber))
            {
                onPlaybackFrameChanged?.Invoke(frameNumber);
            }
        }

        public void SeekToFrame(int seekFrame)
        {
            if (playbackState == PlaybackState.Idle) return;

            playFrame = Mathf.Clamp(seekFrame, 1, totalFrames);

            int mappedFrame = isReversed ? (totalFrames - playFrame) + 1 : playFrame;
            anchorFrame = mappedFrame;

            long ticks = (mappedFrame - 1) * ticksPerFrame;
            ActivePlayback.MoveToTime(new MidiTimeSpan(ticks));
        }

        public void ToggleReverse()
        {
            isReversed = !isReversed;

            if (playbackState != PlaybackState.Idle)
            {
                Playback outgoing = isReversed ? forwardPlayback : reversePlayback;
                Playback incoming = isReversed ? reversePlayback : forwardPlayback;

                outgoing.Stop();

                int mappedFrame = isReversed ? (totalFrames - playFrame) + 1 : playFrame;
                anchorFrame = mappedFrame;

                long ticks = (mappedFrame - 1) * ticksPerFrame;
                incoming.MoveToTime(new MidiTimeSpan(ticks));

                if (playbackState == PlaybackState.Playing)
                {
                    incoming.Start();
                }
            }

            onReverseStateChanged?.Invoke(isReversed);
        }

        public void ToggleLooping()
        {
            isLooped = !isLooped;

            if (playbackState != PlaybackState.Idle)
            {
                forwardPlayback.Loop = isLooped;
                reversePlayback.Loop = isLooped;
            }

            onLoopStateChanged?.Invoke(isLooped);
        }

        public void ChangeBpm(int newBpm)
        {
            if (playbackState == PlaybackState.Playing || playbackState == PlaybackState.Paused)
            {
                forwardPlayback.Speed = (float)newBpm / midiFileBpm;
                reversePlayback.Speed = (float)newBpm / midiFileBpm;
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

                ActivePlayback.Stop();
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

        public void Play(MidiFileDetail midiFileDetail, int initialFrame = 1)
        {
            try
            {
                InitPlaybacks(midiFileDetail, initialFrame);

                midiFileBpm = MidiUtils.GetBpmFromMidiFile(midiFileDetail.ForwardMidiFile);

                ActivePlayback.Start();

                LogMan.Log("Playing from frame " + playFrame);

                SetPlaybackState(PlaybackState.Playing);
            }
            catch (Exception ex)
            {
                LogMan.LogError("Play failed: " + ex);
                ResetPlayback();
            }
        }

        private void InitPlaybacks(MidiFileDetail midiFileDetail, int initialFrame)
        {
            if (playbackState != PlaybackState.Idle)
            {
                ResetPlayback();
            }

            forwardPlayback = CreatePlayback(midiFileDetail.ForwardMidiFile);
            reversePlayback = CreatePlayback(midiFileDetail.ReverseMidiFile);

            ticksPerFrame = ((TicksPerQuarterNoteTimeDivision)midiFileDetail.ForwardMidiFile.TimeDivision).TicksPerQuarterNote;

            totalFrames = MidiUtils.GetTotalFrames(midiFileDetail.ForwardMidiFile);
            playFrame = Mathf.Clamp(initialFrame, 1, totalFrames);

            if (isReversed)
            {
                int reversedStartFrame = (totalFrames - playFrame) + 1;
                long reverseTick = (reversedStartFrame - 1) * ticksPerFrame;
                reversePlayback.MoveToTime(new MidiTimeSpan(reverseTick));
                anchorFrame = reversedStartFrame;
            }
            else
            {
                long forwardTick = (playFrame - 1) * ticksPerFrame;
                forwardPlayback.MoveToTime(new MidiTimeSpan(forwardTick));
                anchorFrame = playFrame;
            }

            forwardPlayback.OutputDevice = outputDevice;
            reversePlayback.OutputDevice = outputDevice;

            forwardPlayback.Loop = isLooped;
            reversePlayback.Loop = isLooped;
        }

        private Playback CreatePlayback(MidiFile midiFile)
        {
            Playback playback = midiFile.GetPlayback(new PlaybackSettings
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

            return playback;
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
                        playFrame = frameNumber; 
                        frameQueue.Enqueue(frameNumber);
                    }
                }
            }
        }

        private void OnRepeatStarted(object sender, EventArgs e)
        {
            long ticks = (anchorFrame - 1) * ticksPerFrame;
            ActivePlayback.MoveToTime(new MidiTimeSpan(ticks));
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

        private void DisposePlayback(ref Playback pb)
        {
            if (pb == null) return;

            pb.Stop();
            pb.ErrorOccurred -= OnPlaybackError;
            pb.Finished -= OnPlaybackFinished;
            pb.EventPlayed -= OnEventPlayed;
            pb.RepeatStarted -= OnRepeatStarted;
            pb.Dispose();
            pb = null;
        }

        private void ResetPlayback()
        {
            DisposePlayback(ref forwardPlayback);
            DisposePlayback(ref reversePlayback);
            SetPlaybackState(PlaybackState.Idle);
        }

        private void OnDestroy()
        {
            ResetPlayback();
        }
    }
}