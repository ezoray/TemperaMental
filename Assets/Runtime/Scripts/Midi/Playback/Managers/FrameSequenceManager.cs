using System.Collections.Generic;
using System.Diagnostics;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Playbacks
{
    public class FrameSequenceManager : MonoBehaviour
    {
        [SerializeField] FrameManager frameManager;
        [SerializeField] PlaybackManager playbackManager;

        IReadOnlyList<Frame> frames;
        volatile bool isNewPlayFrame;
        volatile bool isPlaybackFinished;
        volatile int pendingSeekFrame;
        bool isLooped;
        bool isReversed;

        int anchorFrame;
        volatile int playFrame;
        int bpm;
        long frameDurationTicks;

        PlaybackState playbackState;
        volatile bool isPlaybackStateChanged;

        readonly object playbackStateLock = new object();

        [SerializeField] UnityEvent<int> onPlaybackFrameChanged;
        [SerializeField] UnityEvent<PlaybackState> onPlaybackStateChanged;
        [SerializeField] UnityEvent<bool> onLoopStateChanged;
        [SerializeField] UnityEvent<bool> onReverseStateChanged;


        private void Awake()
        {
            playbackState = PlaybackState.Reset;
        }


        private void OnEnable()
        {
            playbackManager.OnFramePlaybackCompleted += ActionOnFramePlaybackCompleted;
        }

        private void Start()
        {
            onLoopStateChanged?.Invoke(isLooped);
            onReverseStateChanged?.Invoke(isReversed);
            onPlaybackStateChanged?.Invoke(playbackState);
            frames = frameManager.GetFrames();
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
                LogMan.Log("Playback Finished");
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
            lock (playbackStateLock)
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
        }

        public void Stop()
        {
            lock (playbackStateLock)
            {
                switch (playbackState)
                {
                    case PlaybackState.Playing:
                        SetPlaybackState(PlaybackState.Stopping);
                        playbackManager.CancelFrame();
                        break;

                    case PlaybackState.Paused:
                        SetPlayFrame(anchorFrame);
                        SetPlaybackState(PlaybackState.Reset);
                        break;

                    case PlaybackState.Stopped:
                        SetPlayFrame(anchorFrame);
                        SetPlaybackState(PlaybackState.Reset);
                        break;

                    case PlaybackState.Reset:
                        if (playFrame != anchorFrame)
                        {
                            SetPlayFrame(anchorFrame);
                            SetPlaybackState(PlaybackState.Reset);
                        }
                        break;

                    default:
                        // no-op
                        break;
                }
            }
        }

        public void SeekToFrame(int seekFrame)
        {
            int clampedFrame = Mathf.Clamp(seekFrame, 1, frames.Count);

            lock (playbackStateLock)
            {
                switch (playbackState)
                {
                    case PlaybackState.Playing:
                        pendingSeekFrame = clampedFrame;
                        SetPlaybackState(PlaybackState.Seeking);
                        playbackManager.CancelFrame();
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
        }

        public void ToggleReverse()
        {
            isReversed = !isReversed;

            anchorFrame = playFrame;

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
            playbackManager.NotifyDurationChanged(frameDurationTicks);
        }

        private void PlayFrame()
        {
            bool isSent = playbackManager.PlayFrame(frames[playFrame -1].GetEmitterGroups(), frameDurationTicks, true);

            if (!isSent)
            {
                LogMan.LogWarning($"PlayFrame dropped — frame {playFrame} could not be sent, device busy");
            }
        }

        private void SetPlayFrame(int frame)
        {
            playFrame = Mathf.Clamp(frame, 1, frames.Count);
            isNewPlayFrame = true;
        }

        private void PausePlayback()
        {
            lock (playbackStateLock)
            {
                if (playbackState != PlaybackState.Playing) return;

                SetPlaybackState(PlaybackState.Pausing);
                playbackManager.CancelFrame();
            }
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

        private void ActionOnFramePlaybackCompleted()
        {
            lock (playbackStateLock)
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
            playbackManager.OnFramePlaybackCompleted -= ActionOnFramePlaybackCompleted;
        }
    }
}
