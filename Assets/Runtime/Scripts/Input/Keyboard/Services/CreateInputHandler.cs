using TemperaMental.Frames;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.Input.Keyboards
{
    public class CreateInputHandler : InputHandlerBase
    {
        [SerializeField] FrameEventController frameEventController;

        TemperaMentalInputActions.CreateActions createActions;

        private void OnDuplicate(InputAction.CallbackContext ctx) => frameEventController.OnClickDuplicateFrame();
        private void OnCopy(InputAction.CallbackContext ctx) => frameEventController.OnClickCopyFrame();
        private void OnNew(InputAction.CallbackContext ctx) => frameEventController.OnClickNewFrame();
        private void OnPaste(InputAction.CallbackContext ctx) => frameEventController.OnClickPasteFrame();
        private void OnClear(InputAction.CallbackContext ctx) => frameEventController.OnClickClearFrame();
        private void OnDelete(InputAction.CallbackContext ctx) => frameEventController.OnClickDeleteFrame();
        private void OnDeleteAll(InputAction.CallbackContext ctx) => frameEventController.OnClickDeleteAllFrames();

        public override void SetInputActions(TemperaMentalInputActions inputActions)
        {
            base.actionMap = createActions = inputActions.Create;

            createActions.Duplicate.performed += OnDuplicate;
            createActions.Copy.performed += OnCopy;
            createActions.New.performed += OnNew;
            createActions.Paste.performed += OnPaste;
            createActions.Clear.performed += OnClear;
            createActions.Delete.performed += OnDelete;
            createActions.DeleteAll.performed += OnDeleteAll;
        }

        private void OnDisable()
        {
            createActions.Duplicate.performed -= OnDuplicate;
            createActions.Copy.performed -= OnCopy;
            createActions.New.performed -= OnNew;
            createActions.Paste.performed -= OnPaste;
            createActions.Clear.performed -= OnClear;
            createActions.Delete.performed -= OnDelete;
            createActions.DeleteAll.performed -= OnDeleteAll;

            createActions.Disable();
        }
    }
}
