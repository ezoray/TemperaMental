using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using TemperaMental.Utils;
using UnityEngine;

namespace TemperaMental.Transforms
{
    public class RandomTransformService : TransformBaseService
    {
        int gridWidth;
        int gridHeight;
        int maxEmitters;
        int totalBits;

        int targetOffset;

        List<int> activeEmitterIds;
        List<int> occupiedPositions;
        List<int> emptyPositions;

        protected override void Awake()
        {
            base.Awake();

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;
            maxEmitters = ConfigRegistry.Grid.MaxEmitters;

            totalBits = gridWidth * gridHeight;

            targetOffset = 1;

            activeEmitterIds = new List<int>(ConfigRegistry.Grid.EmitterCount);
            occupiedPositions = new List<int>(ConfigRegistry.Grid.MaxEmitters);
            emptyPositions = new List<int>(ConfigRegistry.Grid.MaxEmitters);

            allowedDirections = TransformDirections.Random;
            latchableDirections = TransformLatchableDirections.Random;
        }

        public override void ResetTransform()
        {
            base.ResetTransform();
            targetOffset = 1;
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
                ulong[] result = groups;

                for (int i = 0; i < 4; i++)
                {
                    if (!ShouldEmitterFire(i))
                        continue;

                    TransformDirections direction = GetEmitterDirections(i);

                    if (direction == TransformDirections.None)
                        continue;

                    result = DoRandomTransform(result, direction, i);
                }

                return result;
            }
        }

        protected override ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction)
        {
            if ((direction & TransformDirections.Up) != 0)
            {
                AdjustTargetOffset(1);
                return groups;
            }

            if ((direction & TransformDirections.Down) != 0)
            {
                AdjustTargetOffset(-1);
                return groups;
            }

            // simple mode only — build active emitter ids from active emitters
            activeEmitterIds.Clear();

            for (int i = 0; i < transformedGroups.Length; i++)
            {
                TransformActiveEmitters emitterFlag = (TransformActiveEmitters)(1 << i);

                if ((ActiveEmitters & emitterFlag) != 0)
                    activeEmitterIds.Add(i);
            }

            int count = EmitterUtils.GetEmitterCount(groups);

            int target =
                (direction & TransformDirections.Right) != 0
                    ? Mathf.Min(count + targetOffset, maxEmitters)
                    : Mathf.Max(count - targetOffset, 0);

            return DoRandomTransform(groups, target, activeEmitterIds);
        }

        public void AdjustTargetOffset(int change)
        {
            targetOffset += change;
            targetOffset = Mathf.Clamp(targetOffset, 1, maxEmitters);

            LogMan.LogTemp("Random emitters +" + targetOffset);
        }

        private ulong[] DoRandomTransform(ulong[] groups, TransformDirections direction, int emitterId)
        {
            activeEmitterIds.Clear();
            activeEmitterIds.Add(emitterId);

            int currentCount = EmitterUtils.GetEmitterCount(groups);

            int targetCount =
                (direction & TransformDirections.Right) != 0
                    ? Mathf.Min(currentCount + targetOffset, maxEmitters)
                    : Mathf.Max(currentCount - targetOffset, 0);

            return DoRandomTransform(groups, targetCount, activeEmitterIds);
        }

        public ulong[] DoRandomTransform(ulong[] groups, int targetCount, TransformMode mode)
        {
            activeEmitterIds.Clear();

            if (mode == TransformMode.Individual)
            {
                // scope to selected emitter only
                activeEmitterIds.Add(IndividualEmitter);
            }
            else
            {
                // build from active emitters
                for (int i = 0; i < groups.Length; i++)
                {
                    TransformActiveEmitters emitterFlag = (TransformActiveEmitters)(1 << i);

                    if ((ActiveEmitters & emitterFlag) != 0)
                        activeEmitterIds.Add(i);
                }
            }

            return DoRandomTransform(groups, targetCount, activeEmitterIds);
        }

        private ulong[] DoRandomTransform(ulong[] groups, int targetCount, List<int> emitterIds)
        {
            System.Array.Copy(groups, transformedGroups, groups.Length);

            int groupCount = transformedGroups.Length;

            // total count across all emitters
            int currentCount = 0;

            for (int i = 0; i < groupCount; i++)
            {
                ulong mask = transformedGroups[i];

                for (int bit = 0; bit < totalBits; bit++)
                {
                    if ((mask & (1UL << bit)) != 0)
                        currentCount++;
                }
            }

            // collect occupied positions from the specified emitters only
            occupiedPositions.Clear();

            foreach (int id in emitterIds)
            {
                ulong mask = transformedGroups[id];

                for (int bit = 0; bit < totalBits; bit++)
                {
                    if ((mask & (1UL << bit)) != 0)
                        occupiedPositions.Add(bit);
                }
            }

            if (targetCount == currentCount)
                return transformedGroups;

            if (targetCount > currentCount && emitterIds.Count == 0)
                return transformedGroups;

            if (targetCount < currentCount && occupiedPositions.Count == 0)
                return transformedGroups;

            // add emitters
            if (targetCount > currentCount)
            {
                ulong allOccupied = 0;

                for (int i = 0; i < groupCount; i++)
                    allOccupied |= transformedGroups[i];

                emptyPositions.Clear();

                for (int bit = 0; bit < totalBits; bit++)
                {
                    if ((allOccupied & (1UL << bit)) == 0)
                        emptyPositions.Add(bit);
                }

                int toAdd = Mathf.Min(
                    targetCount - currentCount,
                    emptyPositions.Count);

                for (int i = 0; i < toAdd; i++)
                {
                    int randomIndex = Random.Range(i, emptyPositions.Count);

                    (emptyPositions[i], emptyPositions[randomIndex]) =
                        (emptyPositions[randomIndex], emptyPositions[i]);

                    int pos = emptyPositions[i];

                    int emitterId = emitterIds[Random.Range(0, emitterIds.Count)];

                    transformedGroups[emitterId] |= 1UL << pos;
                }
            }
            // remove emitters
            else if (targetCount < currentCount)
            {
                int toRemove = Mathf.Min(
                    currentCount - targetCount,
                    occupiedPositions.Count);

                for (int i = 0; i < toRemove; i++)
                {
                    int randomIndex = Random.Range(i, occupiedPositions.Count);

                    (occupiedPositions[i], occupiedPositions[randomIndex]) =
                        (occupiedPositions[randomIndex], occupiedPositions[i]);

                    int pos = occupiedPositions[i];
                    ulong clearBitMask = ~(1UL << pos);

                    foreach (int id in emitterIds)
                    {
                        transformedGroups[id] &= clearBitMask;
                    }
                }
            }

            return transformedGroups;
        }
    }
}