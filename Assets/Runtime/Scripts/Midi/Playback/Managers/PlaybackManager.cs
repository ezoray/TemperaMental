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

        // anchorFrame — the file-space position used for looping and seeking within the active playback object
        // playStartAnchor — the display-space frame where Play was last called
        int anchorFrame;
        int playStartAnchor;
        volatile int playFrame;
        int totalFrames;
        short ticksPerFrame;
        int midiFileBpm;

        PlaybackState playbackState;
        ConcurrentQueue<int> frameQueue;

        [SerializeField] UnityEvent<bool> onOutputDeviceChanged;
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
                HandlePlaybackFinished();
            }

            while (frameQueue.TryDequeue(out int frameNumber))
            {
                onPlaybackFrameChanged?.Invoke(frameNumber);
            }
        }

        // plays from current playhead position and sets that as the anchor
        // if already playing restarts from same anchor
        public void Play(MidiFileDetail midiFileDetail, int initialFrame)
        {
            if (outputDevice == null) return;

            try
            {
                int frameToPlayFrom = (playbackState == PlaybackState.Playing) ? playStartAnchor : initialFrame;

                InitPlaybacks(midiFileDetail, frameToPlayFrom);

                playStartAnchor = frameToPlayFrom;
                midiFileBpm = MidiUtils.GetBpmFromMidiFile(midiFileDetail.ForwardMidiFile);

                ActivePlayback.Start();

                LogMan.Log($"Playing from frame {playStartAnchor}");
                SetPlaybackState(PlaybackState.Playing);
            }
            catch (Exception ex)
            {
                LogMan.LogError("Play failed: " + ex);
                ResetPlayback();
            }
        }

        public void TogglePlayPause(MidiFileDetail midiFileDetail, int initialFrame)
        {
            switch (playbackState)
            {
                case PlaybackState.Idle:
                case PlaybackState.Stopped:
                    Play(midiFileDetail, initialFrame);
                    break;

                case PlaybackState.Playing:
                    PausePlayback();
                    break;

                case PlaybackState.Paused:
                    ResumePlayback();
                    break;
            }
        }

        public void Stop()
        {
            try
            {
                switch (playbackState)
                {
                    case PlaybackState.Playing:
                        ActivePlayback.Stop();
                        LogMan.Log($"Playback stopped");
                        SetPlaybackState(PlaybackState.Stopped);
                        break;

                    case PlaybackState.Paused:
                        ActivePlayback.Stop();
                        MoveToAnchor();
                        LogMan.Log($"Playback stopped, returned to frame {playStartAnchor}");
                        SetPlaybackState(PlaybackState.Idle);
                        break;

                    case PlaybackState.Stopped:
                        ActivePlayback.Stop();
                        MoveToAnchor();
                        LogMan.Log($"Returned to frame {playStartAnchor}");
                        SetPlaybackState(PlaybackState.Idle);
                        break;
      
                    case PlaybackState.Idle:
                        // no-op
                        break;
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError("Stop failed: " + ex);
            }
        }

        public void SeekToFrame(int seekFrame)
        {
            if (playbackState == PlaybackState.Idle) return;

            playFrame = Mathf.Clamp(seekFrame, 1, totalFrames);
            anchorFrame = isReversed ? (totalFrames - playFrame) + 1 : playFrame;
            playStartAnchor = playFrame; // seeking sets a new play start anchor

            long ticks = (anchorFrame - 1) * ticksPerFrame;

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

                anchorFrame = isReversed ? (totalFrames - playFrame) + 1 : playFrame;

                long ticks = (anchorFrame - 1) * ticksPerFrame;
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

        public void ClearOutputDevice()
        {
            outputDevice = null;
            Stop();

            onOutputDeviceChanged?.Invoke(false);
        }

        public void SetOutputDevice(OutputDevice outputDevice)
        {
            this.outputDevice = outputDevice;

            onOutputDeviceChanged?.Invoke(true);
        }

        private void PausePlayback()
        {
            if (playbackState != PlaybackState.Playing) return;

            ActivePlayback.Stop();
            LogMan.Log("Playback paused");
            SetPlaybackState(PlaybackState.Paused);
        }

        private void ResumePlayback()
        {
            if (playbackState != PlaybackState.Paused) return;

            ActivePlayback.Start();
            LogMan.Log("Playback resumed");
            SetPlaybackState(PlaybackState.Playing);
        }

        private void MoveToAnchor()
        {
            // convert playStartAnchor (display-space) back to file-space for the active playback
            int fileSpaceAnchor = isReversed ? (totalFrames - playStartAnchor) + 1 : playStartAnchor;
            long ticks = (fileSpaceAnchor - 1) * ticksPerFrame;
            ActivePlayback.MoveToTime(new MidiTimeSpan(ticks));
            playFrame = playStartAnchor;
            frameQueue.Enqueue(playFrame);
        }

        private void HandlePlaybackFinished()
        {
            ResetPlayback();
            LogMan.Log("Playback finished");
            SetPlaybackState(PlaybackState.Idle);
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
                // only default to end frame if no meaningful position has been set
                if (playFrame == 1 && playStartAnchor == 0)
                {
                    playFrame = totalFrames;
                }

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
            Playback playback = midiFile.GetPlayback();

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

        private void DisposePlayback(ref Playback playback)
        {
            if (playback == null) return;

            playback.Stop();
            playback.ErrorOccurred -= OnPlaybackError;
            playback.Finished -= OnPlaybackFinished;
            playback.EventPlayed -= OnEventPlayed;
            playback.RepeatStarted -= OnRepeatStarted;
            playback.Dispose();
            playback = null;
        }

        private void ResetPlayback()
        {
            DisposePlayback(ref forwardPlayback);
            DisposePlayback(ref reversePlayback);
            SetPlaybackState(PlaybackState.Idle);

            // prevent app from holding on to empty memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void OnDestroy()
        {
            ResetPlayback();
        }
    }
}