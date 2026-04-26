namespace TemperaMental.UI.Emitters
{
    public enum EmitterTransformSelectableFlags
    {
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
        Latch = 16,
        Wrap = 32,
        RandomSlider = 64,

        Random = RandomSlider | Left | Right | Latch,
        Flip = Up | Down | Left | Right | Latch,
        Rotate = Left | Right | Latch,
        Swap = Up | Down | Left | Right | Latch,
        Shift = Up | Down | Left | Right | Wrap | Latch
    }
}
