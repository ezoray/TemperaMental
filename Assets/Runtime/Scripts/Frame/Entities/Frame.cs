using System;
using TemperaMental.Applications.Config;
using TemperaMental.Utils;
using UnityEngine;

namespace TemperaMental.Frames
{
    public class Frame
    {
        // slightly dangerous to assume ConfigRegistry is initialised here
        static readonly int EmitterCount = ConfigRegistry.Grid.EmitterCount;

        readonly ulong[] emitterGroups;

        public Frame()
        {
            emitterGroups = new ulong[EmitterCount];
        }

        public Frame(Frame otherFrame)
        {
            emitterGroups = new ulong[EmitterCount];
            Array.Copy(otherFrame.emitterGroups, emitterGroups, EmitterCount);
        }

        public Frame(ulong[] emitterGroups)
        {
            this.emitterGroups = (ulong[])emitterGroups.Clone();
        }
        public void SetEmitterGroups(ulong[] groups)
        {
            Array.Copy(groups, emitterGroups, emitterGroups.Length);
        }

        // return reference to emitter array for direct access to up-to-date values
        public ulong[] GetEmitterGroups()
        {
            return emitterGroups;
        }

        public void AddEmitter(Vector2Int position, int emitterId)
        {
            int pos = EmitterUtils.PositionToIndex(position);

            // evict any other emitter at this position
            for (int i = 0; i < EmitterCount; i++)
            {
                if (i == emitterId) continue;
                emitterGroups[i] &= ~(1UL << pos);
            }

            emitterGroups[emitterId] |= 1UL << pos;
        }

        public bool TryRemoveEmitter(Vector2Int position, out int emitterId)
        {
            emitterId = -1;
            int index = EmitterUtils.PositionToIndex(position);
            for (int i = 0; i < EmitterCount; i++)
            {
                if ((emitterGroups[i] & (1UL << index)) != 0)
                {
                    emitterGroups[i] &= ~(1UL << index);
                    emitterId = i;
                    return true;
                }
            }
            return false;
        }

        public bool CheckSameEmitterAtPosition(Vector2Int position, int currentEmitterId)
        {
            int pos = EmitterUtils.PositionToIndex(position);
            return (emitterGroups[currentEmitterId] & (1UL << pos)) != 0;
        }

        public void ClearEmitters()
        {
            for (int i = 0; i < EmitterCount; i++)
            {
                emitterGroups[i] = 0;
            }
        }
    }
}