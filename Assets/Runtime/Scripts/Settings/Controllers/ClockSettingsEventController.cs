using UnityEngine;

namespace TemperaMental.Settings
{
    public class ClockSettingsEventController : MonoBehaviour
    {
        [SerializeField] ClockSettingsManager settingsManager;

        // -1 or +1 from the < and > buttons
        public void OnClickClockMode(int directionValue) => settingsManager.StepClockMode(directionValue);

        // 0 from the Off button, 1 from the On button
        public void OnClickSendStartStop(int value) => settingsManager.SetSendStartStop(value != 0);

        public void OnClockSettingsClosed() => settingsManager.SaveClockSettings();
    }
}
