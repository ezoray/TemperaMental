using System;
using TemperaMental.Applications.Config;
using TemperaMental.Core;

namespace TemperaMental.Emitters
{
    public class ShiftTransformService : TransformBaseService
    {
        int gridWidth, gridHeight;
        bool wrap;

        private void OnEnable()
        {
            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;
        }

        public override ulong[] DoTransform(ulong[] groups, TransformEmitterFlags activeEmitters, TransformDirectionFlags directions)
        {
            ulong[] transformedGroups = groups;

            for (int i = 0; i < 4; i++)
            {
                TransformDirectionFlags directionFlag = (TransformDirectionFlags)(1 << i);

                if (directions.HasFlag(directionFlag))
                {
                    transformedGroups = DoSingleTransform(transformedGroups, activeEmitters, directionFlag);
                }
            }

            return transformedGroups;
        }

        // returns a emitter array with all emitter bitmasks shifted in the given direction
        private ulong[] DoSingleTransform(ulong[] groups, TransformEmitterFlags activeEmitters, TransformDirectionFlags direction)
        {
            // Build a mask of all positions occupied by inactive emitters
            ulong inactiveOccupied = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                if (!activeEmitters.HasFlag((TransformEmitterFlags)(1 << i)))
                    inactiveOccupied |= groups[i];
            }

            ulong[] result = new ulong[groups.Length];

            for (int i = 0; i < groups.Length; i++)
            {
                bool isActive = activeEmitters.HasFlag((TransformEmitterFlags)(1 << i));

                if (!isActive)
                {
                    // Inactive emitters are copied unchanged
                    result[i] = groups[i];
                    continue;
                }

                // Shift the active emitter and strip any bits that land on inactive-occupied positions
                result[i] = ShiftBitmask(groups[i], direction, wrap) & ~inactiveOccupied;
            }

            // Resolve collisions between active emitters — last-writer wins per original logic,
            // but only among active slots. Build allAdds to suppress removes as in FrameMidiPlayer.
            for (int i = 0; i < result.Length; i++)
            {
                if (!activeEmitters.HasFlag((TransformEmitterFlags)(1 << i))) continue;

                ulong movedInto = result[i] & ~groups[i];

                for (int j = 0; j < result.Length; j++)
                {
                    if (i == j) continue;
                    if (!activeEmitters.HasFlag((TransformEmitterFlags)(1 << j))) continue;

                    // Strip positions that emitter i has moved into from other active emitters
                    result[j] &= ~movedInto;
                }
            }

            return result;
        }

        ulong ShiftBitmask(ulong mask, TransformDirectionFlags direction, bool wrap)
        {
            return direction switch
            {
                TransformDirectionFlags.Left => ShiftLeft(mask, wrap),
                TransformDirectionFlags.Right => ShiftRight(mask, wrap),
                TransformDirectionFlags.Up => ShiftUp(mask, wrap),
                TransformDirectionFlags.Down => ShiftDown(mask, wrap),
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

        public bool Wrap { get => wrap; set => wrap = value; }
    }
}
