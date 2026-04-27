using TemperaMental.Core;

namespace TemperaMental.UI.Emitters
{
    public struct EmitterTransformUIState
    {
        public TransformLitButtons LitButtons;
        public TransformDirections Directions;

        public EmitterTransformUIState(TransformLitButtons litButtons, TransformDirections directions)
        {
            LitButtons = litButtons;
            Directions = directions;
        }
    }
}
