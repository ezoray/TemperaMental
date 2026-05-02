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

        private void OnDestroy()
        {
            actionReference.action.performed -= PerformedHandler;
            actionReference.action.Disable();
        } 
    }
}
