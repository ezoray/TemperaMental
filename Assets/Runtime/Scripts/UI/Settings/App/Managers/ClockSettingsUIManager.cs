using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Settings.App
{
    public class ClockSettingsUIManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI clockMode;
        [SerializeField] TextMeshProUGUI sendStartStop;
        [SerializeField] TextMeshProUGUI clockFrom;
        [SerializeField] TextMeshProUGUI clockTo;

        public void ActionOnClockModeChanged(ClockMode mode)
        {
            clockMode.text = mode.ToString();
        }

        public void ActionOnSendStartStopChanged(bool isSent)
        {
            sendStartStop.text = isSent ? ConfigRegistry.UI.OnText : ConfigRegistry.UI.OffText;
        }

        public void ActionOnClockFromChanged(string choice)
        {
            clockFrom.text = string.IsNullOrEmpty(choice) ? ConfigRegistry.UI.SameAsInText : choice;
        }

        public void ActionOnClockToChanged(string choice)
        {
            clockTo.text = string.IsNullOrEmpty(choice) ? ConfigRegistry.UI.SameAsOutText
                : choice == ConfigRegistry.Midi.ClockToNoneValue ? ConfigRegistry.UI.NoneText
                : choice;
        }
    }
}
