using UnityEngine;
using TMPro;

namespace Tempera.Mental.Logs
{
    public class LogMan : MonoBehaviour
    {
        const string INFO = "#6ACA6A";
        const string WARN = "#CACA6A";
        const string ERROR = "#E01306";

        public static LogMan Instance;
        public TextMeshProUGUI logDisplay;

        void Awake()
        {
            Instance = this;
        }

        public static void Log(string message)
        {
            ProcessLog(message, INFO, LogType.Log);
        }

        public static void LogWarning(string message)
        {
            ProcessLog(message, WARN, LogType.Warning);
        }

        public static void LogError(string message)
        {
            ProcessLog(message, ERROR, LogType.Error);
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
