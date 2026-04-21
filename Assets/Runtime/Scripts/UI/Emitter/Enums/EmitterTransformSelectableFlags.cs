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

        Random = RandomSlider,
        Flip = Up | Down | Left | Right
    }
}
