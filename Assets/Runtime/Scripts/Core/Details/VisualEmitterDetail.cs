using UnityEngine;

namespace Tempera.Mental.Core
{
    public struct VisualEmitterDetail
    {
        public Vector3Int Position;
        public Color Color;

        public VisualEmitterDetail(Vector3Int position, Color color)
        {
            Position = position;
            Color = color;
        }
    }
}
