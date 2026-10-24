using UnityEngine;

namespace TemperaMental.Settings
{
    // the MIDI channel and max BPM are saved as soon as they change
    public class MidiSettingsEventController : MonoBehaviour
    {
        [SerializeField] MidiSettingsManager settingsManager;

        // -1 or +1 from the - and + buttons
        public void OnClickMaxBpm(int directionValue)
        {
            settingsManager.UpdateMaxBpm(directionValue);
            settingsManager.SaveMidiSettings();
        }

        public void OnClickMidiChannel(int directionValue)
        {
            settingsManager.UpdateMidiChannel(directionValue);
            settingsManager.SaveMidiSettings();
        }
    }
}
