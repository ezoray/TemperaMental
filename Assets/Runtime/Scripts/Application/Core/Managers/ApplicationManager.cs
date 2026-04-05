using TemperaMental.Applications.Config;
using TemperaMental.Input;
using TemperaMental.Input.Mouse;
using TemperaMental.Utils;
using TMPro;
using UnityEngine;

namespace TemperaMental.Applications.Core
{
    public class ApplicationManager : MonoBehaviour
    {
        [SerializeField] MouseManager mouseManager;
        [SerializeField] TextMeshProUGUI versionText;

        TemperaMentalInputActions inputActions;

        private void Awake()
        {
            inputActions = new TemperaMentalInputActions();

            EmitterUtils.Initialise();
        }

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = ConfigRegistry.App.FrameRate;

            versionText.text = "v" + Application.version;

            mouseManager.InitActions(inputActions);
        }

        public void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            inputActions.Dispose();
        }
    }
}
