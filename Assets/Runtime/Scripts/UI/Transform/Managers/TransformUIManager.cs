using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.UI.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TemperaMental.UI.Transforms
{
    public class TransformUIManager : MonoBehaviour
    {
        [Header("Order: Blue, Red, Yellow, Green")]
        [SerializeField] DimmableButton[] emitterButtons;

        [Header("Order: Shift, Random, Flip, Rotate, Swap")]
        [SerializeField] LightableButton[] transformButtons;

        [SerializeField] Slider rateSlider;
        [SerializeField] Slider randomSlider;

        [Header("Order: Up, Down, Left, Right")]
        [SerializeField] DimmableRepeatableButton[] directionButtons;

        [SerializeField] Image latchButtonImage;
        [SerializeField] Image wrapButtonImage;

        Color defaultOffColor;
        Color latchOnColor;
        Color wrapOnColor;
        Color directionOnColor;

        Dictionary<TransformMode, TransformLitButtons> modeLitButtons;

        int rateCount;
        float[] transformRates;

        [SerializeField] UnityEvent<float> onTransformRateChanged;


        private void Awake()
        {
            modeLitButtons = new Dictionary<TransformMode, TransformLitButtons>
            {
                { TransformMode.Shift, TransformLitButtons.Shift },
                { TransformMode.Random, TransformLitButtons.Random },
                { TransformMode.Flip, TransformLitButtons.Flip },
                { TransformMode.Rotate, TransformLitButtons.Rotate },
                { TransformMode.Swap, TransformLitButtons.Swap }
            };

            defaultOffColor = ConfigRegistry.UI.DefaultColor;
            latchOnColor = ConfigRegistry.UI.GreenColor;
            wrapOnColor = ConfigRegistry.UI.OrangeColor;
            directionOnColor = ConfigRegistry.UI.CyanColor;

            randomSlider.minValue = 0;
            randomSlider.maxValue = ConfigRegistry.Grid.MaxEmitters;
            randomSlider.SetValueWithoutNotify(0);

            transformRates = new float[ConfigRegistry.Transform.RatePairs.Count];
            rateCount = transformRates.Length;

            for (int i = 0; i < ConfigRegistry.Transform.RatePairs.Count; i++)
            {
                transformRates[i] = ConfigRegistry.Transform.RatePairs[i].Value;
            }

            rateSlider.minValue = 0;
            rateSlider.maxValue = rateCount - 1;
            rateSlider.SetValueWithoutNotify(rateCount - 1);
        }

        public void ActionOnTransformsReset()
        {
            wrapButtonImage.color = defaultOffColor;
            SetElementsByLatchState(false);

            foreach (var emitter in emitterButtons)
            {
                emitter.SetDimmed(false);
            }

            rateSlider.SetValueWithoutNotify(rateCount - 1);
        }

        public void ActionOnDirectionLatchStateChanged(TransformDirections direction, bool isLatched)
        {
            int directionIndex = 0;

            for (int i = 0; i < directionButtons.Length; i++)
            {
                if (direction == (TransformDirections)(1 << i))
                {
                    directionIndex = i;
                    break;
                }
            }

            directionButtons[directionIndex].image.color = isLatched ? directionOnColor : defaultOffColor;
        }

        public void ActionOnLatchStateChanged(bool isOn)
        {
            SetElementsByLatchState(isOn);
        }

        private void SetElementsByLatchState(bool isOn)
        {
            latchButtonImage.color = isOn ? latchOnColor : defaultOffColor;

            foreach (var directionButton in directionButtons)
            {
                directionButton.SetNoRepeat(isOn);

                if (!isOn)
                {
                    directionButton.image.color = defaultOffColor;
                }
            }
        }    

        public void ActionWrapStateChanged(bool isOn)
        {
            wrapButtonImage.color = isOn ? wrapOnColor : defaultOffColor;
        }

        public void ActionOnRateSliderChanged(float sliderValue)
        {
            float multiplier = transformRates[(int)sliderValue];
            onTransformRateChanged?.Invoke(multiplier);
        }

        public void ActionOnRemoveEmitter(Vector2Int position, int emitterCount)
        {
            randomSlider.SetValueWithoutNotify(emitterCount);
        }

        public void ActionOnAddEmitter(EmitterDetail emitterDetail)
        {
            randomSlider.SetValueWithoutNotify(emitterDetail.EmitterCount);
        }

        public void ActionOnFrameChanged(FrameDetail frameDetail)
        {
            randomSlider.SetValueWithoutNotify(frameDetail.EmitterCount);
        }

        public void ActionOnTransformEmitterChanged(int emitterId, bool isActive)
        {
            emitterButtons[emitterId].SetDimmed(!isActive);
        }

        public void ActionOnTransformModeChanged(TransformMode transformMode, TransformDetail transformDetail)
        {
            SetActiveEmitters(transformDetail.ActiveEmitters);

            if (modeLitButtons.TryGetValue(transformMode, out var litButtons))
            {
                SetModeLitState(litButtons);
            }

            SetDirectionsInteractable(transformDetail.AllowedDirections);

            latchButtonImage.color = transformDetail.IsLatched ? latchOnColor : defaultOffColor;

            for (int i = 0; i < directionButtons.Length; i++)
            {
                bool inLatchableMask = ((int)transformDetail.LatchableDirections & (1 << i)) != 0;
                bool isLatched = transformDetail.IsLatched && inLatchableMask;
                directionButtons[i].SetNoRepeat(isLatched);

                bool isLit = isLatched && ((int)transformDetail.CurrentDirections & (1 << i)) != 0;
                directionButtons[i].image.color = isLit ? directionOnColor : defaultOffColor;
            }

            int index = System.Array.IndexOf(transformRates, transformDetail.Rate);
            rateSlider.SetValueWithoutNotify(index);
        }

        private void SetDirectionsInteractable(TransformDirections directions)
        {
            for (int i = 0; i < directionButtons.Length; i++)
            {
                bool isEnabled = ((int)directions & (1 << i)) != 0;

                directionButtons[i].interactable = isEnabled;
            }
        }

        private void SetModeLitState(TransformLitButtons litButtons)
        {
            for (int i = 0; i < transformButtons.Length; i++)
            {
                bool isLit = ((int)litButtons & (1 << i)) != 0;

                transformButtons[i].SetLit(isLit);
            }
        }

        private void SetActiveEmitters(TransformEmitters activeEmitters)
        {
            for (int i = 0; i < emitterButtons.Length; i++)
            {
                bool isActive = ((int)activeEmitters & (1 << i)) != 0;

                emitterButtons[i].SetDimmed(!isActive);
            }
        }
    }
}
