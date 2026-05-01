using TemperaMental.Input;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TemperaMental.UI.Core
{
    public class KeyHandlerBase : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] protected InputActionReference actionReference;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right) return;
            if (actionReference == null) return;

            LogMan.Log($"Rebinding {actionReference.action.name} " +
                $"[{GetBoundKeyName(actionReference.action)}] — press a key, or Escape to cancel");

            KeybindManager.Instance.StartRebind(
                actionReference.action,
                OnRebindComplete,
                OnRebindConflict,
                OnRebindCancelled
            );
        }

        private void OnRebindComplete(InputAction action)
        {
            LogMan.Log($"{action.name} rebound to [{GetBoundKeyName(actionReference.action)}]");
        }

        private void OnRebindConflict(InputAction action, string conflictActionName)
        {
            if (conflictActionName == null)
                LogMan.Log($"{action.name} [{GetBoundKeyName(actionReference.action)}] — this binding cannot be rebound");
            else
                LogMan.Log($"{action.name} rebind cancelled — key is already bound to {conflictActionName}");
        }

        private void OnRebindCancelled()
        {
            LogMan.Log("Rebind cancelled");
        }

        protected string GetBoundKeyName(InputAction action)
        {
            return KeybindManager.Instance.GetBindingDisplayString(action);
        }
    }
}