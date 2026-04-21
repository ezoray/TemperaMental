using System;

namespace TemperaMental.Core
{
    [Flags]
    public enum TransformEmitterFlags
    {
        None = 0,
        Blue = 1,
        Red = 2,
        Yellow = 4,
        Green = 8,

        All = Blue | Red | Yellow | Green
    }
}
