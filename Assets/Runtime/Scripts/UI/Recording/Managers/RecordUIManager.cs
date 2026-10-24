using System.Collections;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Recording
{
    // the Record button: lit while recording (transform results or the Tempera), flashing while armed
    public class RecordUIManager : MonoBehaviour
    {
        [SerializeField] Button recordButton;

        Color defaultOffColor;
        Color recordOnColor;
        float flashInterval;

        bool isRecordingTransforms;
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
            isRecordingTransforms = isOn;
            Refresh();
        }

        // recording from the Tempera
        public void ActionOnTemperaRecordStateChanged(RecordState state)
        {
            temperaState = state;
            Refresh();
        }

        private void Refresh()
        {
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

            bool isOn = isRecordingTransforms || temperaState == RecordState.Recording;
            recordButton.image.color = isOn ? recordOnColor : defaultOffColor;
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
