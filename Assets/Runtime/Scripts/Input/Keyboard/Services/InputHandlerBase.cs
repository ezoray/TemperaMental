using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.Input
{
    public abstract class InputHandlerBase : MonoBehaviour
    {
        protected InputActionMap actionMap;

        public abstract void SetInputActions(TemperaMentalInputActions inputActions);

        public virtual void SetEnabled(bool isEnabled)
        {
            if (actionMap == null) return;

            if (isEnabled) actionMap.Enable(); else actionMap.Disable();
        }
    }
}
