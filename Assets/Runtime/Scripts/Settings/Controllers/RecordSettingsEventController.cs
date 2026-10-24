using UnityEngine;

namespace TemperaMental.Settings
{
    // each button in the Record page's row steps its setting to the next choice and saves it straight away
    public class RecordSettingsEventController : MonoBehaviour
    {
        [SerializeField] RecordSettingsManager settingsManager;

        public void OnClickTrigger()
        {
            settingsManager.StepTrigger();
            settingsManager.SaveRecordSettings();
        }

        public void OnClickMode()
        {
            settingsManager.StepMode();
            settingsManager.SaveRecordSettings();
        }

        public void OnClickStartState()
        {
            settingsManager.StepStartState();
            settingsManager.SaveRecordSettings();
        }

        public void OnClickLength()
        {
            settingsManager.StepLength();
            settingsManager.SaveRecordSettings();
        }
    }
}
