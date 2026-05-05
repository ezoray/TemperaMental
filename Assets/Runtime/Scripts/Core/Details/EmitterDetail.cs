using TemperaMental.Utils;
using UnityEngine;

namespace TemperaMental.Core
{
    public struct EmitterDetail
    {
        public readonly Vector2Int Position;
        public readonly int EmitterId;
        public readonly ulong[] EmitterGroups;
        public readonly int EmitterCount;

        public EmitterDetail(Vector2Int position, int emitterId, ulong[] emitterGroups)
        {
            Position = position;
            EmitterId = emitterId;
            EmitterGroups = emitterGroups;
            EmitterCount = EmitterUtils.GetEmitterCount(emitterGroups);
        }

        public EmitterDetail(Vector2Int position, int emitterId)
        {
            Position = position;
            EmitterId = emitterId;
            EmitterGroups = default;
            EmitterCount = default;
        }
    }
}
