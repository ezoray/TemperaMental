using System;
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
        OutputDevice outputDevice;
        string outputDeviceName;
        int ticksPerFrame;
        private Playback playback;
        private int lastFrameIndex = -1;

        bool isPlaybackFinished;

        [Header("Events")]
        [SerializeField] UnityEvent<int> onFrameChanged;
        [SerializeField] UnityEvent onPlaybackFinished;

        // todo use event markers and event callback to determine new frame and with current way playback is finished before switch to last frame
        private void Update()
        {
            if (isPlaybackFinished)
            {
                isPlaybackFinished = false;
                ResetPlayback();

                onPlaybackFinished?.Invoke();
            }
            else
            {
                if (playback == null || !playback.IsRunning) return;

                ITimeSpan currentTime = playback.GetCurrentTime(TimeSpanType.Midi);
                long currentTick = ((MidiTimeSpan)currentTime).TimeSpan;

                // Compensate for the 5x speed workaround
                long adjustedTick = (long)(currentTick / playback.Speed);

                int currentFrame = (int)(adjustedTick / ticksPerFrame);

                if (currentFrame != lastFrameIndex)
                {
                    lastFrameIndex = currentFrame;
                    onFrameChanged.Invoke(currentFrame);
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
                    playback.Stop();
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
                    return false;
                }

                ticksPerFrame = GetTicksPerQuarterNote(midiFile);

                playback = midiFile.GetPlayback();
                playback.OutputDevice = outputDevice;

                playback.Speed = 5.0; // todo investigate where the bug is that makes this required

                playback.Finished += OnPlaybackFinished;
                playback.EventPlayed += OnEventPlayed;

                playback.Start();

                return true; // seems too early to use playback.IsRunning
            }
            catch (Exception ex)
            {
                LogMan.LogError("TryPlay: " + ex);
                ResetPlayback();
                return false;
            }
        }

        private void OnEventPlayed(object sender, MidiEventPlayedEventArgs e)
        {
        }

        private void OnPlaybackFinished(object sender, EventArgs e)
        {
            playback.Finished -= OnPlaybackFinished;
            playback.EventPlayed -= OnEventPlayed;

            isPlaybackFinished = true;
        }

         // --- NEW HELPER METHODS ---

        //private void ResetHardware()
        //{
        //    if (outputDevice == null) return;

        //    // CC 12 / 127 is your Clear command
        //    outputDevice.SendEvent(new ControlChangeEvent(
        //        (SevenBitNumber)12,
        //        (SevenBitNumber)127));

        //    Debug.Log("Hardware Reset: Sent CC 12");
        //}

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

    

        public string OutputDeviceName { get => outputDeviceName; }
    }
}