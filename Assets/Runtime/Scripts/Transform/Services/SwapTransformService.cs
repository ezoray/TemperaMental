using System;
using System.Collections.Generic;
using TemperaMental.Core;

namespace TemperaMental.Transforms
{
    public class SwapTransformService : TransformBaseService
    {

        protected override void Awake()
        {
            base.Awake();

            allowedDirections = TransformDirections.Swap;
            latchableDirections = TransformLatchableDirections.Swap;
        }

        protected override ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction)
        {
            ulong[] transformedGroups = new ulong[groups.Length];
            Array.Copy(groups, transformedGroups, groups.Length);

            if (direction.HasFlag(TransformDirections.Left) || direction.HasFlag(TransformDirections.Right))
            {
                List<int> activeIds = new List<int>();
                for (int i = 0; i < groups.Length; i++)
                    if (activeEmitters.HasFlag((TransformEmitters)(1 << i)))
                        activeIds.Add(i);

                if (activeIds.Count > 1)
                {
                    if (direction.HasFlag(TransformDirections.Left))
                    {
                        ulong last = transformedGroups[activeIds[activeIds.Count - 1]];
                        for (int i = activeIds.Count - 1; i > 0; i--)
                            transformedGroups[activeIds[i]] = transformedGroups[activeIds[i - 1]];
                        transformedGroups[activeIds[0]] = last;
                    }
                    else
                    {
                        ulong first = transformedGroups[activeIds[0]];
                        for (int i = 0; i < activeIds.Count - 1; i++)
                            transformedGroups[activeIds[i]] = transformedGroups[activeIds[i + 1]];
                        transformedGroups[activeIds[activeIds.Count - 1]] = first;
                    }
                }
            }
            else
            {
                (int, int) pair = direction.HasFlag(TransformDirections.Up) ? (0, 3) : (1, 2);
                int a = pair.Item1;
                int b = pair.Item2;
                bool aActive = activeEmitters.HasFlag((TransformEmitters)(1 << a));
                bool bActive = activeEmitters.HasFlag((TransformEmitters)(1 << b));
                if (aActive && bActive)
                    (transformedGroups[a], transformedGroups[b]) = (transformedGroups[b], transformedGroups[a]);
            }

            return transformedGroups;
        }
    }
}
