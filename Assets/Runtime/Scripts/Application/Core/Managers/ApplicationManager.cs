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
            Application.targetFrameRate = ConfigRegistry.App.FrameRate;

            inputActions = new TemperaMentalInputActions();
            mouseManager.SetMouseActions(inputActions.Mouse);

            EmitterUtils.Initialise();
        }

        private void Start()
        {
            QualitySettings.vSyncCount = 0;


            versionText.text = "v" + Application.version;
        }

        private void OnDestroy()
        {
            inputActions.Dispose();
        }
    }
}
