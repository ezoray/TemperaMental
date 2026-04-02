using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "UIConfig", menuName = "Scriptable Objects/UIConfig")]
    public class UIConfig : ScriptableObject
    {
        // loop & reverse button colours
        public Color DefaultOffColor = new Color(0.55f, 0.55f, 0.55f);
        public Color LoopOnColor = new Color(0.4f, 0.8f, 0.4f);
        public Color ReverseOnColor = new Color(1.0f, 0.5f, 1f);

        // dimmable button text value
        public float AlphaValue = 0.3f;

        // display text
        public string OnText = "ON";
        public string OffText = "OFF";

        // device dropdown placeholders
        public string NoDevicesText = "No Devices";
        public string SelectDeviceText = "Select Device";

    }
}
