using UnityEngine;

namespace TemperaMental.Settings
{
    public class TemperaSettingsEventController : MonoBehaviour
    {
        [SerializeField] TemperaSettingsManager settingsManager;

        // -1 or +1 from the - and + buttons
        public void OnClickControlChannel(int directionValue) => settingsManager.StepControlChannel(directionValue);

        // 0 from the Off button, 1 from the On button
        public void OnClickFollowTempera(int value) => settingsManager.SetFollowTempera(value != 0);

        public void OnTemperaSettingsClosed() => settingsManager.SaveTemperaSettings();
    }
}
