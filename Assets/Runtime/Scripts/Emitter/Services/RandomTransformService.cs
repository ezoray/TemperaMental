//using System;
using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Emitters
{
    public class RandomTransformService : MonoBehaviour
    {
        int gridWidth, gridHeight;

        private void OnEnable()
        {
            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;
        }

        public void Randomise(ulong[] groups, int targetCount, TransformEmitterFlags activeEmitters)
        {
            // collect active emitter ids
            List<int> activeIds = new List<int>();

            for (int i = 0; i < groups.Length; i++)
            {
                if (activeEmitters.HasFlag((TransformEmitterFlags)(1 << i))) activeIds.Add(i);
            }

            // collect all currently occupied positions across active emitters
            List<int> occupiedPositions = new List<int>();
            for (int i = 0; i < groups.Length; i++)
            {
                if (!activeEmitters.HasFlag((TransformEmitterFlags)(1 << i))) continue;
                ulong mask = groups[i];
                for (int bit = 0; bit < gridWidth * gridHeight; bit++)
                {
                    if ((mask & (1UL << bit)) != 0) occupiedPositions.Add(bit);
                }
            }

            int currentCount = occupiedPositions.Count;

            if (targetCount == currentCount) return;
            if (targetCount > currentCount && activeIds.Count == 0) return;
            if (targetCount < currentCount && occupiedPositions.Count == 0) return;

            if (targetCount > currentCount)
            {
                // collect all empty positions
                ulong allOccupied = 0;
                for (int i = 0; i < groups.Length; i++)
                {
                    allOccupied |= groups[i];
                }

                List<int> emptyPositions = new List<int>();
                for (int bit = 0; bit < gridWidth * gridHeight; bit++)
                {
                    if ((allOccupied & (1UL << bit)) == 0) emptyPositions.Add(bit);
                }

                // place randomly into empty positions using random active emitter
                int toAdd = Mathf.Min(targetCount - currentCount, emptyPositions.Count);

                for (int i = 0; i < toAdd; i++)
                {
                    int randomIndex = Random.Range(i, emptyPositions.Count);
                    (emptyPositions[i], emptyPositions[randomIndex]) = (emptyPositions[randomIndex], emptyPositions[i]);

                    int pos = emptyPositions[i];
                    int emitterId = activeIds[Random.Range(0, activeIds.Count)];
                    groups[emitterId] |= 1UL << pos;
                }
            }
            else if (targetCount < currentCount)
            {
                // shuffle occupied positions and remove the excess
                int toRemove = currentCount - targetCount;
                for (int i = 0; i < toRemove; i++)
                {
                    int randomIndex = Random.Range(i, occupiedPositions.Count);
                    (occupiedPositions[i], occupiedPositions[randomIndex]) = (occupiedPositions[randomIndex], occupiedPositions[i]);

                    int pos = occupiedPositions[i];

                    for (int j = 0; j < groups.Length; j++)
                    {
                        groups[j] &= ~(1UL << pos);
                    }
                }
            }
        }
    }
}
