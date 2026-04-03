using UnityEngine;
using TMPro;
using TemperaMental.Core;
using TemperaMental.Applications.Config;
using System.Collections;

namespace TemperaMental.UI.Display
{
    public class DisplayUIManager : MonoBehaviour
    {
        public string onText;
        public string offText;

        [SerializeField] TextMeshProUGUI bpmText;
        [SerializeField] TextMeshProUGUI frameText;
        [SerializeField] TextMeshProUGUI reverseText;
        [SerializeField] TextMeshProUGUI loopText;
        [SerializeField] TextMeshProUGUI logText;

        float tempMessageDuration;
        float tempMessageFadeDuration;

        private void Awake()
        {
            onText = ConfigRegistry.UI.OnText;
            offText = ConfigRegistry.UI.OffText;

            bpmText.text = ConfigRegistry.Midi.DefaultBpm.ToString();
            tempMessageDuration = ConfigRegistry.UI.TempMessageDuration;
            tempMessageFadeDuration = ConfigRegistry.UI.TempMessageFadeDuration;
        }

        private Coroutine tempMessageCoroutine;
        private string lastLogMessage;


        public void ActionOnTempMessage(string message)
        {
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

        public void ActionOnReverseStateChanged(bool isReversed)
        {
            reverseText.text = isReversed ? onText : offText;
        }

        public void ActionOnLoopStateChanged(bool isLooping)
        {
            loopText.text = isLooping ? onText : offText;
        }

        public void ActionOnFrameChanged(FrameDetail frameDetail)
        {
            frameText.text = $"{frameDetail.FrameNumber} / {frameDetail.FrameTotal}";
        }

        public void ActionOnBpmChanged(int bpm)
        {
            bpmText.text = bpm.ToString();
        }

    }
}
