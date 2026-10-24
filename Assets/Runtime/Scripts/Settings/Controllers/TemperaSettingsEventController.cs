using UnityEngine;

namespace TemperaMental.Settings
{
    // the Control channel (Settings mode) and Follow Tempera (Edit mode) are saved as soon as they change
    public class TemperaSettingsEventController : MonoBehaviour
    {
        [SerializeField] TemperaSettingsManager settingsManager;

        // -1 or +1 from the - and + buttons
        public void OnClickControlChannel(int directionValue)
        {
            settingsManager.StepControlChannel(directionValue);
            settingsManager.SaveTemperaSettings();
        }

        public void OnClickToggleFollowTempera()
        {
            settingsManager.ToggleFollowTempera();
            settingsManager.SaveTemperaSettings();
        }
    }
}
