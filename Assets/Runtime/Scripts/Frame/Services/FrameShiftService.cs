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

        // returns a new Frame with all emitter bitmasks shifted in the given direction.
        public Frame Shift(Frame frame, ShiftDirection direction, bool wrap)
        {
            ulong[] groups = frame.GetEmitterGroups(); // direct ref to backing array

            ulong[] shifted = new ulong[groups.Length];
            for (int i = 0; i < groups.Length; i++)
                shifted[i] = ShiftBitmask(groups[i], direction, wrap);

            for (int i = 0; i < shifted.Length; i++)
            {
                ulong movedInto = shifted[i] & ~groups[i];
                for (int j = 0; j < groups.Length; j++)
                {
                    if (i == j) continue;
                    groups[j] &= ~movedInto;
                }
                groups[i] = shifted[i];
            }

            return frame;
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

        // move emitters to lower x(subtract one column)
        ulong ShiftLeft(ulong mask, bool wrap)
        {
            ulong lost = mask & ColumnMask(0);
            ulong shifted = (mask >> gridHeight) & ValidMask();

            if (wrap)
                shifted |= lost << ((gridWidth - 1) * gridHeight);

            return shifted;
        }

        // move emitters to higher x(add one column)
        ulong ShiftRight(ulong mask, bool wrap)
        {
            ulong lost = mask & ColumnMask(gridWidth - 1);
            ulong shifted = (mask << gridHeight) & ValidMask();

            if (wrap)
                shifted |= lost >> ((gridWidth - 1) * gridHeight);

            return shifted;
        }

        // move emitters to higher y(decrease index within each column)
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

        // move emitters to lower y(increase index within each column)
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

        private ulong ValidMask()
        {
            int totalBits = gridWidth * gridHeight;
            return totalBits >= 64 ? ulong.MaxValue : (1UL << totalBits) - 1;
        }

        private ulong ColumnMask(int column)
        {
            ulong colMask = (1UL << gridHeight) - 1;
            return colMask << (column * gridHeight);
        }
    }
}