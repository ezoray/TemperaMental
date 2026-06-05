using UnityEngine;

namespace TemperaMental.Core
{
    public struct EmitterDetail
    {
        public readonly Vector2Int Position;
        public readonly int EmitterId;
        public readonly int EmitterCount;

        public EmitterDetail(Vector2Int position, int emitterId, int emitterCount)
        {
            Position = position;
            EmitterId = emitterId;
            EmitterCount = emitterCount;
        }
    }
}
