using UnityEngine;

namespace Tempera.Mental.Frames
{
    public struct EmitterDetail
    {
        public Vector3Int Position { get; set; }
        public int EmitterId { get; set; }

        public EmitterDetail(Vector3Int position, int emitterId)
        {
            Position = position;
            EmitterId = emitterId;
        }
    }
}
