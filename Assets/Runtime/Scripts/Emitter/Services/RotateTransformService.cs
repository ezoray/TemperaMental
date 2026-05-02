using TemperaMental.Applications.Config;
using TemperaMental.Core;

namespace TemperaMental.Emitters
{
    public class RotateTransformService : TransformBaseService
    {
        int gridWidth, gridHeight;

        protected override void OnEnable()
        {
            base.OnEnable();

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;
            allowedDirections = ConfigRegistry.Emitter.RotateTransformDirections;
        }

        protected override ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction)
        {
            ulong[] transformedGroups = new ulong[groups.Length];
            bool clockwise = direction.HasFlag(TransformDirections.Right);

            // copy inactive emitters unchanged
            for (int i = 0; i < groups.Length; i++)
            {
                if (!activeEmitters.HasFlag((TransformEmitters)(1 << i)))
                    transformedGroups[i] = groups[i];
            }

            // transform active emitters
            for (int i = 0; i < groups.Length; i++)
            {
                if (!activeEmitters.HasFlag((TransformEmitters)(1 << i))) continue;

                ulong mask = groups[i];
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        int index = x * gridHeight + y;
                        if ((mask & (1UL << index)) == 0) continue;

                        int newX = clockwise ? (gridHeight - 1) - y : y;
                        int newY = clockwise ? x : (gridWidth - 1) - x;
                        int newIndex = newX * gridHeight + newY;

                        // skip if inactive emitter occupies destination in original
                        bool blocked = false;
                        for (int j = 0; j < groups.Length; j++)
                        {
                            if (activeEmitters.HasFlag((TransformEmitters)(1 << j))) continue;
                            if ((groups[j] & (1UL << newIndex)) != 0) { blocked = true; break; }
                        }

                        if (blocked) continue;
                        transformedGroups[i] |= 1UL << newIndex;
                    }
                }
            }

            return transformedGroups;
        }
    }
}