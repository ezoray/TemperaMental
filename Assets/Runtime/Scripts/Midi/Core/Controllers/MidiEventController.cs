using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiEventController : MonoBehaviour
    {
        [SerializeField] MidiManager midiManager;
        [SerializeField] MidiImmediateService immediateService;

        public void ActionOnPlaybackStateChanged(PlaybackState playbackState) => immediateService.EnableSendingByPlaybackState(playbackState);

        public void ActionOnRemoveEmitter(Vector2Int position) => immediateService.RemoveEmitter(position);

        public void ActionOnAddEmitter(EmitterDetail emitterDetail) => immediateService.AddEmitter(emitterDetail);

        public void ActionOnEmitterTypeChanged(int emitterId) => immediateService.SetEmitterType(emitterId);

        public void ActionOnFrameChanged(FrameDetail frameDetail) => immediateService.SendEmitters(frameDetail.EmitterDetails);

        public void ActionOnBpmValueChanged(float bpm) => midiManager.SetBpm(Mathf.RoundToInt(bpm));

        public void ActionOnDeviceRemoved() => immediateService.ClearOutputDevice();

        public void ActionOnDeviceChanged(OutputDevice device) => immediateService.SetOutputDevice(device);
    }
}
