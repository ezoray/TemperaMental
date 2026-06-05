using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Settings
{
    public class MidiSettingsEventController : MonoBehaviour
    {
        [SerializeField] MidiSettingsManager settingsManager;


        public void ActionOnSettingsViewClosed(DisplayViewType viewType)
        {
            settingsManager.SetMidiChannel();
        }

        public void OnClickChannel(int value)
        {
            int direction = value % 2 == 0 ? -1 : 1;
            settingsManager.CycleMidiChannel(direction);
        }
    }
}
