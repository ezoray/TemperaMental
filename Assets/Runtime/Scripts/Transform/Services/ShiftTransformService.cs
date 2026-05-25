using System;
using TemperaMental.Applications.Config;
using TemperaMental.Core;

namespace TemperaMental.Transforms
{
    public class ShiftTransformService : TransformBaseService
    {
        int gridWidth;
        int gridHeight;

        bool isWrapping;

        protected override void Awake()
        {
            base.Awake();

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;

            allowedDirections = TransformDirections.Shift;
            latchableDirections = TransformLatchableDirections.Shift;
        }

        public override void ResetTransform(int masterTickCount)
        {
            base.ResetTransform(masterTickCount);
            isWrapping = false;
        }

        public bool ToggleWrap()
        {
            isWrapping = !isWrapping;
            return isWrapping;
        }

        public override ulong[] DoTransform(ulong[] groups)
        {
            if (currentDirections == TransformDirections.None)
                return groups;

            return DoSingleTransform(groups, currentDirections);
        }

        protected override ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction)
        {
            bool hasHorizontal =
                (direction & TransformDirections.Left) != 0 ||
                (direction & TransformDirections.Right) != 0;

            bool hasVertical =
                (direction & TransformDirections.Up) != 0 ||
                (direction & TransformDirections.Down) != 0;

            TransformDirections horizontalDirection =
                direction & (TransformDirections.Left | TransformDirections.Right);

            TransformDirections verticalDirection =
                direction & (TransformDirections.Up | TransformDirections.Down);

            int groupCount = groups.Length;

            ulong inactiveOccupied = 0;

            // Build occupied mask from inactive emitters
            for (int i = 0; i < groupCount; i++)
            {
                TransformEmitters emitterFlag = (TransformEmitters)(1 << i);

                if ((activeEmitters & emitterFlag) == 0)
                    inactiveOccupied |= groups[i];
            }

            // Transform active emitters
            for (int i = 0; i < groupCount; i++)
            {
                TransformEmitters emitterFlag = (TransformEmitters)(1 << i);

                // Pass inactive emitters through untouched
                if ((activeEmitters & emitterFlag) == 0)
                {
                    transformedGroups[i] = groups[i];
                    continue;
                }

                ulong mask = groups[i];

                if (hasHorizontal)
                    mask = ShiftBitmask(mask, horizontalDirection, isWrapping);

                if (hasVertical)
                    mask = ShiftBitmask(mask, verticalDirection, isWrapping);

                // Prevent overlap with inactive emitters
                transformedGroups[i] = mask & ~inactiveOccupied;
            }

            // Last-writer-wins collision resolution between active emitters
            for (int i = 0; i < transformedGroups.Length; i++)
            {
                TransformEmitters emitterFlagI = (TransformEmitters)(1 << i);

                if ((activeEmitters & emitterFlagI) == 0)
                    continue;

                ulong movedInto = transformedGroups[i] & ~groups[i];

                for (int j = 0; j < transformedGroups.Length; j++)
                {
                    if (i == j)
                        continue;

                    TransformEmitters emitterFlagJ = (TransformEmitters)(1 << j);

                    if ((activeEmitters & emitterFlagJ) == 0)
                        continue;

                    transformedGroups[j] &= ~movedInto;
                }
            }

            return transformedGroups;
        }

        private ulong ShiftBitmask(ulong mask, TransformDirections direction, bool wrap)
        {
            return direction switch
            {
                TransformDirections.Left => ShiftLeft(mask, wrap),
                TransformDirections.Right => ShiftRight(mask, wrap),
                TransformDirections.Up => ShiftUp(mask, wrap),
                TransformDirections.Down => ShiftDown(mask, wrap),
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }

        // Move emitters to lower x (subtract one column)
        private ulong ShiftLeft(ulong mask, bool wrap)
        {
            ulong lost = mask & ColumnMask(0);
            ulong shifted = (mask >> gridHeight) & ValidMask();

            if (wrap)
                shifted |= lost << ((gridWidth - 1) * gridHeight);

            return shifted;
        }

        // Move emitters to higher x (add one column)
        private ulong ShiftRight(ulong mask, bool wrap)
        {
            ulong lost = mask & ColumnMask(gridWidth - 1);
            ulong shifted = (mask << gridHeight) & ValidMask();

            if (wrap)
                shifted |= lost >> ((gridWidth - 1) * gridHeight);

            return shifted;
        }

        // Move emitters to higher y (decrease index within each column)
        private ulong ShiftUp(ulong mask, bool wrap)
        {
            ulong result = 0;

            for (int x = 0; x < gridWidth; x++)
            {
                int shift = x * gridHeight;

                ulong col = (mask & ColumnMask(x)) >> shift;

                ulong lost = col & 1UL;
                ulong shifted = col >> 1;

                if (wrap)
                    shifted |= lost << (gridHeight - 1);

                result |= shifted << shift;
            }

            return result;
        }

        // Move emitters to lower y (increase index within each column)
        private ulong ShiftDown(ulong mask, bool wrap)
        {
            ulong result = 0;
            ulong colBits = (1UL << gridHeight) - 1;

            for (int x = 0; x < gridWidth; x++)
            {
                int shift = x * gridHeight;

                ulong col = (mask & ColumnMask(x)) >> shift;

                ulong lost = (col >> (gridHeight - 1)) & 1UL;
                ulong shifted = (col << 1) & colBits;

                if (wrap)
                    shifted |= lost;

                result |= shifted << shift;
            }

            return result;
        }

        private ulong ValidMask()
        {
            int totalBits = gridWidth * gridHeight;

            return totalBits >= 64
                ? ulong.MaxValue
                : (1UL << totalBits) - 1;
        }

        private ulong ColumnMask(int column)
        {
            ulong colMask = (1UL << gridHeight) - 1;
            return colMask << (column * gridHeight);
        }

        public bool Wrap
        {
            get => isWrapping;
            set => isWrapping = value;
        }
    }
}