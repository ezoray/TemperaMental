using TemperaMental.Core;
using TemperaMental.Frames;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiImmediateEventController : MonoBehaviour
    {
        [SerializeField] FrameManager frameManager;
        [SerializeField] MidiTempoManager midiTempoManager;
        [SerializeField] MidiImmediateManager immediateManager;

        public void ActionOnRemoveEmitter(EmitterDetail emitterDetail) => immediateManager.RemoveEmitter(emitterDetail);

        public void ActionOnAddEmitter(EmitterDetail emitterDetail) => immediateManager.AddEmitter(emitterDetail);

        public void ActionOnEmitterTypeChanged(int emitterId) => immediateManager.SetEmitterType(emitterId);

        public void ActionOnFrameChanged(FrameDetail frameDetail) => immediateManager.SendFrame(frameDetail.EmitterGroups);

        public void ActionOnPlaybackReadyStateChanged(bool isReady)
        {
            if (isReady)
            {
                ulong[] emitterGroups = frameManager.GetCurrentFrameEmitters();
                immediateManager.SendFrame(emitterGroups);
            }
        }
    }
}
