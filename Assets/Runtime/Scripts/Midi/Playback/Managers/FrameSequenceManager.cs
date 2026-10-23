using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Midi.Clock;
using TemperaMental.Midi.Core;
using TemperaMental.Midi.Output;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace TemperaMental.Midi.Playbacks
{
    // Sequencer: plays one frame per beat of the ClockEngine.
    // Play, resume and seek restart the clock so the chosen frame is sent straight away.
    // HandleBeat runs on the clock thread. State shared with the main thread is guarded by playbackStateLock,
    // and UI notifications are raised from Update using volatile flags.
    public class FrameSequenceManager : MonoBehaviour
    {
        [SerializeField] FrameManager frameManager;
        [SerializeField, FormerlySerializedAs("playbackManager")] MidiOutputScheduler outputScheduler;
        [SerializeField] MidiImmediateManager immediateManager;
        [SerializeField] ClockEngine clockEngine;

        volatile bool isNewPlayFrame;
        volatile bool isPlaybackFinished;
        volatile bool isPlaybackStateChanged;
        volatile bool isLooped;
        volatile bool isReversed;

        int anchorFrame;
        volatile int playFrame;

        // epoch of the clock restart that began the current playback; beats from other epochs are ignored
        long playEpoch = -1;

        volatile PlaybackState playbackState;

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
            clockEngine.OnBeat += HandleBeat;
        }

        private void Start()
        {
            onLoopStateChanged?.Invoke(isLooped);
            onReverseStateChanged?.Invoke(isReversed);
            onPlaybackStateChanged?.Invoke(playbackState);
        }

        private void Update()
        {
            if (isNewPlayFrame)
            {
                isNewPlayFrame = false;
                onPlaybackFrameChanged?.Invoke(playFrame);
            }

            if (isPlaybackStateChanged)
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
                        SetPlaybackState(PlaybackState.Paused);
                        break;

                    case PlaybackState.Paused:
                        StartPlayback();
                        break;

                    case PlaybackState.Reset:
                    case PlaybackState.Stopped:
                        SetAnchorFrame(initialFrame);
                        SetPlayFrame(initialFrame);
                        StartPlayback();
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
                        SetPlaybackState(PlaybackState.Stopped);
                        break;

                    case PlaybackState.Paused:
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
                        // restart the clock so the new frame plays immediately for a full beat
                        SetPlayFrame(newSeekFrame);
                        playEpoch = clockEngine.Restart();
                        break;

                    case PlaybackState.Paused:
                    case PlaybackState.Stopped:
                    case PlaybackState.Reset:
                        SetAnchorFrame(newSeekFrame);
                        SetPlayFrame(newSeekFrame);
                        break;
                }
            }
        }

        public void ToggleReverse()
        {
            lock (playbackStateLock)
            {
                isReversed = !isReversed;

                SetAnchorFrame(playFrame);
            }

            onReverseStateChanged?.Invoke(isReversed);
        }

        public void ToggleLooping()
        {
            isLooped = !isLooped;

            onLoopStateChanged?.Invoke(isLooped);
        }

        // clock thread
        private void HandleBeat(ClockBeat beat)
        {
            lock (playbackStateLock)
            {
                if (playbackState != PlaybackState.Playing || beat.Epoch != playEpoch) return;

                // beat 0 is the restart that started (or resumed) playback: play the current frame
                if (beat.Beat == 0)
                {
                    PlayCurrentFrame();
                }
                else
                {
                    AdvancePlayback();
                }
            }
        }

        private void StartPlayback()
        {
            SetPlaybackState(PlaybackState.Playing);
            playEpoch = clockEngine.Restart();
        }

        private void PlayCurrentFrame()
        {
            ulong[][] sequence = frameManager.PublishedSequence;
            int index = Mathf.Clamp(playFrame, 1, sequence.Length) - 1;

            outputScheduler.EnqueueFrame(sequence[index]);
        }

        private int FrameCount => frameManager.PublishedSequence.Length;

        private void SetAnchorFrame(int frame)
        {
            anchorFrame = Mathf.Clamp(frame, 1, FrameCount);
        }

        private void SetPlayFrame(int frame)
        {
            playFrame = Mathf.Clamp(frame, 1, FrameCount);
            isNewPlayFrame = true;
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

        private void AdvancePlayback()
        {
            int frameCount = FrameCount;
            int nextFrame = playFrame + (isReversed ? -1 : 1);

            bool reachedEnd = isReversed ? nextFrame < 1 : nextFrame > frameCount;

            if (reachedEnd)
            {
                if (isLooped)
                {
                    // check anchor frame isn't the same as final frame, prevents looping over single frame
                    // no emitter data would be sent anyway and static playback could be confusing
                    if (frameCount == 1 || (isReversed ? anchorFrame == 1 : anchorFrame == frameCount))
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

            PlayCurrentFrame();
        }

        private void OnDisable()
        {
            clockEngine.OnBeat -= HandleBeat;
        }
    }
}
