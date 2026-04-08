using System;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Utils;
using UnityEngine;

namespace TemperaMental.Frames
{
    public class Frame
    {
        readonly int width;
        readonly int height;
        readonly int emitterCount;

        // one ulong per emitter, bits map to Tempera grid positions
        readonly ulong[] emitterGroups;

        public Frame(int width, int height)
        {
            this.width = width;
            this.height = height;

            emitterCount = ConfigRegistry.Grid.EmitterCount;
            emitterGroups = new ulong[emitterCount];
        }

        public Frame(Frame otherFrame)
        {
            width = otherFrame.width;
            height = otherFrame.height;
            emitterCount = otherFrame.emitterCount;

            emitterGroups = new ulong[emitterCount];
            Array.Copy(otherFrame.emitterGroups, emitterGroups, emitterGroups.Length);
        }

        // returns the ulong bitmask array directly so callers can iterate by emitter without conversion
        public ulong[] GetEmitterGroups()
        {
            return emitterGroups;
        }

        public void AddEmitter(EmitterDetail emitterDetail)
        {
            int pos = EmitterUtils.PositionToIndex(emitterDetail.Position);

            // evict any other emitter at this position
            for (int i = 0; i < emitterCount; i++)
            {
                if (i == emitterDetail.EmitterId) continue;
                emitterGroups[i] &= ~(1UL << pos);
            }

            emitterGroups[emitterDetail.EmitterId] |= 1UL << pos;
        }

        public bool TryRemoveEmitter(Vector2Int position)
        {
            int pos = EmitterUtils.PositionToIndex(position);

            for (int emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                if ((emitterGroups[emitterId] & (1UL << pos)) != 0)
                {
                    emitterGroups[emitterId] &= ~(1UL << pos);
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
            for (int i = 0; i < emitterCount; i++)
            {
                emitterGroups[i] = 0;
            }
        }
    }
}