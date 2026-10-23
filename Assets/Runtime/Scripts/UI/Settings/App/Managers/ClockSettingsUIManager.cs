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

        public void ActionOnClockModeChanged(ClockMode mode)
        {
            clockMode.text = mode.ToString();
        }

        public void ActionOnSendStartStopChanged(bool isSent)
        {
            sendStartStop.text = isSent ? ConfigRegistry.UI.OnText : ConfigRegistry.UI.OffText;
        }
    }
}
