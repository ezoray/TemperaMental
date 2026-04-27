using System;

namespace TemperaMental.Core
{
    [Flags]
    public enum TransformDirections
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,

        Random = Left | Right,
        Flip = Up | Down | Left | Right,
        Rotate = Left | Right,
        Swap = Up | Down | Left | Right,
        Shift = Up | Down | Left | Right
    }
}
