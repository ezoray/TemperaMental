using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "AppConfig", menuName = "Scriptable Objects/AppConfig")]
    public class AppConfig : ScriptableObject
    {
        public int FrameRate = 60;
    }
}
