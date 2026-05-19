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

        public static bool CheckGroupsDifferent(ulong[] aGroup, ulong[] bGroup)
        {
            if (aGroup == null || bGroup == null)
                return false;

            for (int i = 0; i < aGroup.Length; i++)
            {
                if (aGroup[i] != bGroup[i])
                    return true;
            }

            return false;
        }

        public static int GetEmitterCount(ulong[] emitterGroups)
        {
            int placedEmitterCount = 0;

            foreach (ulong mask in emitterGroups)
            {
                ulong v = mask;
                while (v != 0) { v &= v - 1; placedEmitterCount++; }
            }

            return placedEmitterCount;
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