using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Tempera.Mental.Frames;
using Tempera.Mental.Logs;
using Tempera.Mental.Midi.Devices;
using Tempera.Mental.Midi.Transforms;
using UnityEngine;
using UnityEngine.Events;

namespace Tempera.Mental.Midi.Playbacks
{
    public class PlaybackEventController : MonoBehaviour
    {
        [SerializeField] DeviceManager deviceManager;
        [SerializeField] PlaybackManager playbackManager;
        [SerializeField] TransformService midiTransformService;
        [SerializeField] FrameManager frameManager;
        [SerializeField] UnityEvent<string> onSetOutputDevice;


        public void OnDeviceRemoved(string deviceName)
        {
            // todo check device removed isn't the one being used
            if(deviceName.Equals(playbackManager.OutputDeviceName))
            {
                playbackManager.StopPlayback();
            }
        }

        public void OnDeviceAdded(string deviceName)
        {

        }

        public void OnDeviceChanged(string deviceName)
        {
            LogMan.Log("OnDeviceChanged: " + deviceName);

            if(deviceManager.TryGetOutputDevice(deviceName, out var outputDevice))
            {
                playbackManager.SetOutputDevice(outputDevice as OutputDevice);
            }
        }

        public void OnClickPlayMidiFile()
        {
            LogMan.Log("OnClickPlayMidiFile frames: " + frameManager.Frames.Count);

            List<Frame> frames = frameManager.Frames;
            MidiFile midiFile = midiTransformService.FromFramesToMidiFile(frames);

            playbackManager.PlayMidiFile(midiFile);
        }

        public void OnSelectBpm(int bpm)
        {

        }
    }
}
