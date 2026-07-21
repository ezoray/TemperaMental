using TemperaMental.Logs;
using TemperaMental.Settings;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiTempoEventController : MonoBehaviour
    {
        [SerializeField] MidiSettingsManager settingsManager;
        [SerializeField] MidiTempoManager tempoManager;


        public void ActionOnBpmLoaded(int newBpm)
        {
            int maxBpm = settingsManager.MaxBpm;

            if (newBpm > maxBpm)
            {
                LogMan.LogTemp($"File BPM {newBpm} exceeds maximum, setting to {maxBpm}");
                newBpm = maxBpm;
            }

            tempoManager.SetBpm(newBpm);
        }

        // slider
        public void ActionOnBpmValueChanged(float bpm) => tempoManager.SetBpm(Mathf.RoundToInt(bpm));
    }
}
