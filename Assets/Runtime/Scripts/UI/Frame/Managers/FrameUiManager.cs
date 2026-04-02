using System.Collections.Generic;
using TemperaMental.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Frames
{
    public class FrameUIManager : MonoBehaviour
    {
        [SerializeField] Slider frameSlider;

        [Header("Order: Blue, Red, Yellow, Green")]
        [SerializeField] List<Button> emitterButtons;

        // highlight image for selected colour
        [SerializeField] RectTransform selectionRing;

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

        public void ActionOnEmitterChanged(int newEmitterId)
        {
            RectTransform rectTransform = emitterButtons[newEmitterId].GetComponent<RectTransform>();

            selectionRing.position = rectTransform.position;
        }

        public void ActionOnFrameChanged(FrameDetail frameDetail)
        {
            frameSlider.maxValue = frameDetail.FrameTotal;
            frameSlider.SetValueWithoutNotify(frameDetail.FrameNumber);
        }
    }
}
