using UnityEngine;

namespace Tempera.Mental.Frames
{
    public struct EmitterDetail
    {
        public Vector2Int Position { get; set; }
        public int EmitterId { get; set; }

        public EmitterDetail(Vector2Int position, int emitterId)
        {
            Position = position;
            EmitterId = emitterId;
        }
    }
}
