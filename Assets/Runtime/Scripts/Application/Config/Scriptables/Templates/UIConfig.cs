using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "UIConfig", menuName = "Scriptable Objects/UIConfig")]
    public class UIConfig : ScriptableObject
    {
        // button colours
        public Color DefaultColor = new Color(0.55f, 0.55f, 0.55f);
        public Color GreenColor = new Color(0.4f, 0.8f, 0.4f);
        public Color PurpleColor = new Color(1f, 0.5f, 1f);
        public Color CyanColor = new Color(0.5f, 0.8f, 1f);
        public Color RedColor = new Color(1f, 0.2f, 0f);

        // dimmable button text alpha value
        public float DimAlphaValue = 0.3f;

        // dimmable button image colour reduction
        public float ColorDimFactor = 0.75f;

        // display text
        public string OnText = "ON";
        public string OffText = "OFF";

        // device dropdown placeholders
        public string NoDevicesText = "No Devices";
        public string SelectDeviceText = "Select Device";

        // fading log message
        public float TempMessageDuration = 2f;
        public float TempMessageFadeDuration = 0.5f;
    }
}
