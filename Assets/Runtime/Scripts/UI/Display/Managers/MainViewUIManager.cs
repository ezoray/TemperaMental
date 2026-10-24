using System.Collections;
using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Display.MainView
{
    public class MainViewUIManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI bpmText;
        [SerializeField] TextMeshProUGUI transformText;
        [SerializeField] TextMeshProUGUI frameText;
        [SerializeField] TextMeshProUGUI emittersText;
        [SerializeField] TextMeshProUGUI logText;

        // status line: the clock on the left, recording on the right
        [SerializeField] TextMeshProUGUI clockStatusText;
        [SerializeField] TextMeshProUGUI recordStatusText;

        ClockMode clockMode;
        bool isExternalClockPresent;
        RecordState recordState;
        RecordTrigger recordTrigger;
        RecordMode recordMode;
        bool isRecordingTransforms;

        float tempMessageDuration;
        float tempMessageFadeDuration;

        private Coroutine tempMessageCoroutine;
        private string lastLogMessage;

        Dictionary<float, string> transformRates;


        private void Awake()
        {
            tempMessageDuration = ConfigRegistry.UI.TempMessageDuration;
            tempMessageFadeDuration = ConfigRegistry.UI.TempMessageFadeDuration;

            transformRates = new Dictionary<float, string>();

            foreach (var timePair in ConfigRegistry.Transform.RatePairs)
            {
                transformRates.Add(timePair.Value, timePair.Label);
            }
        }

        public void ActionOnTransformChanged(TransformDetail transformDetail)
        {
            SetRateTextLabel(transformDetail.Rate);
        }

        public void ActionOnTransformRateChanged(int rate)
        {
            SetRateTextLabel(rate);
        }

        public void ActionOnRemoveEmitter(EmitterDetail emitterDetail)
        {
            emittersText.text = $"{emitterDetail.EmitterCount}";
        }

        public void ActionOnAddEmitter(EmitterDetail emitterDetail)
        {           
            emittersText.text = $"{emitterDetail.EmitterCount}";
        }

        public void ActionOnTempMessage(string message)
        {
            SetMessageAlpha(1f);

            if (tempMessageCoroutine != null) StopCoroutine(tempMessageCoroutine);
            tempMessageCoroutine = StartCoroutine(ShowTempMessage(message));
        }

        public void ActionOnLogMessage(string message)
        {
            lastLogMessage = message;
            if (tempMessageCoroutine != null)
            {
                StopCoroutine(tempMessageCoroutine);
                tempMessageCoroutine = null;

                SetMessageAlpha(1f);
            }
            logText.text = message;
        }

        private IEnumerator ShowTempMessage(string message)
        {
            logText.text = message;
            yield return new WaitForSeconds(tempMessageDuration);

            float elapsed = 0f;
            while (elapsed < tempMessageFadeDuration)
            {
                elapsed += Time.deltaTime;
                SetMessageAlpha(1f - (elapsed / tempMessageFadeDuration));
                yield return null;
            }

            SetMessageAlpha(1f);

            logText.text = lastLogMessage;
            tempMessageCoroutine = null;
        }

        public void ActionOnFrameChanged(FrameDetail frameDetail)
        {
            frameText.text = $"{frameDetail.FrameNumber} / {frameDetail.FrameTotal}";

            emittersText.text = $"{frameDetail.EmitterCount}";
        }

        public void ActionOnBpmChanged(int bpm)
        {
            bpmText.text = bpm.ToString();
        }

        // --- status line ---

        public void ActionOnClockModeChanged(ClockMode mode)
        {
            clockMode = mode;
            RefreshClockStatus();
        }

        public void ActionOnExternalClockPresenceChanged(bool isPresent)
        {
            isExternalClockPresent = isPresent;
            RefreshClockStatus();
        }

        public void ActionOnTemperaRecordStateChanged(RecordState state)
        {
            recordState = state;
            RefreshRecordStatus();
        }

        public void ActionOnRecordTriggerChanged(RecordTrigger trigger)
        {
            recordTrigger = trigger;
            RefreshRecordStatus();
        }

        public void ActionOnRecordModeChanged(RecordMode mode)
        {
            recordMode = mode;
            RefreshRecordStatus();
        }

        // recording transform results
        public void ActionOnRecordingStateChanged(bool isRecording)
        {
            isRecordingTransforms = isRecording;
            RefreshRecordStatus();
        }

        private void RefreshClockStatus()
        {
            if (clockStatusText == null) return;

            clockStatusText.text = clockMode switch
            {
                ClockMode.Send => "Sending clock",
                ClockMode.Receive => isExternalClockPresent ? "External clock" : "No clock",
                _ => "Internal clock"
            };
        }

        private void RefreshRecordStatus()
        {
            if (recordStatusText == null) return;

            recordStatusText.text = recordState switch
            {
                RecordState.Armed => $"Armed: {recordTrigger}",
                RecordState.Recording => $"Rec {recordMode}",
                _ => isRecordingTransforms ? "Rec transforms" : ""
            };
        }

        private void SetRateTextLabel(int rate)
        {
            if (transformRates.TryGetValue(rate, out var rateLabel))
            {
                transformText.text = rateLabel;
            }
        }

        private void SetMessageAlpha(float alpha)
        {
            Color color = logText.color;
            color.a = alpha;
            logText.color = color;
        }

        private void OnDisable()
        {
            if (tempMessageCoroutine != null)
            {
                StopCoroutine(tempMessageCoroutine);
                tempMessageCoroutine = null;
            }
        }
    }
}
