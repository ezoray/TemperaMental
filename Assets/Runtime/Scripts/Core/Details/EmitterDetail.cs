using UnityEngine;

namespace TemperaMental.Core
{
    public struct EmitterDetail
    {
        public Vector2Int Position { get; }
        public int EmitterId { get; }
        public ulong[] EmitterGroups { get; }

        public EmitterDetail(Vector2Int position, int emitterId, ulong[] emitterGroups = default)
        {
            Position = position;
            EmitterId = emitterId;
            EmitterGroups = emitterGroups;
        }
    }
}
