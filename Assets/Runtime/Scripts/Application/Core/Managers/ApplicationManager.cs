using TemperaMental.Applications.Config;
using TemperaMental.Input;
using TemperaMental.Input.Mouse;
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
        }

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = ConfigRegistry.App.FrameRate;

            versionText.text = "v" + Application.version;

            mouseManager.InitActions(inputActions);
        }

        private void OnDestroy()
        {
            inputActions.Dispose();
        }
    }
}
