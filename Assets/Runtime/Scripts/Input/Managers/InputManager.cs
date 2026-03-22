using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TemperaMental.Input
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] float dragDelay = 0.15f; // seconds before "Drag" mode starts
        [SerializeField] float dragDistanceThreshold = 10f; // pixels moved before "Drag" starts
        [SerializeField] float processRate = 0.05f; // process drag logic every 50ms (20Hz)

        bool isEnabled;
        bool isLeftPressed;
        bool isRightPressed;

        [SerializeField] UnityEvent<Vector2> onLeftClick;
        [SerializeField] UnityEvent<Vector2> onRightClick;

        private List<RaycastResult> raycastResults = new List<RaycastResult>();

        private void Start() => isEnabled = true;

        float holdTimer;
        Vector2 startMousePos;
        float nextProcessTime;
        bool isDragging;

        private void Update()
        {
            if (!isEnabled || (!isLeftPressed && !isRightPressed)) return;

            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            holdTimer += Time.deltaTime;

            // determine if we've transitioned from a potential click to a definite drag
            if (!isDragging)
            {
                float dist = Vector2.Distance(currentMousePos, startMousePos);
                if (holdTimer > dragDelay || dist > dragDistanceThreshold)
                {
                    isDragging = true;
                }
            }

            // only run the logic at the 'processRate' interval
            if (isDragging && Time.time >= nextProcessTime)
            {
                HandleDrag(isLeftPressed ? onLeftClick : onRightClick);
                nextProcessTime = Time.time + processRate;
            }
        }

        public void OnMouseLeftClick(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                isLeftPressed = true;
                holdTimer = 0;
                isDragging = false;
                startMousePos = Mouse.current.position.ReadValue();
            }
            else if (context.canceled)
            {
                // if released quickly without dragging, treat it as a single click
                if (!isDragging) HandleDrag(onLeftClick);
                isLeftPressed = false;
            }
        }


        private void HandleDrag(UnityEvent<Vector2> actionEvent)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            if (IsInterfaceTouch(mousePosition)) return;

            actionEvent?.Invoke(mousePosition);
        }

        public void OnMouseRightClick(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                isRightPressed = true;
                holdTimer = 0;
                isDragging = false;
                startMousePos = Mouse.current.position.ReadValue();
            }
            else if (context.canceled)
            {
                if (!isDragging) HandleDrag(onRightClick);
                isRightPressed = false;
            }
        }

        private bool IsInterfaceTouch(Vector2 mousePosition)
        {
            if (EventSystem.current == null) return false;
            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = mousePosition };
            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            foreach (var result in raycastResults)
            {
                if (result.gameObject.layer == LayerMask.NameToLayer("UI")) return true;
            }
            return false;
        }

        public void SetEnable(bool isEnabled)
        {
            this.isEnabled = isEnabled;
            if (!isEnabled) { isLeftPressed = false; isRightPressed = false; }
        }
    }
}