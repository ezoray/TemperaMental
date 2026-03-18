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
        const string FRAME_NO = "FRAME_NO_";
        const string END_OF_FRAME = "FRAME_END";

        OutputDevice outputDevice;
        string outputDeviceName;
        private Playback playback;

        volatile bool isPlaybackFinished;
        bool isLooping;

        private ConcurrentQueue<int> frameQueue;

        [SerializeField] UnityEvent<int> onFrameChanged;
        [SerializeField] UnityEvent onPlaybackFinished;

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

                onPlaybackFinished?.Invoke();
            }
            else
            {
                while (frameQueue.TryDequeue(out int frameNumber))
                {
                    onFrameChanged?.Invoke(frameNumber);
                }
            }
        }

        public bool TryStop()
        {
            try
            {
                PlaybackState state = GetPlaybackState();

                if (state == PlaybackState.Playing || state == PlaybackState.Paused)
                {
                    // todo currently we generate the frames to midifile each time Play is hit even if there have been no changes
                    // this is convenient but can cause logic problems and playback.Stop is actually a pause
                    ResetPlayback();
                    return true;
                }
                else
                {
                    LogMan.LogWarning("TryStop playback in wrong state: " + state);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError("TryPause: " + ex);
                return false;
            }        
        }

        public bool TryPause()
        {
            try
            {
                if (GetPlaybackState() != PlaybackState.Playing)
                {
                    LogMan.LogError("TryPause playback in wrong state: " + GetPlaybackState());
                    return false;
                }

                playback.Stop();
                return true;
            }

            catch (Exception ex)
            {
                LogMan.LogError("TryPause: " + ex); 
                return false;
            }              
        }

        public bool TryResumePlay()
        {
            try
            {
                PlaybackState state = GetPlaybackState();

                if (state != PlaybackState.Paused)
                {
                    LogMan.LogWarning("TryResumePlay playback in wrong state: " + GetPlaybackState());
                    return false;
                }

                playback.Start();
                return true;
            }
            catch (Exception ex)
            {
                LogMan.LogError("TryResumePlay: " + ex);
                return false;
            }
        }

        public bool TryPlay(MidiFile midiFile)
        {
            try
            {
                PlaybackState state = GetPlaybackState();

                if (state != PlaybackState.Reset)
                {
                    LogMan.LogWarning("TryPlay playback in wrong state: " + GetPlaybackState());
                    ResetPlayback();
                }

                playback = midiFile.GetPlayback();
                playback.OutputDevice = outputDevice;

                playback.Loop = isLooping;

                playback.ErrorOccurred += OnPlaybackError;
                playback.Finished += OnPlaybackFinished;
                playback.EventPlayed += OnEventPlayed;

                playback.Start();

                return true; // too early to use playback.IsRunning
            }
            catch (Exception ex)
            {
                LogMan.LogError("TryPlay: " + ex);
                ResetPlayback();
                return false;
            }
        }

        private void OnEventPlayed(object sender, MidiEventPlayedEventArgs eventArgs)
        {
            if (eventArgs.Event is MarkerEvent marker)
            {
                string text = marker.Text;

                if (text.StartsWith(FRAME_NO))
                {
                    string numberPart = text.Replace(FRAME_NO, "");

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
            ResetPlayback();

            isPlaybackFinished = true;
        }

        public int GetTicksPerQuarterNote(MidiFile midiFile)
        {
            if (midiFile.TimeDivision is TicksPerQuarterNoteTimeDivision timeDivision)
            {
                return timeDivision.TicksPerQuarterNote;
            }
            return 480;
        }

        public PlaybackState GetPlaybackState()
        {
            if (playback == null) return PlaybackState.Reset;

            if (playback.IsRunning) return PlaybackState.Playing;

            // Use .TimeSpan to get the 'long' tick value
            long currentTick = playback.GetCurrentTime<MidiTimeSpan>().TimeSpan;

            return (currentTick == 0) ? PlaybackState.Stopped : PlaybackState.Paused;
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

        public void SetOutputDevice(OutputDevice outputDevice)
        {
            LogMan.Log("SetOutputDevice : " + outputDevice.Name);

            this.outputDevice = outputDevice;
            outputDeviceName = outputDevice.Name;
            outputDevice.PrepareForEventsSending();
        }

        private void OnDestroy()
        {
            //            ResetHardware(); // Safety clear
            playback?.Dispose();
        }

        public void SetLoopState(bool isLooping)
        {
            LogMan.Log("SetLoopState : " + isLooping);

            this.isLooping = isLooping;

            if (GetPlaybackState() != PlaybackState.Reset)
            {
                playback.Loop = isLooping;
            }
        }

        public string OutputDeviceName { get => outputDeviceName; }
    }
}