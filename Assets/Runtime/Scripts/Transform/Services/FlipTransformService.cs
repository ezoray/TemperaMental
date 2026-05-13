using TemperaMental.Applications.Config;
using TemperaMental.Core;

namespace TemperaMental.Transforms
{
    public class FlipTransformService : TransformBaseService
    {
        int gridWidth;
        int gridHeight;

        protected override void Awake()
        {
            base.Awake();

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;

            allowedDirections = TransformDirections.Flip;
            latchableDirections = TransformLatchableDirections.Flip;
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

            int groupCount = groups.Length;

            for (int i = 0; i < groupCount; i++)
            {
                TransformEmitters emitterFlag = (TransformEmitters)(1 << i);

                // Skip inactive emitters
                if ((activeEmitters & emitterFlag) == 0)
                {
                    transformedGroups[i] = groups[i];
                    continue;
                }

                transformedGroups[i] = 0;

                ulong mask = groups[i];

                for (int x = 0; x < gridWidth; x++)
                {
                    int flippedX = (gridWidth - 1) - x;

                    for (int y = 0; y < gridHeight; y++)
                    {
                        int index = (x * gridHeight) + y;
                        ulong bit = 1UL << index;

                        // Skip unset bits
                        if ((mask & bit) == 0)
                            continue;

                        int newX = hasHorizontal ? flippedX : x;
                        int newY = hasVertical ? (gridHeight - 1) - y : y;

                        int newIndex = (newX * gridHeight) + newY;
                        ulong newBit = 1UL << newIndex;

                        bool blocked = false;

                        for (int j = 0; j < groupCount; j++)
                        {
                            TransformEmitters otherEmitterFlag = (TransformEmitters)(1 << j);

                            // Ignore active emitters
                            if ((activeEmitters & otherEmitterFlag) != 0)
                                continue;

                            if ((groups[j] & newBit) != 0)
                            {
                                blocked = true;
                                break;
                            }
                        }

                        if (!blocked)
                            transformedGroups[i] |= newBit;
                    }
                }
            }

            return transformedGroups;
        }
    }
}