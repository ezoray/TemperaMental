namespace TemperaMental.Core
{
    public struct TransformDetail
    {
        public readonly TransformEmitters ActiveEmitters;
        public readonly bool IsLatched;
        public readonly TransformDirections AllowedDirections;
        public readonly TransformLatchableDirections LatchableDirections;
        public readonly TransformDirections CurrentDirections;
        public readonly float Rate;

        public TransformDetail(TransformEmitters activeEmitters, bool isLatched,
            TransformDirections allowedDirections, TransformLatchableDirections repeatDirections,
            TransformDirections currentDirections, float rate)
        {
            ActiveEmitters = activeEmitters;
            IsLatched = isLatched;
            AllowedDirections = allowedDirections;
            LatchableDirections = repeatDirections;
            CurrentDirections = currentDirections;
            Rate = rate;
        }
    }
}
