using TemperaMental.Applications.Config;
using UnityEngine;
namespace TemperaMental.Utils
{
    public static class EmitterUtils
    {
        static int gridHeight;
        static int cellsPerLane;
        public static readonly ulong[] LaneMasks =
        {
            0xFFFFUL,
            0xFFFF0000UL,
            0xFFFF00000000UL,
            0xFFFF000000000000UL
        };
        public static void Initialise()
        {
            gridHeight = ConfigRegistry.Grid.GridHeight;
            cellsPerLane = gridHeight * 2;
        }
        // After Simple-mode collision resolution, reassigns any emitter's bits that fall
        // inside another emitter's active 2-Lane territory to that lane owner.
        public static void ReassignLaneBitsAcrossActive(ulong[] groups, bool[] twoLaneActive)
        {
            for (int laneEmitterId = 0; laneEmitterId < groups.Length; laneEmitterId++)
            {
                if (!twoLaneActive[laneEmitterId])
                    continue;
                ulong laneMask = LaneMasks[laneEmitterId];
                for (int sourceId = 0; sourceId < groups.Length; sourceId++)
                {
                    if (sourceId == laneEmitterId)
                        continue;
                    ulong stray = groups[sourceId] & laneMask;
                    if (stray == 0)
                        continue;
                    groups[laneEmitterId] |= stray;
                    groups[sourceId] &= ~stray;
                }
            }
        }
        // Distributes 'mask' bits into transformedGroups, reassigning any bit that falls
        // within another emitter's active 2-Lane territory to that emitter's group instead.
        // Uses |= (not =) when writing the stayed bits back to sourceEmitterId's slot, so
        // foreign bits already placed there by an earlier emitter's turn this same tick are
        // preserved rather than overwritten. Returns early when mask is zero so no slot
        // is touched unnecessarily.
        public static void ReassignLaneBits(ulong[] transformedGroups, ulong mask, int sourceEmitterId, bool[] twoLaneActive)
        {
            if (mask == 0)
                return;
            ulong stays = mask;
            for (int laneEmitterId = 0; laneEmitterId < transformedGroups.Length; laneEmitterId++)
            {
                if (laneEmitterId == sourceEmitterId || !twoLaneActive[laneEmitterId])
                    continue;
                ulong laneMask = LaneMasks[laneEmitterId];
                ulong reassigned = mask & laneMask;
                if (reassigned == 0)
                    continue;
                transformedGroups[laneEmitterId] |= reassigned;
                stays &= ~reassigned;
            }
            transformedGroups[sourceEmitterId] |= stays;
        }
        public static int GetLaneOwner(int temperaIndex, bool[] twoLaneActive)
        {
            int laneEmitterId = temperaIndex / cellsPerLane;
            return twoLaneActive[laneEmitterId] ? laneEmitterId : -1;
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