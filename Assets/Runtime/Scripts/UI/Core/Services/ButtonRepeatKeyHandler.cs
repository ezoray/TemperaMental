using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.UI.Core
{
    public class ButtonRepeatKeyHandler :KeyHandlerBase
    {
        [SerializeField] DimmableRepeatableButton button;

        CanvasGroup canvasGroup;

        protected override void Awake()
        {
            base.Awake();

            canvasGroup = GetComponentInParent<CanvasGroup>();

            actionReference.action.started += StartedHandler;
            actionReference.action.canceled += CanceledHandler;
            actionReference.action.Enable();
        }

        private void StartedHandler(InputAction.CallbackContext context)
        {
            if (canvasGroup == null || canvasGroup.interactable)
            {
                button.OnPress(true);
            }
        }

        private void CanceledHandler(InputAction.CallbackContext context)
        {
            button.OnRelease();
        }

        void OnDestroy()
        {
            actionReference.action.started -= StartedHandler;
            actionReference.action.canceled -= CanceledHandler;
            actionReference.action.Disable();
        }
    }
}