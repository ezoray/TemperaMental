using TemperaMental.Applications.Config;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Core
{
    // configure under Debug in inspector
    public class DimmableButton : Button
    {
        [SerializeField] UIConfig uiConfig;
        [SerializeField] Image buttonImage;

        Color defaultColor;
        Color dimmedColor;

        protected override void Awake()
        {
            base.Awake();

            defaultColor = buttonImage.color;
            dimmedColor = new Color(
             defaultColor.r * uiConfig.ColorDimFactor,
             defaultColor.g * uiConfig.ColorDimFactor,
             defaultColor.b * uiConfig.ColorDimFactor,
             defaultColor.a
            );
        }

        public void SetDimmed(bool isDimmed)
        {
            buttonImage.color = isDimmed ? dimmedColor : defaultColor;
        }
    }
}
