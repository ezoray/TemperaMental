using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
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

        [Header("Events")]
        public UnityEvent<int> OnFrameChanged;

        private void Update()
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
                OnFrameChanged.Invoke(currentFrame);
            }
        }

        public void StopPlayback()
        {
            if(playback != null && playback.IsRunning)
            {
                playback.Stop();
                playback.Dispose();
                playback = null;
            }
        }

        public void Stop()
        {
            playback?.Stop();
            playback?.MoveToStart();
            //      ResetHardware(); // Clear when stopping too
        }

        public void Pause()
        {
            playback?.Stop();
        }

        public void Continue()
        {
            if (playback != null && !playback.IsRunning)
            {
                playback.Start();
            }


        }

        public void PlayMidiFile(MidiFile midiFile)
        {
            try
            {
                SetupMidiFile(midiFile);

                // Log tempo info for debugging
                var tempoMap = midiFile.GetTempoMap();
                var tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(0));
                Debug.Log($"BPM: {60000000.0 / tempo.MicrosecondsPerQuarterNote}");

                // WORKAROUND: DryWetMidi clock runs 5x slower in Unity environment
                // Setting speed to 5.0 compensates to achieve correct BPM
                playback.Speed = 5.0;

                playback.Finished += OnPlaybackFinished;

                playback.Start();
            }
            catch(Exception ex)
            {
                LogMan.LogError("PlayMidiFile: " + ex);
            }
        }

        private void OnPlaybackFinished(object sender, EventArgs e)
        {
            LogMan.Log("PlaybackManager playback finished");
        }

        private void SetupMidiFile(MidiFile midiFile)
        {
            // Dispose existing playback
            if (playback != null)
            {
                playback.Stop();
                playback.Dispose();
                playback = null;
            }

            ticksPerFrame = GetTicksPerQuarterNote(midiFile);

            // Create playback with high precision tick generator
            var playbackSettings = new PlaybackSettings
            {
                ClockSettings = new MidiClockSettings
                {
                    CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator()
                }
            };

            playback = midiFile.GetPlayback(playbackSettings);
            playback.OutputDevice = outputDevice;
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