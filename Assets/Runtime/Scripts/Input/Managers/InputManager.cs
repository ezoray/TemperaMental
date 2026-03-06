using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Tempera.Mental.Input
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] UnityEvent<Vector2> OnLeftClick;
        [SerializeField] UnityEvent<Vector2> OnRightClick;

        bool isEnabled;
        private List<RaycastResult> raycastResults = new List<RaycastResult>();


        private void Start()
        {
            isEnabled = true;
        }

        public void OnMouseLeftClick(InputAction.CallbackContext context)
        {
            if (!isEnabled) return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Debug.Log("InputManager OnMouseLeftClick " + mousePosition);

            if (IsInterfaceTouch(mousePosition))
            {
                Debug.Log("Click blocked by UI");
                return;
            }

            OnLeftClick?.Invoke(Mouse.current.position.ReadValue());
        }

        public void OnMouseRightClick(InputAction.CallbackContext context)
        {
            if (!isEnabled) return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Debug.Log("InputManager OnMouseRightClick " + mousePosition);

            if (IsInterfaceTouch(mousePosition))
            {
                Debug.Log("Click blocked by UI");
                return;
            }

            OnRightClick?.Invoke(Mouse.current.position.ReadValue());
        }

        private bool IsInterfaceTouch(Vector2 mousePosition)
        {
            if (EventSystem.current == null) return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = mousePosition };

            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            foreach (var result in raycastResults)
            {
                // Check for specific layer OR specific tags/components
                if (result.gameObject.layer == LayerMask.NameToLayer("UI"))
                {
                    return true;
                }
            }
            return false;
        }

        public void SetEnable(bool isEnabled)
        {
            this.isEnabled = isEnabled;
        }
    }
}
