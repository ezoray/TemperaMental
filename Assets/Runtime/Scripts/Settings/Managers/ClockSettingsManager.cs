using System;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Settings
{
    // clock source (Internal or Send) and whether Start/Stop are sent with the clock, saved in PlayerPrefs
    public class ClockSettingsManager : MonoBehaviour
    {
        string clockModePrefsKey;
        string sendStartStopPrefsKey;

        ClockMode clockMode;
        bool isStartStopSent;

        [SerializeField] UnityEvent<ClockMode> onClockModeChanged = new UnityEvent<ClockMode>();
        [SerializeField] UnityEvent<bool> onSendStartStopChanged = new UnityEvent<bool>();

        private void Awake()
        {
            clockModePrefsKey = ConfigRegistry.Midi.ClockModePrefsKey;
            sendStartStopPrefsKey = ConfigRegistry.Midi.SendStartStopPrefsKey;

            clockMode = (ClockMode)PlayerPrefs.GetInt(clockModePrefsKey, (int)ClockMode.Internal);

            // Receive isn't available yet
            if (clockMode == ClockMode.Receive) clockMode = ClockMode.Internal;

            isStartStopSent = PlayerPrefs.GetInt(sendStartStopPrefsKey, ConfigRegistry.Midi.DefaultSendStartStop ? 1 : 0) == 1;
        }

        // raised in Start rather than Awake so every listener has finished its own Awake
        private void Start()
        {
            onClockModeChanged?.Invoke(clockMode);
            onSendStartStopChanged?.Invoke(isStartStopSent);
        }

        // steps through the available modes, wrapping around. Receive is added once clock following exists
        public void StepClockMode(int directionValue)
        {
            ClockMode[] modes = { ClockMode.Internal, ClockMode.Send };

            int index = Array.IndexOf(modes, clockMode);
            index = ((index + directionValue) % modes.Length + modes.Length) % modes.Length;
            clockMode = modes[index];

            onClockModeChanged?.Invoke(clockMode);
        }

        public void SetSendStartStop(bool isSent)
        {
            if (isStartStopSent == isSent) return;

            isStartStopSent = isSent;

            onSendStartStopChanged?.Invoke(isStartStopSent);
        }

        public void SaveClockSettings()
        {
            LogMan.LogTemp($"Clock {clockMode}, Start/Stop {(isStartStopSent ? ConfigRegistry.UI.OnText : ConfigRegistry.UI.OffText)}");

            PlayerPrefs.SetInt(clockModePrefsKey, (int)clockMode);
            PlayerPrefs.SetInt(sendStartStopPrefsKey, isStartStopSent ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
