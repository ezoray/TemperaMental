using System.Collections.Generic;
using System.Diagnostics;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Playbacks
{
    public class FrameSequencer : MonoBehaviour
    {
        [SerializeField] FrameManager frameManager;
        [SerializeField] FrameMidiPlayer midiPlayer;

        IReadOnlyList<Frame> frames;
       volatile bool isNewPlayFrame;
        volatile bool isPlaybackFinished;
        volatile int pendingSeekFrame;
        volatile bool isLooped;
        volatile bool isReversed;

        volatile int anchorFrame;
        volatile int playFrame;
        int bpm;
        long frameDurationTicks;

        volatile PlaybackState playbackState;
        volatile bool isPlaybackStateChanged;

        [SerializeField] UnityEvent<int> onPlaybackFrameChanged;
        [SerializeField] UnityEvent<PlaybackState> onPlaybackStateChanged;
        [SerializeField] UnityEvent<bool> onLoopStateChanged;
        [SerializeField] UnityEvent<bool> onReverseStateChanged;

        private void OnEnable()
        {
            playbackState = PlaybackState.Reset;

            isPlaybackFinished = false;
            isLooped = false;
            isReversed = false;

            bpm = ConfigRegistry.Midi.DefaultBpm;
            frameDurationTicks = (long)(60.0 / bpm * Stopwatch.Frequency);

            midiPlayer.OnFramePlaybackCompleted += ActionOnFramePlaybackCompleted;
        }

        private void Start()
        {
            frames = frameManager.GetFrames();

            LogMan.Log($"Start: frames {frames.Count} duration {frameDurationTicks}");
        }

        private void Update()
        {
            if (isNewPlayFrame)
            {
                isNewPlayFrame = false;
                onPlaybackFrameChanged?.Invoke(playFrame);
            }

            if(isPlaybackStateChanged)
            {
                LogMan.Log($"Playback {playbackState}");
                isPlaybackStateChanged = false;            
                onPlaybackStateChanged?.Invoke(playbackState);
            }

            if (isPlaybackFinished)
            {
                isPlaybackFinished = false;
                LogMan.Log("Playback finished");
            }
        }

        public void Play(int initialFrame)
        {
            int fromFrame = (playbackState == PlaybackState.Playing) ? anchorFrame : initialFrame;

            anchorFrame = fromFrame;
            LogMan.Log($"Play: fromFrame {fromFrame} anchor {anchorFrame} count: {frames.Count}");

            SetPlaybackState(PlaybackState.Playing);
            SetPlayFrame(fromFrame);
            PlayFrame();
        }

        public void TogglePlayPause(int initialFrame)
        {
            switch (playbackState)
            {
                case PlaybackState.Playing:
                    PausePlayback();
                    break;

                case PlaybackState.Paused:
                    ResumePlayback();
                    break;

                case PlaybackState.Reset:
                case PlaybackState.Stopped:
                    SetPlaybackState(PlaybackState.Playing);
                    SetPlayFrame(initialFrame);
                    PlayFrame();
                    break;

                default:
                    // no-op
                    break;
            }
        }

        public void Stop()
        {
            switch (playbackState)
            {
                case PlaybackState.Playing:
                    SetPlaybackState(PlaybackState.Stopping);
                    midiPlayer.CancelFrame();
                    break;

                case PlaybackState.Paused:
                    SetPlayFrame(anchorFrame);                    
                    SetPlaybackState(PlaybackState.Reset);
                    break;

                case PlaybackState.Stopped:
                    SetPlayFrame(anchorFrame);
                    SetPlaybackState(PlaybackState.Reset);
                    break;

                default:
                    // no-op
                    break;
            }
        }

        public void SeekToFrame(int seekFrame)
        {
            int clampedFrame = Mathf.Clamp(seekFrame, 1, frames.Count);

            switch (playbackState)
            {
                case PlaybackState.Playing:
                    pendingSeekFrame = clampedFrame;
                    SetPlaybackState(PlaybackState.Seeking);
                    midiPlayer.CancelFrame();
                    break;

                case PlaybackState.Pausing:
                case PlaybackState.Paused:
                case PlaybackState.Stopping:
                case PlaybackState.Stopped:
                case PlaybackState.Reset:
                    anchorFrame = clampedFrame;
                    SetPlayFrame(clampedFrame);
                    break;

                default:
                    // no-op
                    break;
            }
        }

        public void ToggleReverse()
        {
            isReversed = !isReversed;

            if (playbackState != PlaybackState.Reset)
            {
                anchorFrame = playFrame;
            }

            onReverseStateChanged?.Invoke(isReversed);
        }

        public void ToggleLooping()
        {
            isLooped = !isLooped;

            onLoopStateChanged?.Invoke(isLooped);
        }

        public void SetBpm(int newBpm)
        {
            bpm = newBpm;
            frameDurationTicks = (long)(60.0 / bpm * Stopwatch.Frequency);
        }

        private void PlayFrame()
        {
            midiPlayer.PlayFrame(frames[playFrame -1].GetEmitterGroups(), frameDurationTicks, true);
        }

        private void SetPlayFrame(int frame)
        {
            playFrame = Mathf.Clamp(frame, 1, frames.Count);
            isNewPlayFrame = true;
        }

        private void PausePlayback()
        {
            if (playbackState != PlaybackState.Playing) return;

            SetPlaybackState(PlaybackState.Pausing);
            midiPlayer.CancelFrame();
        }

        private void ResumePlayback()
        {
            if (playbackState != PlaybackState.Paused) return;

            SetPlaybackState(PlaybackState.Playing);
            PlayFrame();
        }

        private void SetPlaybackState(PlaybackState state)
        {
            playbackState = state;
            isPlaybackStateChanged = true;
        }

        public void ActionOnFramePlaybackCompleted()
        {
            switch (playbackState)
            {
                case PlaybackState.Playing:
                    AdvancePlayback();
                    break;

                case PlaybackState.Pausing:
                    SetPlaybackState(PlaybackState.Paused);
                    break;

                case PlaybackState.Stopping:
                    SetPlaybackState(PlaybackState.Stopped);
                    break;

                case PlaybackState.Seeking:
                    SetPlayFrame(pendingSeekFrame);
                    SetPlaybackState(PlaybackState.Playing);
                    PlayFrame();
                    break;

                default:
                    // no-op
                    break;
            }
        }

        private void AdvancePlayback()
        {
            int nextFrame = playFrame + (isReversed ? -1 : 1);

            bool reachedEnd = isReversed ? nextFrame < 1 : nextFrame > frames.Count;

            if (reachedEnd)
            {
                if (isLooped)
                {
                    SetPlayFrame(anchorFrame);
                }
                else
                {
                    SetPlaybackState(PlaybackState.Reset);
                    isPlaybackFinished = true;
                    return;
                }
            }
            else
            {
                SetPlayFrame(nextFrame);
            }

            PlayFrame();
        }

        private void OnDisable()
        {
            midiPlayer.OnFramePlaybackCompleted -= ActionOnFramePlaybackCompleted;
        }
    }
}
