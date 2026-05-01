using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "AppConfig", menuName = "Scriptable Objects/AppConfig")]
    public class AppConfig : ScriptableObject
    {
        public int FrameRate = 60;

        // app aspect ratio handling
        public float TargetRatio = 3f / 4f;

        public int MinWidth = 540;
        public int MinHeight = 720;

        // mouse drag settings
        public float DragDelay = 0.1f;
        public float DragDistanceThreshold = 10f;
        public float ProcessRate = 0.025f;

        // repeatable button values
        public float InitialDelay = 0.6f;
        public float RepeatRate = 0.1f;
    }
}
