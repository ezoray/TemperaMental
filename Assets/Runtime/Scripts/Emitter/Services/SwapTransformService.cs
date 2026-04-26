using System;
using System.Collections.Generic;
using TemperaMental.Core;

namespace TemperaMental.Emitters
{
    public class SwapTransformService : TransformBaseService
    {
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
            ulong[] transformedGroups = new ulong[groups.Length];
            Array.Copy(groups, transformedGroups, groups.Length);

            if (direction.HasFlag(TransformDirectionFlags.Left) || direction.HasFlag(TransformDirectionFlags.Right))
            {
                List<int> activeIds = new List<int>();
                for (int i = 0; i < groups.Length; i++)
                    if (activeEmitters.HasFlag((TransformEmitterFlags)(1 << i)))
                        activeIds.Add(i);

                if (activeIds.Count > 1)
                {
                    if (direction.HasFlag(TransformDirectionFlags.Left))
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
                (int, int)[] upPairs = { (0, 1), (2, 3) };
                (int, int)[] downPairs = { (0, 2), (1, 3) };
                (int, int)[] pairs = direction.HasFlag(TransformDirectionFlags.Up) ? upPairs : downPairs;

                foreach (var (a, b) in pairs)
                {
                    bool aActive = activeEmitters.HasFlag((TransformEmitterFlags)(1 << a));
                    bool bActive = activeEmitters.HasFlag((TransformEmitterFlags)(1 << b));
                    if (!aActive || !bActive) continue;

                    (transformedGroups[a], transformedGroups[b]) = (transformedGroups[b], transformedGroups[a]);
                }
            }

            return transformedGroups;
        }
    }
}
