using TemperaMental.Applications.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Core
{
    // configure under Debug in inspector
    public class LightableButton : Button
    {
        [SerializeField] UIConfig uiConfig;
        [SerializeField] ButtonColorOption buttonColorOption;
        [SerializeField] Image buttonImage;
        [SerializeField] TextMeshProUGUI buttonText;

        Color litColor;
        Color unlitColor;


        float alphaValue;

        protected override void Awake()
        {
            base.Awake();

            unlitColor = uiConfig.DefaultColor;

            litColor = buttonColorOption switch
            {
                ButtonColorOption.Purple => uiConfig.PurpleColor,
                ButtonColorOption.Green => uiConfig.GreenColor,
                ButtonColorOption.Cyan => uiConfig.CyanColor,
                _ => uiConfig.DefaultColor
            };

            alphaValue = uiConfig.DimAlphaValue;
        }

        public void SetLit(bool isLit)
        {
            buttonImage.color = isLit ? litColor : unlitColor;
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (state == SelectionState.Normal || state == SelectionState.Disabled)
            {
                float targetAlpha = (state == SelectionState.Disabled) ? alphaValue : 1f;

                Color color = buttonText.color;
                color.a = targetAlpha;
                buttonText.color = color;
            }
        }

    }
}
