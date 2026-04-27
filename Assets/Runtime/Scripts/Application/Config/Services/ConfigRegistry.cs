using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [DefaultExecutionOrder(-100)]
    public class ConfigRegistry : MonoBehaviour
    {
        public static ConfigRegistry Instance { get; private set; }

        [SerializeField] LoggingConfig logging; 
        [SerializeField] AppConfig app;
        [SerializeField] EmitterConfig emitter;
        [SerializeField] GridConfig grid;
        [SerializeField] MidiConfig midi;
        [SerializeField] UIConfig ui;

        public static LoggingConfig Logging => Instance.logging;
        public static AppConfig App => Instance.app;
        public static EmitterConfig Emitter => Instance.emitter;
        public static GridConfig Grid => Instance.grid;
        public static MidiConfig Midi => Instance.midi;
        public static UIConfig UI => Instance.ui;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeMethodLoad()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
    }
}
