using System;
using TemperaMental.Applications.Config;
using UnityEngine;

namespace TemperaMental.Frames
{
    // service for shifting emitter positions on the Tempera grid
    public class FrameShiftService : MonoBehaviour
    {
        int gridWidth;
        int gridHeight;

        void Awake()
        {
            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;
        }

        /// <summary>
        /// Returns a new Frame with all emitter bitmasks shifted in the given direction.
        /// The source frame is not mutated.
        /// </summary>
        public Frame Shift(Frame source, ShiftDirection direction, bool wrap)
        {
            Frame result = new Frame(source);
            ulong[] groups = result.GetEmitterGroups();

            ulong[] shifted = new ulong[groups.Length];
            for (int i = 0; i < groups.Length; i++)
                shifted[i] = ShiftBitmask(groups[i], direction, wrap);

            // Build mask of all positions occupied after shifting
            ulong allShifted = 0;
            for (int i = 0; i < shifted.Length; i++)
                allShifted |= shifted[i];

            // Write shifted results, but evict those positions from emitters that didn't shift there
            for (int i = 0; i < groups.Length; i++)
            {
                // Positions this emitter shifted into
                ulong movedInto = shifted[i] & ~groups[i];  // bits that are new after shifting
                                                            // Clear positions being moved into from all other emitters
                for (int j = 0; j < groups.Length; j++)
                {
                    if (i == j) continue;
                    groups[j] &= ~movedInto;
                }
                groups[i] = shifted[i];
            }

            return result;
        }

        ulong ShiftBitmask(ulong mask, ShiftDirection direction, bool wrap)
        {
            return direction switch
            {
                ShiftDirection.Left => ShiftLeft(mask, wrap),
                ShiftDirection.Right => ShiftRight(mask, wrap),
                ShiftDirection.Up => ShiftUp(mask, wrap),
                ShiftDirection.Down => ShiftDown(mask, wrap),
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }

        // -------------------------------------------------------------------------
        // Left — move emitters to lower x (subtract one column)
        // In index space: bits move down by gridHeight positions
        // -------------------------------------------------------------------------
        ulong ShiftLeft(ulong mask, bool wrap)
        {
            ulong lost = mask & ColumnMask(0);
            ulong shifted = (mask >> gridHeight) & ValidMask();

            if (wrap)
                shifted |= lost << ((gridWidth - 1) * gridHeight);

            return shifted;
        }

        // -------------------------------------------------------------------------
        // Right — move emitters to higher x (add one column)
        // In index space: bits move up by gridHeight positions
        // -------------------------------------------------------------------------
        ulong ShiftRight(ulong mask, bool wrap)
        {
            ulong lost = mask & ColumnMask(gridWidth - 1);
            ulong shifted = (mask << gridHeight) & ValidMask();

            if (wrap)
                shifted |= lost >> ((gridWidth - 1) * gridHeight);

            return shifted;
        }

        // -------------------------------------------------------------------------
        // Up — move emitters to higher y (decrease index within each column)
        // Processed per column to prevent bleed across column boundaries
        // -------------------------------------------------------------------------
        ulong ShiftUp(ulong mask, bool wrap)
        {
            ulong result = 0;

            for (int x = 0; x < gridWidth; x++)
            {
                ulong col = (mask & ColumnMask(x)) >> (x * gridHeight);

                ulong lost = col & 1UL;                      // top of column, lowest index
                ulong shifted = col >> 1;

                if (wrap)
                    shifted |= lost << (gridHeight - 1);     // wrap to bottom of same column

                result |= shifted << (x * gridHeight);
            }

            return result;
        }

        // -------------------------------------------------------------------------
        // Down — move emitters to lower y (increase index within each column)
        // Processed per column to prevent bleed across column boundaries
        // -------------------------------------------------------------------------
        ulong ShiftDown(ulong mask, bool wrap)
        {
            ulong result = 0;
            ulong colBits = (1UL << gridHeight) - 1;

            for (int x = 0; x < gridWidth; x++)
            {
                ulong col = (mask & ColumnMask(x)) >> (x * gridHeight);

                ulong lost = (col >> (gridHeight - 1)) & 1UL; // bottom of column, highest index
                ulong shifted = (col << 1) & colBits;

                if (wrap)
                    shifted |= lost;                           // wrap to top of same column

                result |= shifted << (x * gridHeight);
            }

            return result;
        }

        // -------------------------------------------------------------------------
        // Mask helpers
        // -------------------------------------------------------------------------

        ulong ValidMask()
        {
            int totalBits = gridWidth * gridHeight;
            return totalBits >= 64 ? ulong.MaxValue : (1UL << totalBits) - 1;
        }

        ulong ColumnMask(int column)
        {
            ulong colMask = (1UL << gridHeight) - 1;
            return colMask << (column * gridHeight);
        }
    }
}