using TemperaMental.Applications.Config;
using TemperaMental.Core;

namespace TemperaMental.Emitters
{
    public class FlipTransformService : TransformBaseService
    {
        int gridWidth, gridHeight;

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

        private ulong[] DoSingleTransform(ulong[] groups, TransformEmitterFlags activeEmitters, TransformDirectionFlags direction)
        {        
            ulong[] transformedGroup = new ulong[groups.Length];

            bool horizontal = direction.HasFlag(TransformDirectionFlags.Left) || direction.HasFlag(TransformDirectionFlags.Right);

            // copy inactive emitters unchanged
            for (int i = 0; i < groups.Length; i++)
            {
                if (!activeEmitters.HasFlag((TransformEmitterFlags)(1 << i)))
                    transformedGroup[i] = groups[i];
            }

            // transform active emitters
            for (int i = 0; i < groups.Length; i++)
            {
                if (!activeEmitters.HasFlag((TransformEmitterFlags)(1 << i))) continue;

                ulong mask = groups[i];

                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        int index = x * gridHeight + y;
                        if ((mask & (1UL << index)) == 0) continue;

                        int newX = horizontal ? (gridWidth - 1) - x : x;
                        int newY = horizontal ? y : (gridHeight - 1) - y;
                        int newIndex = newX * gridHeight + newY;

                        // skip if inactive emitter occupies destination in original
                        bool blocked = false;
                        for (int j = 0; j < groups.Length; j++)
                        {
                            if (activeEmitters.HasFlag((TransformEmitterFlags)(1 << j))) continue;
                            if ((groups[j] & (1UL << newIndex)) != 0) { blocked = true; break; }
                        }

                        if (blocked) continue;

                        transformedGroup[i] |= 1UL << newIndex;
                    }
                }
            }

            return transformedGroup;
        }
    }
}
