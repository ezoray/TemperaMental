using UnityEngine;

namespace TemperaMental.Settings
{
    public class MidiSettingsEventController : MonoBehaviour
    {
        [SerializeField] MidiSettingsManager settingsManager;

        public void OnMidiSettingsClosed()
        {
            settingsManager.SaveMidiSettings();
        }

        public void OnClickMaxBpm(int directionValue)
        {
            settingsManager.UpdateMaxBpm(directionValue);
        }

        public void OnClickMidiChannel(int directionValue)
        {
            settingsManager.UpdateMidiChannel(directionValue);
        }
    }
}
