using UnityEngine;

namespace TemperaMental.Core
{
    public struct EmitterTransformDetail
    {
        public readonly bool IsLatched;
        public TransformDirections CurrentDirections;

        public EmitterTransformDetail(bool isLatched, TransformDirections currentDirections)
        {
            IsLatched = isLatched;
            CurrentDirections = currentDirections;
        }
    }
}
