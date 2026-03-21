using Tempera.Mental.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tempera.Mental.UI.Frames
{
    public class FrameUiManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI frameText;
        [SerializeField] Slider frameSlider;

        // background highlight image for selected colour
        [SerializeField] RectTransform selectionRing;

        [SerializeField] UnityEvent<int> onFrameNumberChanged;

        // move highlight image to behind selected colour button
        public void OnClickSelectColor(Button button)
        {
            RectTransform buttonRect = button.GetComponent<RectTransform>();

            selectionRing.position = buttonRect.position;
        }

        // frame slider
        public void OnFrameNumberChanged(float frameNumber)
        {
            onFrameNumberChanged?.Invoke((int)frameNumber);
        }

        public void ActionOnChangeFrame(VisualFrameDetail frameDetail)
        {
            frameSlider.maxValue = frameDetail.FrameTotal;
            frameSlider.value = frameDetail.FrameNumber;

            frameText.text = $"{frameDetail.FrameNumber} / {frameDetail.FrameTotal}";
        }
    }
}
