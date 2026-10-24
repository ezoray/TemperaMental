using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "UIConfig", menuName = "Scriptable Objects/UIConfig")]
    public class UIConfig : ScriptableObject
    {
        // button colours
        public Color DefaultColor = new Color(0.55f, 0.55f, 0.55f);
        public Color YellowColor = new Color(0.85f, 0.85f, 0f);
        public Color GreenColor = new Color(0.4f, 0.8f, 0.4f);
        public Color PurpleColor = new Color(1f, 0.5f, 1f);
        public Color CyanColor = new Color(0.5f, 0.8f, 1f);
        public Color OrangeColor = new Color(1f, 0.5f, 0f);
        public Color RedColor = new Color(1f, 0.25f, 0f);

        // dimmable button text alpha value
        public float DimAlphaValue = 0.3f;

        // dimmable button image colour reduction
        public float ColorDimFactor = 0.6f;

        // display text
        public string OnText = "ON";
        public string OffText = "OFF";

        // button text
        public string Play = "Play";
        public string Pause = "Pause";

        // device dropdown placeholders
        public string NoDevicesText = "Connect Midi Device";
        public string SelectDeviceText = "Select Midi Device";

        // record button: how fast it flashes while armed (seconds per change)
        public float RecordArmFlashInterval = 0.35f;

        // record settings
        public string RecordLengthOffText = "Off";

        // the page last shown (Compose, Play, Transform, Record, Setup)
        public string AppPagePrefsKey = "AppPage";

        // the canvas display when no canvas is chosen
        public string NoCanvasText = "-";

        // clock devices
        public string SameAsInText = "Same as In";
        public string SameAsOutText = "Same as Out";
        public string NoneText = "None";

        // fading log message
        public float TempMessageDuration = 2f;
        public float TempMessageFadeDuration = 0.5f;
    }
}
