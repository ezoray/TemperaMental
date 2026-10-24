using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Display.MainView
{
    // The main view's button row has switchable surfaces (File, Clock, Record, Tempera), stepped with the < and > buttons at
    // each end of the row. The surface shown is remembered. Clock and Record buttons show a dimmed caption and the
    // setting's current value; pressing one steps the setting (see the settings event controllers).
    public class MainViewSurfaceUIManager : MonoBehaviour
    {
        [Header("Order: File, Clock, Record, Tempera")]
        [SerializeField] GameObject[] surfaces;

        [Header("Clock surface")]
        [SerializeField] TextMeshProUGUI clockModeText;
        [SerializeField] TextMeshProUGUI startStopText;

        [Header("Record surface")]
        [SerializeField] TextMeshProUGUI triggerText;
        [SerializeField] TextMeshProUGUI modeText;
        [SerializeField] TextMeshProUGUI startStateText;
        [SerializeField] TextMeshProUGUI lengthText;

        [Header("Tempera surface")]
        [SerializeField] TextMeshProUGUI holdText;

        string prefsKey;
        int surfaceIndex;

        private void Awake()
        {
            prefsKey = ConfigRegistry.UI.MainViewSurfacePrefsKey;
            surfaceIndex = Mathf.Clamp(PlayerPrefs.GetInt(prefsKey, 0), 0, Mathf.Max(0, surfaces.Length - 1));

            ShowSurface();
        }

        public void OnClickNext() => StepSurface(1);

        public void OnClickPrevious() => StepSurface(-1);

        private void StepSurface(int directionValue)
        {
            if (surfaces.Length == 0) return;

            surfaceIndex = (surfaceIndex + directionValue + surfaces.Length) % surfaces.Length;
            ShowSurface();

            PlayerPrefs.SetInt(prefsKey, surfaceIndex);
            PlayerPrefs.Save();
        }

        private void ShowSurface()
        {
            for (int i = 0; i < surfaces.Length; i++)
            {
                if (surfaces[i] != null) surfaces[i].SetActive(i == surfaceIndex);
            }
        }

        // --- button texts ---

        public void ActionOnClockModeChanged(ClockMode mode) => SetButtonText(clockModeText, "Clock", mode.ToString());

        public void ActionOnSendStartStopChanged(bool isSent) =>
            SetButtonText(startStopText, "Start/Stop", isSent ? ConfigRegistry.UI.OnText : ConfigRegistry.UI.OffText);

        public void ActionOnRecordTriggerChanged(RecordTrigger trigger) => SetButtonText(triggerText, "Trig", trigger.ToString());

        public void ActionOnRecordModeChanged(RecordMode mode) => SetButtonText(modeText, "Mode", mode.ToString());

        public void ActionOnRecordStartStateChanged(RecordStartState startState) => SetButtonText(startStateText, "Start", startState.ToString());

        public void ActionOnRecordLengthChanged(int frames) =>
            SetButtonText(lengthText, "Len", frames > 0 ? frames.ToString() : ConfigRegistry.UI.RecordLengthOffText);

        public void ActionOnHoldChanged(bool isHolding) =>
            SetButtonText(holdText, "Hold", isHolding ? ConfigRegistry.UI.OnText : ConfigRegistry.UI.OffText);

        // one line, the caption dimmed; the buttons are too short for two
        private static void SetButtonText(TextMeshProUGUI text, string caption, string value)
        {
            if (text == null) return;

            text.text = $"<alpha=#88>{caption}<alpha=#FF> {value}";
        }
    }
}
