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
            clockEngine.OnExternalTransport += HandleExternalTransport;
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
                        clockEngine.SendTransport(ClockTransport.Stop);
                        break;

                    case PlaybackState.Paused:
                        StartPlayback(ClockTransport.Continue);
                        break;

                    case PlaybackState.Reset:
                    case PlaybackState.Stopped:
                        SetAnchorFrame(initialFrame);
                        SetPlayFrame(initialFrame);
                        StartPlayback(ClockTransport.Start);
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
                        clockEngine.SendTransport(ClockTransport.Stop);
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
                        // restart the clock so the new frame plays immediately for a full beat;
                        // Start realigns the receiving device's beat with the restart
                        SetPlayFrame(newSeekFrame);
                        playEpoch = clockEngine.Restart(ClockTransport.Start);
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

        // clock thread: transport from the clock source while following external clock.
        // Start plays from the current frame (or restarts from the anchor if already playing), Stop pauses,
        // Continue resumes, Song Position moves to the frame for that beat. The clock is already running,
        // so nothing here restarts it; the epoch says which beats belong to this playback
        private void HandleExternalTransport(ClockTransport transport, long epoch, int songBeat)
        {
            lock (playbackStateLock)
            {
                switch (transport)
                {
                    case ClockTransport.Start:
                        int startFrame = playbackState == PlaybackState.Playing ? anchorFrame : frameManager.GetCurrentFrameNumber();
                        SetAnchorFrame(startFrame);
                        SetPlayFrame(startFrame);
                        SetPlaybackState(PlaybackState.Playing);
                        playEpoch = epoch;
                        break;

                    case ClockTransport.Continue:
                        // carries on from the sequencer's position: where it paused, or where a Song Position put it
                        if (playbackState != PlaybackState.Playing && playbackState != PlaybackState.Paused)
                        {
                            SetAnchorFrame(playFrame);
                        }

                        SetPlaybackState(PlaybackState.Playing);
                        playEpoch = epoch;
                        break;

                    case ClockTransport.Stop:
                        if (playbackState == PlaybackState.Playing)
                            SetPlaybackState(PlaybackState.Paused);
                        break;

                    case ClockTransport.SongPosition:
                        if (playbackState != PlaybackState.Playing)
                            SetAnchorFrame(songBeat + 1);

                        SetPlayFrame(songBeat + 1);
                        playEpoch = epoch;
                        break;
                }
            }
        }

        private void StartPlayback(ClockTransport transport)
        {
            SetPlaybackState(PlaybackState.Playing);
            playEpoch = clockEngine.Restart(transport);
        }

        private void FinishPlayback()
        {
            SetPlaybackState(PlaybackState.Reset);
            clockEngine.SendTransport(ClockTransport.Stop);
            isPlaybackFinished = true;
        }

        // read on the clock thread by the transform manager, so it's always current
        public PlaybackState State => playbackState;

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
                        FinishPlayback();
                        return;
                    }
                    SetPlayFrame(anchorFrame);
                }
                else
                {
                    FinishPlayback();
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
            clockEngine.OnExternalTransport -= HandleExternalTransport;
        }
    }
}
