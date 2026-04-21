using TemperaMental.Frames;

namespace TemperaMental.UI.Emitters
{
    public struct EmitterTransformUIState
    {
        public EmitterTransformLitFlags LitFlags;
        public  EmitterTransformSelectableFlags SelectableFlags;

        public EmitterTransformUIState(EmitterTransformLitFlags litFlags, EmitterTransformSelectableFlags selectableFlags)
        {
            this.LitFlags = litFlags;
            this.SelectableFlags = selectableFlags;
        }
    }
}
