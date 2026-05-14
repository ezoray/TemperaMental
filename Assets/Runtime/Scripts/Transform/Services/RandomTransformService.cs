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

        int targetOffset;

        List<int> activeEmitterIds;
        List<int> occupiedPositions;
        List<int> emptyPositions;

        int totalBits;

        protected override void Awake()
        {
            base.Awake();

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;
            maxEmitters = ConfigRegistry.Grid.MaxEmitters;

            totalBits = gridWidth * gridHeight;

            targetOffset = 1;

            activeEmitterIds = new List<int>(4);
            occupiedPositions = new List<int>(64);
            emptyPositions = new List<int>(64);

            allowedDirections = TransformDirections.Random;
            latchableDirections = TransformLatchableDirections.Random;
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

            int currentCount = EmitterUtils.GetEmitterCount(groups);

            int targetCount =
                (direction & TransformDirections.Right) != 0
                    ? Mathf.Min(currentCount + targetOffset, maxEmitters)
                    : Mathf.Max(currentCount - targetOffset, 0);

            return DoRandomTransform(groups, targetCount);
        }

        public void AdjustTargetOffset(int change)
        {
            targetOffset += change;
            targetOffset = Mathf.Clamp(targetOffset, 1, maxEmitters);

            LogMan.LogTemp("Random emitters +" + targetOffset);
        }

        public ulong[] DoRandomTransform(ulong[] groups, int targetCount)
        {
            System.Array.Copy(groups, transformedGroups, groups.Length);

            int groupCount = transformedGroups.Length;

            // Collect active emitter ids
            activeEmitterIds.Clear();

            for (int i = 0; i < groupCount; i++)
            {
                TransformEmitters emitterFlag = (TransformEmitters)(1 << i);

                if ((activeEmitters & emitterFlag) != 0)
                    activeEmitterIds.Add(i);
            }

            // Total count across all emitters
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

            // Collect occupied positions from active emitters only
            occupiedPositions.Clear();

            for (int i = 0; i < groupCount; i++)
            {
                TransformEmitters emitterFlag = (TransformEmitters)(1 << i);

                if ((activeEmitters & emitterFlag) == 0)
                    continue;

                ulong mask = transformedGroups[i];

                for (int bit = 0; bit < totalBits; bit++)
                {
                    if ((mask & (1UL << bit)) != 0)
                        occupiedPositions.Add(bit);
                }
            }

            if (targetCount == currentCount)
                return transformedGroups;

            if (targetCount > currentCount && activeEmitterIds.Count == 0)
                return transformedGroups;

            if (targetCount < currentCount && occupiedPositions.Count == 0)
                return transformedGroups;

            // Add emitters
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

                    int emitterId =
                        activeEmitterIds[
                            Random.Range(0, activeEmitterIds.Count)
                        ];

                    transformedGroups[emitterId] |= 1UL << pos;
                }
            }
            // Remove emitters
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

                    for (int j = 0; j < groupCount; j++)
                    {
                        TransformEmitters emitterFlag =
                            (TransformEmitters)(1 << j);

                        if ((activeEmitters & emitterFlag) == 0)
                            continue;

                        transformedGroups[j] &= clearBitMask;
                    }
                }
            }

            return transformedGroups;
        }
    }
}