using TemperaMental.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Frames
{
    public class FrameUiManager : MonoBehaviour
    {
        [SerializeField] Slider frameSlider;

        //  highlight image for selected colour
        [SerializeField] RectTransform selectionRing;

        // move highlight image to behind selected colour button
        public void OnClickSelectColor(Button button)
        {
            RectTransform buttonRect = button.GetComponent<RectTransform>();

            selectionRing.position = buttonRect.position;
        }

        public void ActionOnFrameChanged(VisualFrameDetail frameDetail)
        {
            frameSlider.maxValue = frameDetail.FrameTotal;
            frameSlider.value = frameDetail.FrameNumber;
        }
    }
}
