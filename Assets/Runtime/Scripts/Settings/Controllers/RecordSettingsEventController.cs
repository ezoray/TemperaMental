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
    }
}
