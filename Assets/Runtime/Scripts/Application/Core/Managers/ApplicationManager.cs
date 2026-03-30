using TemperaMental.Applications.Config;
using TemperaMental.Input;
using TemperaMental.Input.Keyboards;
using TemperaMental.Input.Mouse;
using UnityEngine;

namespace TemperaMental.Applications.Core
{
    public class ApplicationManager : MonoBehaviour
    {
        [SerializeField] MouseManager mouseManager;
        [SerializeField] KeyboardManager keyboardManager;

        TemperaMentalInputActions inputActions;

        private void Awake()
        {
            inputActions = new TemperaMentalInputActions();
        }

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = ConfigRegistry.App.FrameRate;

            keyboardManager.InitActions(inputActions);
            mouseManager.InitActions(inputActions);
        }

        private void OnDestroy()
        {
            inputActions.Dispose();
        }
    }
}
