using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.UI.Core
{
    public class ButtonKeyHandler : MonoBehaviour
    {        
        [SerializeField] DimmableTextButton button;
        [SerializeField] InputActionReference actionReference;

        private CanvasGroup canvasGroup;

        void Awake()
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

        void OnDestroy()
        {
            actionReference.action.performed -= PerformedHandler;
            actionReference.action.Disable();
        }
    }
}
