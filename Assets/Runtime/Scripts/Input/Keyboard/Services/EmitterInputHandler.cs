using TemperaMental.Applications.Config;
using TemperaMental.Frames;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.Input.Keyboards
{
    public class EmitterInputHandler : InputHandlerBase
    {
        [SerializeField] FrameEventController frameEventController;

        TemperaMentalInputActions.EmitterActions emitterActions;

        private void OnBlue(InputAction.CallbackContext ctx) => frameEventController.OnClickChangeEmitter(ConfigRegistry.Grid.BlueEmitterId);
        private void OnRed(InputAction.CallbackContext ctx) => frameEventController.OnClickChangeEmitter(ConfigRegistry.Grid.RedEmitterId);
        private void OnYellow(InputAction.CallbackContext ctx) => frameEventController.OnClickChangeEmitter(ConfigRegistry.Grid.YellowEmitterId);
        private void OnGreen(InputAction.CallbackContext ctx) => frameEventController.OnClickChangeEmitter(ConfigRegistry.Grid.GreenEmitterId);

        public override void SetInputActions(TemperaMentalInputActions inputActions)
        {
            base.actionMap = emitterActions = inputActions.Emitter;

            emitterActions.Blue.performed += OnBlue;
            emitterActions.Red.performed += OnRed;
            emitterActions.Yellow.performed += OnYellow;
            emitterActions.Green.performed += OnGreen;
        }

        private void OnDisable()
        {
            emitterActions.Blue.performed -= OnBlue;
            emitterActions.Red.performed -= OnRed;
            emitterActions.Yellow.performed -= OnYellow;
            emitterActions.Green.performed -= OnGreen;

            emitterActions.Disable();
        }
    }
}
