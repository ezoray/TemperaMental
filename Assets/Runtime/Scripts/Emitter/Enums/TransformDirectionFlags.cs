using System;

namespace TemperaMental.Emitters
{
    [Flags]
    public enum TransformDirectionFlags
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8
    }
}
