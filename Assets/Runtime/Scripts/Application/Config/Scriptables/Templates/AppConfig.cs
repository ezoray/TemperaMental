using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "AppConfig", menuName = "Scriptable Objects/AppConfig")]
    public class AppConfig : ScriptableObject
    {
        public int FrameRate = 60;

        // dimmable / repeatable button values
        public float AlphaValue = 0.3f;
        public float InitialDelay = 0.3f;
        public float RepeatRate = 0.1f;
    }
}
