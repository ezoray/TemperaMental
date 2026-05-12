using System;

namespace TemperaMental.UI.Transforms
{
    [Flags]
    public enum TransformLitButtons
    {
        Shift = 1,
        Random = 2,
        Flip = 4,
        Rotate = 8,
        Swap = 16
    }
}