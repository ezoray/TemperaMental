using TemperaMental.Applications.Config;
using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Sound
{
    // the canvas number and the Hold switch, shown in Play and Sound modes
    public class SoundUIManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI canvasValue;
        [SerializeField] TextMeshProUGUI holdValue;

        // Program Change 0–127, shown 1–128; -1 is no canvas
        public void ActionOnCanvasChanged(int program)
        {
            canvasValue.text = program >= 0 ? (program + 1).ToString() : ConfigRegistry.UI.NoCanvasText;
        }

        public void ActionOnHoldChanged(bool isHolding)
        {
            holdValue.text = isHolding ? ConfigRegistry.UI.OnText : ConfigRegistry.UI.OffText;
        }
    }
}
