using UnityEngine;

namespace TemperaMental.Core
{
    public struct EmitterDetail
    {
        readonly public Vector2Int Position;
        readonly public int EmitterId;

        public EmitterDetail(Vector2Int position, int emitterId)
        {
            Position = position;
            EmitterId = emitterId;
        }
    }
}
