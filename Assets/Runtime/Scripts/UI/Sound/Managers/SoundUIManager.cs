using TemperaMental.Applications.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Sound
{
    // the canvas number (Compose and Play pages) and the Hold switch (next to Play and Stop on every page)
    public class SoundUIManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI canvasValue;

        [Tooltip("Optional ON / OFF text")]
        [SerializeField] TextMeshProUGUI holdValue;

        [Tooltip("Optional: lit while a note is held")]
        [SerializeField] Image holdImage;

        // Program Change 0–127, shown 1–128; -1 is no canvas
        public void ActionOnCanvasChanged(int program)
        {
            canvasValue.text = program >= 0 ? (program + 1).ToString() : ConfigRegistry.UI.NoCanvasText;
        }

        public void ActionOnHoldChanged(bool isHolding)
        {
            if (holdValue != null) holdValue.text = isHolding ? ConfigRegistry.UI.OnText : ConfigRegistry.UI.OffText;
            if (holdImage != null) holdImage.color = isHolding ? ConfigRegistry.UI.CyanColor : ConfigRegistry.UI.DefaultColor;
        }
    }
}
