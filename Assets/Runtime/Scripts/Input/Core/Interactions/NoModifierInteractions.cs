using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.Input
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    // custom interation for prevent conflict with keys also shared with modifiers (eg C and Ctrl-C)
    // all simple bindings require this interaction
    public class NoModifierInteraction : IInputInteraction
    {
        static NoModifierInteraction()
        {
            InputSystem.RegisterInteraction<NoModifierInteraction>();
        }

        [RuntimeInitializeOnLoadMethod]
        static void Initialize() => InputSystem.RegisterInteraction<NoModifierInteraction>();

        public void Process(ref InputInteractionContext context)
        {
            if (Keyboard.current.ctrlKey.isPressed ||
                Keyboard.current.shiftKey.isPressed ||
                Keyboard.current.altKey.isPressed)
            {
                context.Canceled();
                return;
            }

            if (context.ControlIsActuated())
            {
                context.PerformedAndStayPerformed();
            }
            else
            {
                context.Canceled();
            }
        }

        public void Reset() { }
    }
}
