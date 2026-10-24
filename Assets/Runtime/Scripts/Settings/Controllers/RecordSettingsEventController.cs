using UnityEngine;

namespace TemperaMental.Settings
{
    public class RecordSettingsEventController : MonoBehaviour
    {
        [SerializeField] RecordSettingsManager settingsManager;

        // each Record settings button steps its setting to the next choice
        public void OnClickSource() => settingsManager.StepSource();

        public void OnClickTrigger() => settingsManager.StepTrigger();

        public void OnClickMode() => settingsManager.StepMode();

        public void OnClickStartState() => settingsManager.StepStartState();

        public void OnClickLength() => settingsManager.StepLength();

        public void OnRecordSettingsClosed() => settingsManager.SaveRecordSettings();

        // the main view's Record surface: each press steps the setting and saves it straight away
        public void OnClickSurfaceTrigger()
        {
            settingsManager.StepTrigger();
            settingsManager.SaveRecordSettings();
        }

        public void OnClickSurfaceMode()
        {
            settingsManager.StepMode();
            settingsManager.SaveRecordSettings();
        }

        public void OnClickSurfaceStartState()
        {
            settingsManager.StepStartState();
            settingsManager.SaveRecordSettings();
        }

        public void OnClickSurfaceLength()
        {
            settingsManager.StepLength();
            settingsManager.SaveRecordSettings();
        }
    }
}
