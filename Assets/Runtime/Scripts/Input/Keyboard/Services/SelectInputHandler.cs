using TemperaMental.UI.Frames;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.Input.Keyboards
{
    public class SelectInputHandler : InputHandlerBase
    {
        [SerializeField] FrameUIManager frameUIManager;

        TemperaMentalInputActions.SelectActions selectActions;

        private void OnStart(InputAction.CallbackContext ctx) => frameUIManager.OnClickStartFrame();
        private void OnPrevious(InputAction.CallbackContext ctx) => frameUIManager.OnClickPreviousFrame();
        private void OnNext(InputAction.CallbackContext ctx) => frameUIManager.OnClickNextFrame();
        private void OnEnd(InputAction.CallbackContext ctx) => frameUIManager.OnClickEndFrame();

        public override void SetInputActions(TemperaMentalInputActions inputActions)
        {
            base.actionMap = selectActions = inputActions.Select;

            selectActions.Start.performed += OnStart;
            selectActions.Previous.performed += OnPrevious;
            selectActions.Next.performed += OnNext;
            selectActions.End.performed += OnEnd;
        }

        private void OnDisable()
        {
            selectActions.Start.performed -= OnStart;
            selectActions.Previous.performed -= OnPrevious;
            selectActions.Next.performed -= OnNext;
            selectActions.End.performed -= OnEnd;

            selectActions.Disable();
        }
    }
}
