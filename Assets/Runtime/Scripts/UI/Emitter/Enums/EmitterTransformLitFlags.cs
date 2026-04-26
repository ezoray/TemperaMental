using System;

namespace TemperaMental.UI.Emitters
{
    [Flags]
    public enum EmitterTransformLitFlags
    {
        Random = 1,
        Flip = 2,
        Rotate = 4,
        Swap = 8,
        Shift = 16
    }
}