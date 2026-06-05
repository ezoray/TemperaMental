using System.Collections;
using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Display.MainView
{
    public class MainViewManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI bpmText;
        [SerializeField] TextMeshProUGUI transformText;
        [SerializeField] TextMeshProUGUI frameText;
        [SerializeField] TextMeshProUGUI emittersText;
        [SerializeField] TextMeshProUGUI logText;

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

        public void ActionOnTransformRateChanged(float rate)
        {
            if(transformRates.TryGetValue(rate, out var label))
            {
                transformText.text = label;
            }
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

        private void SetMessageAlpha(float alpha)
        {
            Color color = logText.color;
            color.a = alpha;
            logText.color = color;
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
