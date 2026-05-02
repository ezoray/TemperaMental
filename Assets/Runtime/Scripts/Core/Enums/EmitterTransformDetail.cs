using UnityEngine;

namespace TemperaMental.Core
{
    public struct EmitterTransformDetail
    {
        public readonly TransformEmitters ActiveEmitters;
        public readonly bool IsLatched;
        public readonly TransformDirections CurrentDirections;

        public EmitterTransformDetail(TransformEmitters activeEmitters, bool isLatched, TransformDirections currentDirections)
        {
            ActiveEmitters = activeEmitters;
            IsLatched = isLatched;
            CurrentDirections = currentDirections;
        }
    }
}
