using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiImmediateEventController : MonoBehaviour
    {
        [SerializeField] MidiTempoManager midiManager;
        [SerializeField] MidiImmediateService immediateService;

        public void ActionOnPlaybackStateChanged(PlaybackState playbackState) => immediateService.SetPlaybackState(playbackState);

        public void ActionOnRemoveEmitter(Vector2Int position, int emitterCount) => immediateService.RemoveEmitter(position);

        public void ActionOnAddEmitter(EmitterDetail emitterDetail) => immediateService.AddEmitter(emitterDetail);

        public void ActionOnEmitterTypeChanged(int emitterId) => immediateService.SetEmitterType(emitterId);

        public void ActionOnFrameChanged(FrameDetail frameDetail) => immediateService.SendFrame(frameDetail.EmitterGroups);

        public void ActionOnBpmValueChanged(float bpm) => midiManager.SetBpm(Mathf.RoundToInt(bpm));
    }
}
