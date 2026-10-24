using System.Collections;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Recording
{
    // The Record page's Record button flashes while armed and is lit while recording from the Tempera. The Transform
    // page's Rec results button is lit (and says ON) while transform results are recorded.
    public class RecordUIManager : MonoBehaviour
    {
        [SerializeField] Button recordButton;

        [SerializeField] Button transformRecordButton;
        [SerializeField] TextMeshProUGUI transformRecordValue;

        Color defaultOffColor;
        Color recordOnColor;
        float flashInterval;

        RecordState temperaState;
        Coroutine flashing;

        private void Awake()
        {
            defaultOffColor = ConfigRegistry.UI.DefaultColor;
            recordOnColor = ConfigRegistry.UI.RedColor;
            flashInterval = ConfigRegistry.UI.RecordArmFlashInterval;
        }

        // recording transform results
        public void ActionOnRecordStateChanged(bool isOn)
        {
            if (transformRecordButton != null) transformRecordButton.image.color = isOn ? recordOnColor : defaultOffColor;
            if (transformRecordValue != null) transformRecordValue.text = isOn ? ConfigRegistry.UI.OnText : ConfigRegistry.UI.OffText;
        }

        // recording from the Tempera
        public void ActionOnTemperaRecordStateChanged(RecordState state)
        {
            temperaState = state;

            if (flashing != null)
            {
                StopCoroutine(flashing);
                flashing = null;
            }

            if (temperaState == RecordState.Armed && isActiveAndEnabled)
            {
                flashing = StartCoroutine(Flash());
                return;
            }

            recordButton.image.color = temperaState == RecordState.Recording ? recordOnColor : defaultOffColor;
        }

        private IEnumerator Flash()
        {
            var wait = new WaitForSecondsRealtime(flashInterval);
            bool isOn = true;

            while (true)
            {
                recordButton.image.color = isOn ? recordOnColor : defaultOffColor;
                isOn = !isOn;

                yield return wait;
            }
        }
    }
}
