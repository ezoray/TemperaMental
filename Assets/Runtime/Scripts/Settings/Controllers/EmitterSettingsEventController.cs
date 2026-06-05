using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Settings
{
    public class EmitterSettingsEventController : MonoBehaviour
    {
        [SerializeField] EmitterSettingsManager settingsManager;

        public void ActionOnSettingsViewClosed(DisplayViewType viewType)
        {
            settingsManager.SetEmitterChannels(viewType);
        }

        public void OnClickChannel(int value)
        {
            int emitterId = value / 2;
            int direction = value % 2 == 0 ? -1 : 1;
            settingsManager.CycleChannel(emitterId, direction);
        }
    }
}
