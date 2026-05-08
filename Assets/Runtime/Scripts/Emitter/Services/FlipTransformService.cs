using TemperaMental.Applications.Config;
using TemperaMental.Core;

namespace TemperaMental.Emitters
{
    public class FlipTransformService : TransformBaseService
    {
        int gridWidth, gridHeight;

        protected override void OnEnable()
        {
            base.OnEnable();

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;

            allowedDirections = ConfigRegistry.Emitter.FlipTransformDirections;
        }

        public override ulong[] DoTransform(ulong[] groups)
        {
            if (currentDirections == TransformDirections.None) return groups;
            return DoSingleTransform(groups, currentDirections);
        }

        protected override ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction)
        {
            bool hasHorizontal = direction.HasFlag(TransformDirections.Left) || direction.HasFlag(TransformDirections.Right);
            bool hasVertical = direction.HasFlag(TransformDirections.Up) || direction.HasFlag(TransformDirections.Down);

            ulong[] transformedGroup = new ulong[groups.Length];

            for (int i = 0; i < groups.Length; i++)
            {
                if (!activeEmitters.HasFlag((TransformEmitters)(1 << i)))
                {
                    transformedGroup[i] = groups[i];
                    continue;
                }

                ulong mask = groups[i];

                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        int index = x * gridHeight + y;
                        if ((mask & (1UL << index)) == 0) continue;

                        int newX = hasHorizontal ? (gridWidth - 1) - x : x;
                        int newY = hasVertical ? (gridHeight - 1) - y : y;
                        int newIndex = newX * gridHeight + newY;

                        bool blocked = false;
                        for (int j = 0; j < groups.Length; j++)
                        {
                            if (activeEmitters.HasFlag((TransformEmitters)(1 << j))) continue;
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
