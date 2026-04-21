using TemperaMental.Applications.Config;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TemperaMental.UI.Core
{
    public class DimmableRepeatableButton : Button
    {
        [SerializeField] AppConfig appConfig;
        [SerializeField] UIConfig uiConfig;
        [SerializeField] TextMeshProUGUI buttonText;

        [SerializeField] bool doRepeat;
        [SerializeField] float initialDelay;

        float alphaValue;
        float repeatRate;
        bool isPressed;
        float nextEventTime;

        protected override void Awake()
        {
            base.Awake();
            alphaValue = uiConfig.DimAlphaValue;
            initialDelay = appConfig.InitialDelay;
            repeatRate = appConfig.RepeatRate;
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (buttonText != null && (state == SelectionState.Normal || state == SelectionState.Disabled))
            {
                float targetAlpha = (state == SelectionState.Disabled) ? alphaValue : 1f;
                Color c = buttonText.color;
                c.a = targetAlpha;
                buttonText.color = c;
            }
        }

        void Update()
        {
            if (!doRepeat || !isPressed) return;

            if (Time.time >= nextEventTime)
            {
                onClick.Invoke();
                nextEventTime = Time.time + repeatRate;
            }
        }

        public void SetRepeat(bool isOn)
        {
            doRepeat = isOn;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (!interactable) return;
            OnPress();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (!interactable) return;
            OnRelease();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (!interactable) return;
            OnRelease();
        }

        public void OnPress()
        { 
            isPressed = true;
            nextEventTime = Time.time + initialDelay;
        }

        public void OnRelease()
        {
            isPressed = false;
        }
    }
}