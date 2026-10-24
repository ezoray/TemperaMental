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

        // -1 or +1 from the < and > buttons on the clock devices view
        public void OnClickClockFrom(int directionValue) => settingsManager.StepClockFrom(directionValue);

        public void OnClickClockTo(int directionValue) => settingsManager.StepClockTo(directionValue);

        public void OnClockSettingsClosed() => settingsManager.SaveClockSettings();

        // the main view's Clock surface: each press steps the setting and saves it straight away
        public void OnClickSurfaceClockMode()
        {
            settingsManager.StepClockMode(1);
            settingsManager.SaveClockSettings();
        }

        public void OnClickSurfaceSendStartStop()
        {
            settingsManager.ToggleSendStartStop();
            settingsManager.SaveClockSettings();
        }
    }
}
