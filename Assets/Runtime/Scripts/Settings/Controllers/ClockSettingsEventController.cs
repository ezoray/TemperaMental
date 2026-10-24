using UnityEngine;

namespace TemperaMental.Settings
{
    // clock settings are saved as soon as they change
    public class ClockSettingsEventController : MonoBehaviour
    {
        [SerializeField] ClockSettingsManager settingsManager;

        // Play mode's Clock button steps through Internal, Send and Receive
        public void OnClickNextClockMode()
        {
            settingsManager.StepClockMode(1);
            settingsManager.SaveClockSettings();
        }

        public void OnClickToggleSendStartStop()
        {
            settingsManager.ToggleSendStartStop();
            settingsManager.SaveClockSettings();
        }

        // -1 or +1 from the < and > buttons in Settings mode
        public void OnClickClockFrom(int directionValue)
        {
            settingsManager.StepClockFrom(directionValue);
            settingsManager.SaveClockSettings();
        }

        public void OnClickClockTo(int directionValue)
        {
            settingsManager.StepClockTo(directionValue);
            settingsManager.SaveClockSettings();
        }
    }
}
