using TemperaMental.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Frames
{
    public class FrameUiManager : MonoBehaviour
    {
        [SerializeField] Slider frameSlider;

        // highlight image for selected colour
        [SerializeField] RectTransform selectionRing;

        // move highlight image to behind selected colour button
        public void OnClickSelectColor(Button button)
        {
            RectTransform buttonRect = button.GetComponent<RectTransform>();

            selectionRing.position = buttonRect.position;
        }

        public void OnClickNextFrame()
        {
            frameSlider.value++;
        }

        public void OnClickPreviousFrame()
        {
            frameSlider.value--;
        }

        public void OnClickStartFrame()
        {
            frameSlider.value = frameSlider.minValue;
        }

        public void OnClickEndFrame()
        {
            frameSlider.value = frameSlider.maxValue;
        }

        public void ActionOnFrameChanged(FrameDetail frameDetail)
        {
            frameSlider.maxValue = frameDetail.FrameTotal;
            frameSlider.SetValueWithoutNotify(frameDetail.FrameNumber);
        }
    }
}
