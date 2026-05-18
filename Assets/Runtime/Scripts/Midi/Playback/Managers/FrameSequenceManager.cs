using System.Collections.Generic;
using System.Diagnostics;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Midi.Core;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Playbacks
{
    public class FrameSequenceManager : MonoBehaviour
    {
        [SerializeField] FrameManager frameManager;
        [SerializeField] PlaybackManager playbackManager;
        [SerializeField] MidiImmediateManager immediateManager;

        IReadOnlyList<Frame> frames;
        volatile bool isNewPlayFrame;
        volatile bool isPlaybackFinished;
        volatile int seekFrame;
        bool isLooped;
        bool isReversed;

        int anchorFrame;
        volatile int playFrame;
        int bpm;
        long frameDurationTicks;

        volatile PlaybackState playbackState;
        volatile TransientState transientState;
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
                        if (playbackManager.isPlaybackActive) return;
                        SetPlaybackState(PlaybackState.Playing);
                        SetAnchorFrame(initialFrame);
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
                        transientState = TransientState.Stopping;
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

        public void SeekToFrame(int newSeekFrame)
        {
            lock (playbackStateLock)
            {
                switch (playbackState)
                {
                    case PlaybackState.Playing:
                        SetSeekFrame(newSeekFrame);
                        transientState = TransientState.Seeking;
                        playbackManager.CancelFrame();
                        break;

                    case PlaybackState.Paused:
                    case PlaybackState.Stopped:
                    case PlaybackState.Reset:
                        SetAnchorFrame(newSeekFrame);
                        SetPlayFrame(newSeekFrame);
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

            SetAnchorFrame(playFrame);

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
                // failure for the sequencer to send due to already sending a frame shouldn't happen unless
                // there's a collision with an immediate frame which 'may' be possible
                // we need an exit plan otherwise the app gets stuck in limbo state waiting for a playback callback that never arrives
                transientState = TransientState.Stopping;
                ActionOnFramePlaybackCompleted();
            }
        }

        private void SetSeekFrame(int frame)
        {
            seekFrame = Mathf.Clamp(frame, 1, frames.Count);
        }

        private void SetAnchorFrame(int frame)
        {
            anchorFrame = Mathf.Clamp(frame, 1, frames.Count);
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

                transientState = TransientState.Pausing;
                playbackManager.CancelFrame();
            }
        }

        private void ResumePlayback()
        {
            if (playbackState != PlaybackState.Paused) return;
            if (playbackManager.isPlaybackActive) return;

            SetPlaybackState(PlaybackState.Playing);
            PlayFrame();
        }

        private void SetPlaybackState(PlaybackState state)
        {
            if (playbackState != state)
            {
                playbackState = state;
                immediateManager.SetPlaybackState(state);

                isPlaybackStateChanged = true;
            }
        }

        private void ActionOnFramePlaybackCompleted()
        {
            lock (playbackStateLock)
            {
                switch (transientState)
                {
                    case TransientState.Pausing:
                        transientState = TransientState.None;
                        SetPlaybackState(PlaybackState.Paused);
                        break;

                    case TransientState.Stopping:
                        transientState = TransientState.None;
                        SetPlaybackState(PlaybackState.Stopped);
                        break;

                    case TransientState.Seeking:
                        transientState = TransientState.None;
                        SetPlayFrame(seekFrame);
                        SetPlaybackState(PlaybackState.Playing);
                        PlayFrame();
                        break;

                    case TransientState.None:
                        AdvancePlayback();
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
                    // check anchor frame isn't the same as final frame, prevents looping over single frame
                    // no emitter data would be sent anyway and static playback could be confusing
                    if (frames.Count == 1 || (isReversed ? anchorFrame == 1 : anchorFrame == frames.Count))
                    {
                        SetPlaybackState(PlaybackState.Reset);
                        isPlaybackFinished = true;
                        return;
                    }
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
