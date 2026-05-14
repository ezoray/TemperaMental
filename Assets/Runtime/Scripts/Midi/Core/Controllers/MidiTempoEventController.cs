using System.Collections.Generic;
using TemperaMental.Frames;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiTempoEventController : MonoBehaviour
    {
        [SerializeField] MidiTempoManager tempoManager;


        public void ActionOnBpmLoaded(int bpm) => tempoManager.SetBpm(bpm);

        // slider
        public void ActionOnBpmValueChanged(float bpm) => tempoManager.SetBpm(Mathf.RoundToInt(bpm));
    }
}
