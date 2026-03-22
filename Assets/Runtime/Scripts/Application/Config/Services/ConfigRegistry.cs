using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [DefaultExecutionOrder(-100)]
    public class ConfigRegistry : MonoBehaviour
    {
        public static ConfigRegistry Instance { get; private set; }

        [SerializeField] LoggingConfig logging; 
        [SerializeField] GridConfig grid;
        [SerializeField] MidiConfig midi;
        [SerializeField] AppConfig app;

        public static LoggingConfig Logging => Instance.logging;
        public static GridConfig Grid => Instance.grid;
        public static MidiConfig Midi => Instance.midi;
        public static AppConfig App => Instance.app;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
    }
}
