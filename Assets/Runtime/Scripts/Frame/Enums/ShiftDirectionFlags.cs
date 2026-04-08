using System;

namespace TemperaMental.Frames
{
    [Flags]
    public enum ShiftDirectionFlags
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8
    }
}
