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
        private Playback playback;

        bool isFrameMarkerEvent;
        int frameNumber;
        bool isPlaybackFinished;

        [Header("Events")]
        [SerializeField] UnityEvent<int> onFrameChanged;
        [SerializeField] UnityEvent onPlaybackFinished;

        // todo this is adequate but a better solution is setting up a separate queue to prevent loss of events as they're triggered on the
        // separate playback thread and Update may not run often enough to pick them up
        private void Update()
        {
            if (isPlaybackFinished)
            {
                isPlaybackFinished = false;

                onPlaybackFinished?.Invoke();
            }
            else
            {
                if(isFrameMarkerEvent)
                {
                    isFrameMarkerEvent = false;

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
                }

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

        private void OnEventPlayed(object sender, MidiEventPlayedEventArgs eventArgs)
        {
            if (eventArgs.Event is MarkerEvent marker)
            {
                if (int.TryParse(marker.Text, out frameNumber))
                {
                    isFrameMarkerEvent = true;
                }
            }
        }

        private void OnPlaybackFinished(object sender, EventArgs e)
        {
            playback.Finished -= OnPlaybackFinished;
            playback.EventPlayed -= OnEventPlayed;

            isPlaybackFinished = true;
            ResetPlayback();
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