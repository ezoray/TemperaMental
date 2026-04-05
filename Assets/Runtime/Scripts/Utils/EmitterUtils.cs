using TemperaMental.Applications.Config;
using UnityEngine;

namespace TemperaMental.Utils
{
    public static class EmitterUtils
    {
        static int gridHeight;

        public static void Initialise()
        {
            gridHeight = ConfigRegistry.Grid.GridHeight;
        }

        // tilemap to Tempera grid conversion
        public static int PositionToIndex(Vector2Int position)
        {
            int flippedY = (gridHeight - 1) - position.y;
            return position.x * gridHeight + flippedY;
        }

        // Tempera grid to tilemap conversion
        public static Vector2Int IndexToPosition(byte index)
        {
            int x = index / gridHeight;
            int y = (gridHeight - 1) - (index % gridHeight);
            return new Vector2Int(x, y);
        }
    }
}