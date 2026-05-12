namespace TemperaMental.Core
{
    public struct TransformDetail
    {
        public readonly TransformEmitters ActiveEmitters;
        public readonly bool IsLatched;
        public readonly TransformDirections AllowedDirections;
        public readonly TransformLatchableDirections LatchableDirections;
        public readonly TransformDirections CurrentDirections;

        public TransformDetail(TransformEmitters activeEmitters, bool isLatched,
            TransformDirections allowedDirections, TransformLatchableDirections repeatDirections, TransformDirections currentDirections)
        {
            ActiveEmitters = activeEmitters;
            IsLatched = isLatched;
            AllowedDirections = allowedDirections;
            LatchableDirections = repeatDirections;
            CurrentDirections = currentDirections;
        }
    }
}
