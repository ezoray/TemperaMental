using System;
using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using TemperaMental.Midi.Devices;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Settings
{
    // Clock mode (Internal, Send, Receive), whether Start/Stop are sent, and which devices clock comes from and goes to.
    // Saved in PlayerPrefs when the settings view closes.
    // Clock from: "" means the In device, otherwise an input name.
    // Clock to: "" means the Out device, ClockToNoneValue means nowhere, otherwise an output name.
    public class ClockSettingsManager : MonoBehaviour
    {
        static readonly ClockMode[] Modes = { ClockMode.Internal, ClockMode.Send, ClockMode.Receive };

        [SerializeField] DeviceManager deviceManager;

        string clockModePrefsKey;
        string sendStartStopPrefsKey;
        string clockFromPrefsKey;
        string clockToPrefsKey;
        string clockToNone;

        ClockMode clockMode;
        bool isStartStopSent;
        string clockFrom;
        string clockTo;

        [SerializeField] UnityEvent<ClockMode> onClockModeChanged = new UnityEvent<ClockMode>();
        [SerializeField] UnityEvent<bool> onSendStartStopChanged = new UnityEvent<bool>();
        [SerializeField] UnityEvent<string> onClockFromChanged = new UnityEvent<string>();
        [SerializeField] UnityEvent<string> onClockToChanged = new UnityEvent<string>();

        private void Awake()
        {
            clockModePrefsKey = ConfigRegistry.Midi.ClockModePrefsKey;
            sendStartStopPrefsKey = ConfigRegistry.Midi.SendStartStopPrefsKey;
            clockFromPrefsKey = ConfigRegistry.Midi.ClockFromPrefsKey;
            clockToPrefsKey = ConfigRegistry.Midi.ClockToPrefsKey;
            clockToNone = ConfigRegistry.Midi.ClockToNoneValue;

            clockMode = (ClockMode)PlayerPrefs.GetInt(clockModePrefsKey, (int)ClockMode.Internal);
            if (Array.IndexOf(Modes, clockMode) < 0) clockMode = ClockMode.Internal;

            isStartStopSent = PlayerPrefs.GetInt(sendStartStopPrefsKey, ConfigRegistry.Midi.DefaultSendStartStop ? 1 : 0) == 1;
            clockFrom = PlayerPrefs.GetString(clockFromPrefsKey, "");
            clockTo = PlayerPrefs.GetString(clockToPrefsKey, "");
        }

        // raised in Start rather than Awake so every listener has finished its own Awake
        private void Start()
        {
            onClockModeChanged?.Invoke(clockMode);
            onSendStartStopChanged?.Invoke(isStartStopSent);
            onClockFromChanged?.Invoke(clockFrom);
            onClockToChanged?.Invoke(clockTo);
        }

        // steps through Internal, Send and Receive, wrapping around
        public void StepClockMode(int directionValue)
        {
            int index = Array.IndexOf(Modes, clockMode);
            clockMode = Modes[Wrap(index + directionValue, Modes.Length)];

            onClockModeChanged?.Invoke(clockMode);
        }

        public void ToggleSendStartStop() => SetSendStartStop(!isStartStopSent);

        public void SetSendStartStop(bool isSent)
        {
            if (isStartStopSent == isSent) return;

            isStartStopSent = isSent;

            onSendStartStopChanged?.Invoke(isStartStopSent);
        }

        // steps through "same as In", then each connected input
        public void StepClockFrom(int directionValue)
        {
            var choices = new List<string> { "" };
            choices.AddRange(deviceManager.ConnectedInputs);

            clockFrom = Step(choices, clockFrom, directionValue);
            onClockFromChanged?.Invoke(clockFrom);
        }

        // steps through "same as Out", none, then each connected output
        public void StepClockTo(int directionValue)
        {
            var choices = new List<string> { "", clockToNone };
            choices.AddRange(deviceManager.ConnectedOutputs);

            clockTo = Step(choices, clockTo, directionValue);
            onClockToChanged?.Invoke(clockTo);
        }

        public void SaveClockSettings()
        {
            LogMan.LogTemp($"Clock {clockMode}, Start/Stop {(isStartStopSent ? ConfigRegistry.UI.OnText : ConfigRegistry.UI.OffText)}");

            PlayerPrefs.SetInt(clockModePrefsKey, (int)clockMode);
            PlayerPrefs.SetInt(sendStartStopPrefsKey, isStartStopSent ? 1 : 0);
            PlayerPrefs.SetString(clockFromPrefsKey, clockFrom);
            PlayerPrefs.SetString(clockToPrefsKey, clockTo);
            PlayerPrefs.Save();
        }

        // a remembered device that isn't connected stays selected until stepped past
        private static string Step(List<string> choices, string current, int directionValue)
        {
            int index = choices.IndexOf(current);

            if (index < 0)
            {
                choices.Insert(1, current);
                index = 1;
            }

            return choices[Wrap(index + directionValue, choices.Count)];
        }

        private static int Wrap(int index, int count)
        {
            return (index % count + count) % count;
        }
    }
}
