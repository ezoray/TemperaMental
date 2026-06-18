using TemperaMental.Applications.Config;
using TemperaMental.Core;

namespace TemperaMental.Transforms
{
    public class RotateTransformService : TransformBaseService
    {
        int gridWidth;
        int gridHeight;

        protected ulong[] intermediateGroups;

        protected override void Awake()
        {
            base.Awake();

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;

            allowedDirections = TransformDirections.Rotate;
            latchableDirections = TransformLatchableDirections.Rotate;

            intermediateGroups = new ulong[ConfigRegistry.Grid.EmitterCount];
        }

        public override ulong[] DoTransform(ulong[] groups)
        {
            if (TransformMode == TransformMode.Simple)
            {
                TransformDirections directions = GetDirections();

                if (directions == TransformDirections.None)
                    return groups;

                return DoSingleTransform(groups, directions);
            }
            else
            {
                // copy input into intermediate buffer so each emitter's result
                // feeds into the next pass without aliasing transformedGroups
                System.Array.Copy(groups, intermediateGroups, groups.Length);

                for (int i = 0; i < 4; i++)
                {
                    if (!ShouldEmitterFire(i))
                        continue;

                    TransformDirections direction = GetEmitterDirections(i);

                    if (direction == TransformDirections.None)
                        continue;

                    DoSingleTransformForEmitter(intermediateGroups, direction, i);
                }

                return intermediateGroups;
            }
        }

        // simple mode — rotates all active emitters with the same direction
        protected override ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction)
        {
            bool clockwise = (direction & TransformDirections.Right) != 0;

            int groupCount = groups.Length;

            // copy inactive emitters unchanged
            for (int i = 0; i < groupCount; i++)
            {
                TransformActiveEmitters emitterFlag = (TransformActiveEmitters)(1 << i);

                if ((ActiveEmitters & emitterFlag) == 0)
                    transformedGroups[i] = groups[i];
            }

            // transform active emitters
            for (int i = 0; i < groupCount; i++)
            {
                TransformActiveEmitters emitterFlag = (TransformActiveEmitters)(1 << i);

                if ((ActiveEmitters & emitterFlag) == 0)
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

                        int newX = clockwise ? (gridHeight - 1) - y : y;
                        int newY = clockwise ? x : (gridWidth - 1) - x;

                        int newIndex = (newX * gridHeight) + newY;
                        ulong newBit = 1UL << newIndex;

                        // skip if inactive emitter occupies destination
                        bool blocked = false;

                        for (int j = 0; j < groupCount; j++)
                        {
                            TransformActiveEmitters otherEmitterFlag = (TransformActiveEmitters)(1 << j);

                            // ignore active emitters
                            if ((ActiveEmitters & otherEmitterFlag) != 0)
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

        // individual mode — rotates only the specified emitter in place within intermediateGroups
        private void DoSingleTransformForEmitter(ulong[] groups, TransformDirections direction, int emitterId)
        {
            bool clockwise = (direction & TransformDirections.Right) != 0;

            int groupCount = groups.Length;

            ulong mask = groups[emitterId];
            groups[emitterId] = 0;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    int index = (x * gridHeight) + y;
                    ulong bit = 1UL << index;

                    // skip unset bits
                    if ((mask & bit) == 0)
                        continue;

                    int newX = clockwise ? (gridHeight - 1) - y : y;
                    int newY = clockwise ? x : (gridWidth - 1) - x;

                    int newIndex = (newX * gridHeight) + newY;
                    ulong newBit = 1UL << newIndex;

                    // skip if any other emitter occupies destination
                    bool blocked = false;

                    for (int j = 0; j < groupCount; j++)
                    {
                        if (j == emitterId)
                            continue;

                        if ((groups[j] & newBit) != 0)
                        {
                            blocked = true;
                            break;
                        }
                    }

                    if (!blocked)
                        groups[emitterId] |= newBit;
                }
            }
        }
    }
}