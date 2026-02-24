using UnityEngine;
using TMPro;

namespace Tempera.Mental.Logs
{
    public class LogMan : MonoBehaviour
    {
        public static LogMan Instance;
        public TextMeshProUGUI logDisplay;

        void Awake() => Instance = this;

        // --- Public API ---

        public static void Log(string message)
            => ProcessLog(message, "white", LogType.Log);

        public static void LogWarning(string message)
            => ProcessLog(message, "yellow", LogType.Warning);

        public static void LogError(string message)
            => ProcessLog(message, "red", LogType.Error);

        // --- Internal Logic ---

        private static void ProcessLog(string message, string colorHex, LogType type)
        {
            if (Instance == null) return;

            // 1. Update UI (Rich Text for colors)
            string formattedMessage = $"<color={colorHex}>{message}</color>";
            Instance.AddToUI(formattedMessage);

            // 2. Editor Console (Stripped in builds)
#if UNITY_EDITOR
            switch (type)
            {
                case LogType.Warning: Debug.LogWarning(message); break;
                case LogType.Error: Debug.LogError(message); break;
                default: Debug.Log(message); break;
            }
#endif
        }

        private void AddToUI(string message)
        {
            if (logDisplay != null)
            {
                // Add new log to the bottom
                logDisplay.text = message;
            }
        }
    }
}
