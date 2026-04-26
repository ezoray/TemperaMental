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

        [Header("Order: Up, Down, Left, Right, Latch, Wrap, Slider")]
        [SerializeField] Selectable[] selectables;

        [SerializeField] Slider randomSlider;

        [Header("Order: Up, Down, Left, Right")]
        [SerializeField] List<DimmableRepeatableButton> shiftButtons;

        [SerializeField] Image latchButtonImage;
        [SerializeField] Image wrapButtonImage;

        Color defaultOffColor;
        Color latchOnColor;
        Color wrapOnColor;
        Color shiftOnColor;


        Dictionary<EmitterTransformMode, EmitterTransformUIState> transformModeStates;

        [SerializeField] UnityEvent<float> onRandomValueChanged;

        private void Awake()
        {
            transformModeStates = new Dictionary<EmitterTransformMode, EmitterTransformUIState>
            {
                { EmitterTransformMode.Random, new EmitterTransformUIState(EmitterTransformLitFlags.Random, EmitterTransformSelectableFlags.Random) },
                { EmitterTransformMode.Flip, new EmitterTransformUIState(EmitterTransformLitFlags.Flip, EmitterTransformSelectableFlags.Flip) },
                { EmitterTransformMode.Rotate, new EmitterTransformUIState(EmitterTransformLitFlags.Rotate, EmitterTransformSelectableFlags.Rotate) },
                { EmitterTransformMode.Swap, new EmitterTransformUIState(EmitterTransformLitFlags.Swap, EmitterTransformSelectableFlags.Swap) },
                { EmitterTransformMode.Shift, new EmitterTransformUIState(EmitterTransformLitFlags.Shift, EmitterTransformSelectableFlags.Shift) }
            };

            randomSlider.minValue = 0;
            randomSlider.maxValue = ConfigRegistry.Grid.MaxEmitters;
            randomSlider.SetValueWithoutNotify(0);

            defaultOffColor = ConfigRegistry.UI.DefaultColor;
            latchOnColor = ConfigRegistry.UI.GreenColor;
            wrapOnColor = ConfigRegistry.UI.PurpleColor;
            shiftOnColor = ConfigRegistry.UI.CyanColor;
        }

        private void Start()
        {
            // set Shift as initial enabled transform
            if (transformModeStates.TryGetValue(EmitterTransformMode.Shift, out var transformUIState))
            {
                SetButtonsLitState(transformUIState.LitFlags);
                SetSelectablesInteractable(transformUIState.SelectableFlags);
            }
        }

        private void SetSelectablesInteractable(EmitterTransformSelectableFlags selectableFlags)
        {
            for (int i = 0; i < selectables.Length; i++)
            {
                bool isEnabled = ((int)selectableFlags & (1 << i)) != 0;

                selectables[i].interactable = isEnabled;
            }
        }

        private void SetButtonsLitState(EmitterTransformLitFlags litFlags)
        {
            for (int i = 0; i < modeButtons.Length; i++)
            {
                bool isLit = ((int)litFlags & (1 << i)) != 0;

                modeButtons[i].SetLit(isLit);
            }
        }

        public void ActionOnDirectionLatchStateChanged(int direction, bool isLatched)
        {
            shiftButtons[direction].image.color = isLatched ? shiftOnColor : defaultOffColor;
        }

        public void ActionOnLatchStateChanged(bool isOn)
        {
            latchButtonImage.color = isOn ? latchOnColor : defaultOffColor;

            foreach (var shiftButton in shiftButtons)
            {
                shiftButton.SetRepeat(!isOn);

                if (!isOn)
                {
                    shiftButton.image.color = defaultOffColor;
                }
            }
        }

        public void ActionWrapStateChanged(bool isOn)
        {
            wrapButtonImage.color = isOn ? wrapOnColor : defaultOffColor;
        }

        //public void ActionOnEmittersTransformed(ulong[] emitterGroup)
        //{
        //    int emitterCount = EmitterUtils.GetEmitterCount(emitterGroup);

        //    randomSlider.SetValueWithoutNotify(emitterCount);
        //}

        public void ActionOnRemoveEmitter(Vector2Int position)
        {
            randomSlider.SetValueWithoutNotify(randomSlider.value -1);
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
          //  randomSlider.value = emitterCount;
        }

        public void ActionOnTransformEmitterChanged(int emitterId, bool isEnabled)
        {
            emitterButtons[emitterId].SetDimmed(!isEnabled);
        }

        public void ActionOnTransformModeChanged(EmitterTransformMode transformMode)
        {
            if (transformModeStates.TryGetValue(transformMode, out var transformUIState))
            {
                SetButtonsLitState(transformUIState.LitFlags);
                SetSelectablesInteractable(transformUIState.SelectableFlags);
            }
        }
    }
}
