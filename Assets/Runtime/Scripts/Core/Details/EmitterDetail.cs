using UnityEngine;

namespace TemperaMental.Core
{
    public struct EmitterDetail
    {
        public readonly Vector2Int Position;
        public readonly int EmitterId;
        public readonly int EmitterCount;

        // the change was made on the Tempera itself, so it must not be sent back to it
        public readonly bool IsFromTempera;

        public EmitterDetail(Vector2Int position, int emitterId, int emitterCount, bool isFromTempera = false)
        {
            Position = position;
            EmitterId = emitterId;
            EmitterCount = emitterCount;
            IsFromTempera = isFromTempera;
        }
    }
}
