using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Settings.App
{
    public class RecordSettingsUIManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI source;
        [SerializeField] TextMeshProUGUI trigger;
        [SerializeField] TextMeshProUGUI mode;
        [SerializeField] TextMeshProUGUI startState;
        [SerializeField] TextMeshProUGUI length;

        public void ActionOnSourceChanged(RecordSource value) => source.text = value.ToString();

        public void ActionOnTriggerChanged(RecordTrigger value) => trigger.text = value.ToString();

        public void ActionOnModeChanged(RecordMode value) => mode.text = value.ToString();

        public void ActionOnStartStateChanged(RecordStartState value) => startState.text = value.ToString();

        public void ActionOnLengthChanged(int frames) => length.text = frames > 0 ? frames.ToString() : ConfigRegistry.UI.RecordLengthOffText;
    }
}
