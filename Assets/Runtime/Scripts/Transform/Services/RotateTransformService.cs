using TemperaMental.Applications.Config;
using TemperaMental.Core;

namespace TemperaMental.Transforms
{
    public class RotateTransformService : TransformBaseService
    {
        int gridWidth;
        int gridHeight;

        protected override void Awake()
        {
            base.Awake();

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;

            allowedDirections = TransformDirections.Rotate;
            latchableDirections = TransformLatchableDirections.Rotate;
        }

        protected override ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction)
        {
            bool clockwise = (direction & TransformDirections.Right) != 0;

            int groupCount = groups.Length;

            // Copy inactive emitters unchanged
            for (int i = 0; i < groupCount; i++)
            {
                TransformEmitters emitterFlag = (TransformEmitters)(1 << i);

                if ((activeEmitters & emitterFlag) == 0)
                    transformedGroups[i] = groups[i];
            }

            // Transform active emitters
            for (int i = 0; i < groupCount; i++)
            {
                TransformEmitters emitterFlag = (TransformEmitters)(1 << i);

                if ((activeEmitters & emitterFlag) == 0)
                    continue;

                transformedGroups[i] = 0;

                ulong mask = groups[i];

                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        int index = (x * gridHeight) + y;
                        ulong bit = 1UL << index;

                        // Skip unset bits
                        if ((mask & bit) == 0)
                            continue;

                        int newX = clockwise
                            ? (gridHeight - 1) - y
                            : y;

                        int newY = clockwise
                            ? x
                            : (gridWidth - 1) - x;

                        int newIndex = (newX * gridHeight) + newY;
                        ulong newBit = 1UL << newIndex;

                        // Skip if inactive emitter occupies destination
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