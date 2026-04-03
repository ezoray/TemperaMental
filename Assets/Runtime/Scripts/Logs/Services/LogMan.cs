using TemperaMental.Applications.Config;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Logs
{
    [DefaultExecutionOrder(-99)]
    public class LogMan : MonoBehaviour
    {
        static string info;
        static string warn;
        static string error;

        public static LogMan Instance;

        [SerializeField] UnityEvent<string> onLogMessage;
        [SerializeField] UnityEvent<string> onTempMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeMethodLoad()
        {
            Instance = null;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            info = ConfigRegistry.Logging.ColorInfo;
            warn = ConfigRegistry.Logging.ColorWarn;
            error = ConfigRegistry.Logging.ColorError;
        }

        public static void LogTemp(string message)
        {
            Instance.LogTempToDisplay(message);
        }

        public static void Log(string message)
        {
            ProcessLog(message, info, LogType.Log);
        }

        public static void LogWarning(string message)
        {
            ProcessLog(message, warn, LogType.Warning);
        }

        public static void LogError(string message)
        {
            ProcessLog(message, error, LogType.Error);
        }

        private static void ProcessLog(string message, string colorHex, LogType type)
        {
            if (Instance == null) return;

            string formattedMessage = $"<color={colorHex}>{message}</color>";
            Instance.LogToDisplay(formattedMessage);

            switch (type)
            {
                case LogType.Warning: Debug.LogWarning(message);
                    break;

                case LogType.Error: Debug.LogError(message);
                    break;

                default: Debug.Log(message);
                    break;
            }
        }

        private void LogTempToDisplay(string message)
        {
            onTempMessage?.Invoke(message);
        }

        private void LogToDisplay(string message)
        {
            onLogMessage?.Invoke(message);
        }
    }
}
