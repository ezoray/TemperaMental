using TemperaMental.Applications.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Core
{
    // due to button text not dimming when button is disabled override it and handle it ourselves
    public class DimmableTextButton : Button
    {
        [SerializeField] UIConfig uiConfig;

        // hack this needs to be wired under Debug in the inspector as the field does not show normally
        [SerializeField] TextMeshProUGUI buttonText;

        float alphaValue;

        protected override void Awake()
        {
            base.Awake();

            alphaValue = uiConfig.DimAlphaValue;
        }

        // adjust alpha rather than replace colour as DoStateTransition can be called in editor and before Awake
        // if colour is set at run time that can lead to all text set to an unset Color (black)
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if(state == SelectionState.Normal || state == SelectionState.Disabled)
            {
                float targetAlpha = (state == SelectionState.Disabled) ? alphaValue : 1f;

                Color color = buttonText.color;
                color.a = targetAlpha;
                buttonText.color = color;
            }
        }
    }
}
