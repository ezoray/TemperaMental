using UnityEngine;

namespace TemperaMental.Settings
{
    public class MidiSettingsEventController : MonoBehaviour
    {
        [SerializeField] MidiSettingsManager settingsManager;

        public void OnChannelSettingsClosed()
        {
            settingsManager.SetMidiChannel();
        }

        public void OnClickChannel(int directionValue)
        {
            settingsManager.CycleMidiChannel(directionValue);
        }
    }
}
