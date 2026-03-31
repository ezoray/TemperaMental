using TemperaMental.Applications.Config;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DimmableRepeatableButton : Button
{
    [SerializeField] AppConfig appConfig;
    [SerializeField] TextMeshProUGUI buttonText;

    float alphaValue;
    float initialDelay;
    float repeatRate;

    bool isPressed = false;
    float nextActionTime;

    protected override void Awake()
    {
        base.Awake();

        alphaValue = appConfig.AlphaValue;
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

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);

        if (!interactable) return;

        isPressed = true;
        nextActionTime = Time.time + initialDelay;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        isPressed = false;
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        isPressed = false;
    }

    private void Update()
    {
        if (!isPressed || !interactable) return;
        
        if (Time.time >= nextActionTime)
        {
            onClick.Invoke();
            nextActionTime = Time.time + repeatRate;
        }
    }
}