using System;
using System.Collections.Generic;
using TemperaMental.Applications.Config;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TemperaMental.Input.Mouse
{
    public class MouseManager : MonoBehaviour
    {
        [SerializeField] float dragDelay;
        [SerializeField] float dragDistanceThreshold;
        [SerializeField] float processRate;

        TemperaMentalInputActions.MouseActions mouseActions;
        bool isInitialised;

        bool isLeftPressed;
        bool isRightPressed;
        bool isLeftDragging;
        bool isRightDragging;

        float leftHoldTimer;
        float rightHoldTimer;
        float nextLeftProcessTime;
        float nextRightProcessTime;
        Vector2 leftStartPos;
        Vector2 rightStartPos;
        Vector2 lastLeftDragPos;
        Vector2 lastRightDragPos;

        [SerializeField] UnityEvent<Vector2> onLeftClick;
        [SerializeField] UnityEvent<Vector2> onRightClick;

        readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        private void Awake()
        {
            dragDelay = ConfigRegistry.App.DragDelay;
            dragDistanceThreshold = ConfigRegistry.App.DragDistanceThreshold;
            processRate = ConfigRegistry.App.ProcessRate;
        }

        public void SetMouseActions(TemperaMentalInputActions.MouseActions mouseActions)
        {
            this.mouseActions = mouseActions;

            isInitialised = true;

            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void OnEnable()
        {
            if (!isInitialised) return;
            Subscribe();
        }

        private void Update()
        {
            Vector2 currentMousePos = mouseActions.Position.ReadValue<Vector2>();

            if (isLeftPressed)
            {
                leftHoldTimer += Time.deltaTime;

                if (!isLeftDragging)
                {
                    float dist = Vector2.Distance(currentMousePos, leftStartPos);
                    if (leftHoldTimer > dragDelay || dist > dragDistanceThreshold)
                        isLeftDragging = true;
                }

                if (isLeftDragging && Time.time >= nextLeftProcessTime)
                {
                    bool isCtrlHeld = Keyboard.current.ctrlKey.isPressed;
                    HandleDrag(isCtrlHeld ? onRightClick : onLeftClick, ref lastLeftDragPos);
                    nextLeftProcessTime = Time.time + processRate;
                }
            }

            if (isRightPressed)
            {
                rightHoldTimer += Time.deltaTime;

                if (!isRightDragging)
                {
                    float dist = Vector2.Distance(currentMousePos, rightStartPos);
                    if (rightHoldTimer > dragDelay || dist > dragDistanceThreshold)
                        isRightDragging = true;
                }

                if (isRightDragging && Time.time >= nextRightProcessTime)
                {
                    HandleDrag(onRightClick, ref lastRightDragPos);
                    nextRightProcessTime = Time.time + processRate;
                }
            }
        }

        private void OnLeftClickStarted(InputAction.CallbackContext context)
        {
            isLeftPressed = true;
            leftHoldTimer = 0;
            isLeftDragging = false;
            leftStartPos = mouseActions.Position.ReadValue<Vector2>();
        }

        private void OnLeftClickCanceled(InputAction.CallbackContext context)
        {
            if (!isLeftDragging)
            {
                Vector2 mousePosition = mouseActions.Position.ReadValue<Vector2>();
                if (!IsInterfaceTouch(mousePosition))
                    onLeftClick?.Invoke(mousePosition);
            }
            isLeftPressed = false;
            isLeftDragging = false;
        }

        private void OnRightClickStarted(InputAction.CallbackContext context)
        {
            isRightPressed = true;
            rightHoldTimer = 0;
            isRightDragging = false;
            rightStartPos = mouseActions.Position.ReadValue<Vector2>();
        }

        private void OnRightClickCanceled(InputAction.CallbackContext context)
        {
            if (!isRightDragging)
            {
                Vector2 mousePosition = mouseActions.Position.ReadValue<Vector2>();
                if (!IsInterfaceTouch(mousePosition))
                    onRightClick?.Invoke(mousePosition);
            }
            isRightPressed = false;
            isRightDragging = false;
        }

        private void HandleDrag(UnityEvent<Vector2> actionEvent, ref Vector2 lastPos)
        {
            Vector2 mousePosition = mouseActions.Position.ReadValue<Vector2>();

            if (mousePosition == lastPos) return;
            lastPos = mousePosition;

            if (IsInterfaceTouch(mousePosition)) return;

            actionEvent?.Invoke(mousePosition);
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

        private void Subscribe()
        {
            mouseActions.LeftClick.started += OnLeftClickStarted;
            mouseActions.LeftClick.canceled += OnLeftClickCanceled;
            mouseActions.RightClick.started += OnRightClickStarted;
            mouseActions.RightClick.canceled += OnRightClickCanceled;
            mouseActions.Enable();
        }

        private void Unsubscribe()
        {
            mouseActions.LeftClick.started -= OnLeftClickStarted;
            mouseActions.LeftClick.canceled -= OnLeftClickCanceled;
            mouseActions.RightClick.started -= OnRightClickStarted;
            mouseActions.RightClick.canceled -= OnRightClickCanceled;
            mouseActions.Disable();
        }

        private void OnDisable()
        {
            if (!isInitialised) return;
            Unsubscribe();
        }
    }
}