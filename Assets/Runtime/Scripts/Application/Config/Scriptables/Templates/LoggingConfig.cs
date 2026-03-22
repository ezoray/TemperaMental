using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "LoggingConfig", menuName = "Scriptable Objects/LoggingConfig")]
    public class LoggingConfig : ScriptableObject
    {
        public string ColorInfo = "#6ACA6A";
        public string ColorWarn = "#CACA6A";
        public string ColorError = "#E01306";
    }
}
