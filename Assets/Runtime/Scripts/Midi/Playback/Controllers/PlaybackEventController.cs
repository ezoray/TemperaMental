using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Tempera.Mental.Frames;
using Tempera.Mental.Midi.Transforms;
using UnityEngine;
using UnityEngine.Events;

namespace Tempera.Mental.Midi.Playbacks
{
    public class PlaybackEventController : MonoBehaviour
    {
        [SerializeField] PlaybackManager playbackManager;
        [SerializeField] TransformService midiTransformService;
        [SerializeField] FrameManager frameManager;
        [SerializeField] UnityEvent<string> onSetOutputDevice;

        private void Start()
        {
            try
            {
                foreach (var device in OutputDevice.GetAll())
                {
                    Debug.Log("Device: " + device.Name);
                } 

                OutputDevice outputDevice = OutputDevice.GetByName("MidiView");
                outputDevice.PrepareForEventsSending();

                playbackManager.OutputDevice = outputDevice;

                onSetOutputDevice?.Invoke(outputDevice.Name);
            }
            catch(Exception ex)
            {
                Debug.LogError("PlaybackEventController error getting device: " + ex);
            }
        }

        public void OnClickPlayMidiFile()
        {
            Debug.Log("OnClickPlayMidiFile frames: " + frameManager.Frames.Count);

            List<Frame> frames = frameManager.Frames;
            MidiFile midiFile = midiTransformService.FromFramesToMidiFile(frames);

            playbackManager.PlayMidiFile(midiFile);
        }

        public void OnSelectBpm(int bpm)
        {

        }
    }
}
