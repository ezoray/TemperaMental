using System;
using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;

namespace TemperaMental.Transforms
{
    public class SwapTransformService : TransformBaseService
    {
        List<int> activeEmitterIds;

        protected override void Awake()
        {
            base.Awake();

            activeEmitterIds = new List<int>(ConfigRegistry.Grid.EmitterCount);

            allowedDirections = TransformDirections.Swap;
            latchableDirections = TransformLatchableDirections.Swap;
        }

        public override ulong[] DoTransform(ulong[] groups)
        {
            // swap has no meaningful individual mode — always operates in simple mode
            TransformDirections directions = GetDirections();

            if (directions == TransformDirections.None)
                return groups;

            return DoSingleTransform(groups, directions);
        }

        protected override ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction)
        {
            Array.Copy(groups, transformedGroups, groups.Length);

            bool hasLeft = (direction & TransformDirections.Left) != 0;
            bool hasRight = (direction & TransformDirections.Right) != 0;

            if (hasLeft || hasRight)
            {
                activeEmitterIds.Clear();

                for (int i = 0; i < groups.Length; i++)
                {
                    TransformActiveEmitters emitterFlag = (TransformActiveEmitters)(1 << i);

                    if ((ActiveEmitters & emitterFlag) != 0)
                        activeEmitterIds.Add(i);
                }

                int activeCount = activeEmitterIds.Count;

                if (activeCount > 1)
                {
                    // rotate left
                    if (hasLeft)
                    {
                        int lastIndex = activeCount - 1;
                        ulong last = transformedGroups[activeEmitterIds[lastIndex]];

                        for (int i = lastIndex; i > 0; i--)
                        {
                            transformedGroups[activeEmitterIds[i]] =
                                transformedGroups[activeEmitterIds[i - 1]];
                        }

                        transformedGroups[activeEmitterIds[0]] = last;
                    }
                    // rotate right
                    else
                    {
                        int lastIndex = activeCount - 1;
                        ulong first = transformedGroups[activeEmitterIds[0]];

                        for (int i = 0; i < lastIndex; i++)
                        {
                            transformedGroups[activeEmitterIds[i]] =
                                transformedGroups[activeEmitterIds[i + 1]];
                        }

                        transformedGroups[activeEmitterIds[lastIndex]] = first;
                    }
                }
            }
            else
            {
                bool isUp = (direction & TransformDirections.Up) != 0;

                int a = isUp ? 0 : 1;
                int b = isUp ? 3 : 2;

                TransformActiveEmitters emitterFlagA = (TransformActiveEmitters)(1 << a);
                TransformActiveEmitters emitterFlagB = (TransformActiveEmitters)(1 << b);

                bool aActive = (ActiveEmitters & emitterFlagA) != 0;
                bool bActive = (ActiveEmitters & emitterFlagB) != 0;

                if (aActive && bActive)
                {
                    (transformedGroups[a], transformedGroups[b]) =
                        (transformedGroups[b], transformedGroups[a]);
                }
            }

            return transformedGroups;
        }
    }
}