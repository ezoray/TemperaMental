namespace TemperaMental.Core
{
    public struct TransformDetail
    {
        public readonly TransformMode TransformMode;
        public readonly TransformActiveEmitters ActiveEmitters;
        public readonly bool IsLatched;
        public readonly TransformDirections AllowedDirections;
        public readonly TransformLatchableDirections LatchableDirections;
        public readonly TransformDirections CurrentDirections;
        public readonly int Rate;

        public TransformDetail(TransformMode transformMode, TransformActiveEmitters activeEmitters, bool isLatched,
            TransformDirections allowedDirections, TransformLatchableDirections repeatDirections,
            TransformDirections currentDirections, int rate)
        {
            TransformMode = transformMode;
            ActiveEmitters = activeEmitters;
            IsLatched = isLatched;
            AllowedDirections = allowedDirections;
            LatchableDirections = repeatDirections;
            CurrentDirections = currentDirections;
            Rate = rate;
        }
    }
}
