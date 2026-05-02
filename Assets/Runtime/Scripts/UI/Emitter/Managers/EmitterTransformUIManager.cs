using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.UI.Core;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TemperaMental.UI.Emitters
{
    public class EmitterTransformUIManager : MonoBehaviour
    {
        [Header("Order: Blue, Red, Yellow, Green")]
        [SerializeField] DimmableButton[] emitterButtons;

        [Header("Order: Random, Flip, Rotate, Switch, Shift")]
        [SerializeField] LightableButton[] modeButtons;

        [SerializeField] Slider randomSlider;

        [Header("Order: Up, Down, Left, Right")]
        [SerializeField] DimmableRepeatableButton[] directionButtons;

        [SerializeField] Image latchButtonImage;
        [SerializeField] Image wrapButtonImage;

        Color defaultOffColor;
        Color latchOnColor;
        Color wrapOnColor;
        Color directionOnColor;

        Dictionary<EmitterTransformMode, EmitterTransformUIState> transformModeStates;

        [SerializeField] UnityEvent<float> onRandomValueChanged;

        private void Awake()
        {
            transformModeStates = new Dictionary<EmitterTransformMode, EmitterTransformUIState>
            {
                { EmitterTransformMode.Random, new EmitterTransformUIState(TransformLitButtons.Random, TransformDirections.Random) },
                { EmitterTransformMode.Flip, new EmitterTransformUIState(TransformLitButtons.Flip, TransformDirections.Flip) },
                { EmitterTransformMode.Rotate, new EmitterTransformUIState(TransformLitButtons.Rotate, TransformDirections.Rotate) },
                { EmitterTransformMode.Swap, new EmitterTransformUIState(TransformLitButtons.Swap, TransformDirections.Swap) },
                { EmitterTransformMode.Shift, new EmitterTransformUIState(TransformLitButtons.Shift, TransformDirections.Shift) }
            };

            randomSlider.minValue = 0;
            randomSlider.maxValue = ConfigRegistry.Grid.MaxEmitters;
            randomSlider.SetValueWithoutNotify(0);

            defaultOffColor = ConfigRegistry.UI.DefaultColor;
            latchOnColor = ConfigRegistry.UI.GreenColor;
            wrapOnColor = ConfigRegistry.UI.PurpleColor;
            directionOnColor = ConfigRegistry.UI.CyanColor;
        }

        private void Start()
        {
            // set Shift as initial enabled transform
            if (transformModeStates.TryGetValue(EmitterTransformMode.Shift, out var transformUIState))
            {
                SetModeLitState(transformUIState.LitButtons);
                SetDirectionsInteractable(transformUIState.Directions);
            }
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
            for (int i = 0; i < modeButtons.Length; i++)
            {
                bool isLit = ((int)litButtons & (1 << i)) != 0;

                modeButtons[i].SetLit(isLit);
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
            latchButtonImage.color = isOn ? latchOnColor : defaultOffColor;

            foreach (var directionButton in directionButtons)
            {
                directionButton.SetRepeat(!isOn);

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

        public void ActionOnRemoveEmitter(Vector2Int position)
        {
            randomSlider.SetValueWithoutNotify(randomSlider.value - 1);
        }

        public void ActionOnAddEmitter(EmitterDetail emitterDetail)
        {
            int emitterCount = EmitterUtils.GetEmitterCount(emitterDetail.EmitterGroups);

            randomSlider.SetValueWithoutNotify(emitterCount);
        }

        public void ActionOnFrameChanged(FrameDetail frameDetail)
        {
            int emitterCount = EmitterUtils.GetEmitterCount(frameDetail.EmitterGroups);

            randomSlider.SetValueWithoutNotify(emitterCount);
        }

        public void ActionOnTransformEmitterChanged(int emitterId, bool isActive)
        {
            emitterButtons[emitterId].SetDimmed(!isActive);
        }

        public void ActionOnTransformModeChanged(EmitterTransformMode transformMode, EmitterTransformDetail transformDetail)
        {
            SetActiveEmitters(transformDetail.ActiveEmitters);

            if (transformModeStates.TryGetValue(transformMode, out var transformUIState))
            {
                SetModeLitState(transformUIState.LitButtons);
                SetDirectionsInteractable(transformUIState.Directions);                
            }

            latchButtonImage.color = transformDetail.IsLatched ? latchOnColor : defaultOffColor;

            for (int i = 0; i < directionButtons.Length; i++)
            {
                directionButtons[i].SetRepeat(!transformDetail.IsLatched);

                bool isLit = ((int)transformDetail.CurrentDirections & (1 << i)) != 0;

                directionButtons[i].image.color = isLit ? directionOnColor : defaultOffColor;
            }
        }
    }
}
