using UnityEngine;
using TMPro;
using TemperaMental.Applications.Config;

namespace TemperaMental.Logs
{
    public class LogMan : MonoBehaviour
    {
        static string info;
        static string warn;
        static string error;

        public static LogMan Instance;
        public TextMeshProUGUI logDisplay;

        void Awake()
        {
            Instance = this;

            info = ConfigRegistry.Logging.ColorInfo;
            warn = ConfigRegistry.Logging.ColorWarn;
            error = ConfigRegistry.Logging.ColorError;
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
            Instance.LogToUI(formattedMessage);

#if UNITY_EDITOR
            switch (type)
            {
                case LogType.Warning: Debug.LogWarning(message);
                    break;

                case LogType.Error: Debug.LogError(message);
                    break;

                default: Debug.Log(message);
                    break;
            }
#endif
        }

        private void LogToUI(string message)
        {
            if (logDisplay != null)
            {
                // Add new log to the bottom
                logDisplay.text = message;
            }
        }
    }
}
