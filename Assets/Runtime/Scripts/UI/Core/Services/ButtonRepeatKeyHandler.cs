using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.UI.Core
{
    public class ButtonRepeatKeyHandler : KeyHandlerBase
    {
        [SerializeField] DimmableRepeatableButton button;

        CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponentInParent<CanvasGroup>();
        }

        private void OnEnable()
        {
            actionReference.action.performed += PerformedHandler;
            actionReference.action.started += StartedHandler;
            actionReference.action.canceled += CanceledHandler;
            actionReference.action.Enable();
        }

        private void PerformedHandler(InputAction.CallbackContext context)
        {
            if (canvasGroup == null || canvasGroup.interactable)
            {
                button.onClick.Invoke();
            }
        }

        private void StartedHandler(InputAction.CallbackContext context)
        {
            if (canvasGroup == null || canvasGroup.interactable)
            {
                button.OnPress();
            }
        }

        private void CanceledHandler(InputAction.CallbackContext context)
        {
            button.OnRelease();
        }

        private void OnDisable()
        {
            actionReference.action.performed -= PerformedHandler;
            actionReference.action.started -= StartedHandler;
            actionReference.action.canceled -= CanceledHandler;
            actionReference.action.Disable();
        }
    }
}