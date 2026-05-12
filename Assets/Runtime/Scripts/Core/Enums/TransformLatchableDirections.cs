using System;

namespace TemperaMental.Core
{
    [Flags]
    public enum TransformLatchableDirections
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,

        // latchable direction buttons per transform
        Random = Left | Right,  
        Flip = Up | Down | Left | Right,
        Rotate = Left | Right,
        Swap = Up | Down | Left | Right,
        Shift = Up | Down | Left | Right
    }
}
