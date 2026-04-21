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

        Dictionary<EmitterTransformMode, EmitterTransformUIState> transformModeStates;

        [SerializeField] UnityEvent<float> onRandomValueChanged;

        private void Awake()
        {
            transformModeStates = new Dictionary<EmitterTransformMode, EmitterTransformUIState>
            {
                { EmitterTransformMode.Random, new EmitterTransformUIState(EmitterTransformLitFlags.Random, EmitterTransformSelectableFlags.Random) },
                { EmitterTransformMode.Flip, new EmitterTransformUIState(EmitterTransformLitFlags.Flip, EmitterTransformSelectableFlags.Flip) }
            };

            randomSlider.minValue = 0;
            randomSlider.maxValue = ConfigRegistry.Grid.MaxEmitters;
            randomSlider.SetValueWithoutNotify(0);
        }


        private void OnEnable()
        {
        
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

        public void ActionOnRandomEmitterCountChanged(int emitterCount)
        {
            randomSlider.SetValueWithoutNotify(emitterCount);
        }

        public void ActionOnRemoveEmitter(Vector2Int position)
        {
            randomSlider.SetValueWithoutNotify(randomSlider.value--);
        }

        public void ActionOnAddEmitter(EmitterDetail emitterDetail)
        {
            int emitterCount = EmitterUtils.GetEmitterCount(emitterDetail.EmitterGroups);

            randomSlider.SetValueWithoutNotify(emitterCount);
        }

        public void ActionOnFrameChanged(FrameDetail frameDetail)
        {
            int emitterCount = EmitterUtils.GetEmitterCount(frameDetail.EmitterGroups);

            randomSlider.value = emitterCount;
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
