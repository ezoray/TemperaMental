using UnityEngine;
using TMPro;
using TemperaMental.Core;
using TemperaMental.Applications.Config;
using System.Collections;
using TemperaMental.Logs;

namespace TemperaMental.UI.Display
{
    public class DisplayUIManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI bpmText;
        [SerializeField] TextMeshProUGUI frameText;
        [SerializeField] TextMeshProUGUI emittersText;
        [SerializeField] TextMeshProUGUI logText;

        float tempMessageDuration;
        float tempMessageFadeDuration;

        private Coroutine tempMessageCoroutine;
        private string lastLogMessage;

        private void Awake()
        {
            tempMessageDuration = ConfigRegistry.UI.TempMessageDuration;
            tempMessageFadeDuration = ConfigRegistry.UI.TempMessageFadeDuration;
        }

        public void ActionOnRemoveEmitter(Vector2Int position, int emitterCount)
        {
            emittersText.text = $"{emitterCount}";
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
