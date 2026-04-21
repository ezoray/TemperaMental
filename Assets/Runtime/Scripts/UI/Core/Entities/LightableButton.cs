using TemperaMental.Applications.Config;
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

        Color litColor;
        Color unlitColor;

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
        }

        public void SetLit(bool isLit)
        {
            buttonImage.color = isLit ? litColor : unlitColor;
        }
    }
}
