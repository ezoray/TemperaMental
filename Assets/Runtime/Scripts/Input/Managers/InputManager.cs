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

 
        public void OnMouseLeftClick(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Debug.Log("InputManager OnMouseLeftClick " + mousePosition);

            if (IsInterfaceTouch())
            {
                Debug.Log("Click blocked by UI");
                return;
            }

            OnLeftClick?.Invoke(Mouse.current.position.ReadValue());
        }

        public void OnMouseRightClick(InputAction.CallbackContext context)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Debug.Log("InputManager OnMouseRightClick " + mousePosition);

            if (IsInterfaceTouch())
            {
                Debug.Log("Click blocked by UI");
                return;
            }

            OnRightClick?.Invoke(Mouse.current.position.ReadValue());
        }

        private bool IsInterfaceTouch()
        {
            if (EventSystem.current == null)
                return false;

            return EventSystem.current.IsPointerOverGameObject();
        }


        //// Keep this list as a member variable to avoid memory allocation every click
        //private List<RaycastResult> _results = new List<RaycastResult>();

        //private bool IsInterfaceTouch(Vector2 mousePosition)
        //{
        //    if (EventSystem.current == null) return false;

        //    PointerEventData eventData = new PointerEventData(EventSystem.current)
        //    {
        //        position = mousePosition
        //    };

        //    _results.Clear();
        //    EventSystem.current.RaycastAll(eventData, _results);

        //    foreach (var result in _results)
        //    {
        //        // Check for specific layer OR specific tags/components
        //        if (result.gameObject.layer == LayerMask.NameToLayer("UI"))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        //private void ProcessTileClick(string buttonLabel)
        //{
        //    // Read mouse position directly from the device
        //    Vector2 mousePos = Mouse.current.position.ReadValue();

        //    // Convert screen -> world -> cell
        //    Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mousePos);
        //    Vector3Int cellPosition = targetTilemap.WorldToCell(worldPoint);

        //    Debug.Log($"{buttonLabel} click detected at tile coordinate: {cellPosition}");
        //}
    }
}
