using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TemperaMental.UI.Core
{
    public class ButtonKeyHandler : KeyHandlerBase
    {        
        [SerializeField] Button button;

        CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponentInParent<CanvasGroup>();
        }

        private void OnEnable()
        {
            actionReference.action.performed += PerformedHandler;
            actionReference.action.Enable();
        }

        private void PerformedHandler(InputAction.CallbackContext context)
        {
            if (canvasGroup == null || canvasGroup.interactable)
            {
                button.onClick.Invoke();
            }
        }

        private void OnDisable()
        {
            actionReference.action.performed -= PerformedHandler;
            actionReference.action.Disable();
        }
    }
}
