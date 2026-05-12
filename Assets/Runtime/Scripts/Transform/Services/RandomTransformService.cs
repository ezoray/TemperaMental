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
        int gridWidth, gridHeight;
        int maxEmitters;

        int targetOffset;

        protected override void Awake()
        {
            base.Awake();

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;
            maxEmitters = ConfigRegistry.Grid.MaxEmitters;

            targetOffset = 1;

            allowedDirections = TransformDirections.Random;
            latchableDirections = TransformLatchableDirections.Random;
        }

        protected override ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction)
        {
            if (direction.HasFlag(TransformDirections.Up))
            {
                AdjustTargetOffset(1);
                return groups;
            }

            if (direction.HasFlag(TransformDirections.Down))
            {
                AdjustTargetOffset(-1);
                return groups;
            }

            int currentCount = EmitterUtils.GetEmitterCount(groups);
            int targetCount = direction.HasFlag(TransformDirections.Right)
                ? Mathf.Min(currentCount + targetOffset, maxEmitters)
                : Mathf.Max(currentCount - targetOffset, 0);

            return DoRandomTransform(groups, targetCount);
        }

        public void AdjustTargetOffset(int change)
        {
            targetOffset += change;
            targetOffset = Mathf.Clamp(targetOffset, 1, maxEmitters);

            LogMan.LogTemp("Random: + " + targetOffset);
        }

        public ulong[] DoRandomTransform(ulong[] groups, int targetCount)
        {
            ulong[] transformedGroups = new ulong[groups.Length];
            System.Array.Copy(groups, transformedGroups, groups.Length);

            // collect active emitter ids
            List<int> activeIds = new List<int>();
            for (int i = 0; i < transformedGroups.Length; i++)
                if (activeEmitters.HasFlag((TransformEmitters)(1 << i))) activeIds.Add(i);

            // total count across all slots — matches slider's frame of reference
            int currentCount = 0;
            for (int i = 0; i < transformedGroups.Length; i++)
                for (int bit = 0; bit < gridWidth * gridHeight; bit++)
                    if ((transformedGroups[i] & (1UL << bit)) != 0) currentCount++;

            // occupied positions restricted to active slots — only these can be removed
            List<int> occupiedPositions = new List<int>();
            for (int i = 0; i < transformedGroups.Length; i++)
            {
                if (!activeEmitters.HasFlag((TransformEmitters)(1 << i))) continue;
                ulong mask = transformedGroups[i];
                for (int bit = 0; bit < gridWidth * gridHeight; bit++)
                    if ((mask & (1UL << bit)) != 0) occupiedPositions.Add(bit);
            }

            if (targetCount == currentCount) return transformedGroups;
            if (targetCount > currentCount && activeIds.Count == 0) return transformedGroups;
            if (targetCount < currentCount && occupiedPositions.Count == 0) return transformedGroups;

            if (targetCount > currentCount)
            {
                ulong allOccupied = 0;
                for (int i = 0; i < transformedGroups.Length; i++)
                    allOccupied |= transformedGroups[i];

                List<int> emptyPositions = new List<int>();
                for (int bit = 0; bit < gridWidth * gridHeight; bit++)
                    if ((allOccupied & (1UL << bit)) == 0) emptyPositions.Add(bit);

                int toAdd = Mathf.Min(targetCount - currentCount, emptyPositions.Count);
                for (int i = 0; i < toAdd; i++)
                {
                    int randomIndex = Random.Range(i, emptyPositions.Count);
                    (emptyPositions[i], emptyPositions[randomIndex]) = (emptyPositions[randomIndex], emptyPositions[i]);
                    int pos = emptyPositions[i];
                    int emitterId = activeIds[Random.Range(0, activeIds.Count)];
                    transformedGroups[emitterId] |= 1UL << pos;
                }
            }
            else if (targetCount < currentCount)
            {
                int toRemove = Mathf.Min(currentCount - targetCount, occupiedPositions.Count);
                for (int i = 0; i < toRemove; i++)
                {
                    int randomIndex = Random.Range(i, occupiedPositions.Count);
                    (occupiedPositions[i], occupiedPositions[randomIndex]) = (occupiedPositions[randomIndex], occupiedPositions[i]);
                    int pos = occupiedPositions[i];
                    for (int j = 0; j < transformedGroups.Length; j++)
                    {
                        if (!activeEmitters.HasFlag((TransformEmitters)(1 << j))) continue;
                        transformedGroups[j] &= ~(1UL << pos);
                    }
                }
            }

            return transformedGroups;
        }
    }
}