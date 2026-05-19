using System;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Transforms
{
    public abstract class TransformBaseService : MonoBehaviour
    {
        protected TransformDirections allowedDirections;
        protected TransformLatchableDirections latchableDirections;
        protected TransformDirections currentDirections;

        private bool isLatched;

        protected TransformEmitters activeEmitters;

        protected ulong[] transformedGroups;

        public event Action<TransformDirections, bool> OnDirectionLatchStateChanged;

        public event Action<ulong[]> OnEmittersTransformed;

        protected abstract ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction);

        protected virtual void Awake()
        {
            activeEmitters = TransformEmitters.All;

            transformedGroups = new ulong[ConfigRegistry.Grid.EmitterCount];
        }

        public virtual ulong[] DoTransform(ulong[] groups)
        {
            ulong[] result = groups;

            for (int i = 0; i < 4; i++)
            {
                TransformDirections direction =
                    (TransformDirections)(1 << i);

                if ((currentDirections & direction) != 0)
                {
                    result = DoSingleTransform(result, direction);
                }
            }

            return result;
        }

        public TransformDetail GetTransformDetail()
        {
            return new TransformDetail(
                activeEmitters,
                isLatched,
                allowedDirections,
                latchableDirections,
                currentDirections);
        }

        public void HandleDirectionChange(ulong[] emitterGroup, int directionValue)
        {
            TransformDirections direction = (TransformDirections)(1 << directionValue);

            if ((allowedDirections & direction) == 0)
                return;

            TransformLatchableDirections latchableDirection = (TransformLatchableDirections)(1 << directionValue);

            bool canLatch = (latchableDirections & latchableDirection) != 0;

            // Non-latched or non-latchable direction:
            // perform immediate transform
            if (!isLatched || !canLatch)
            {
                ulong[] result = DoSingleTransform(emitterGroup, direction);

                OnEmittersTransformed?.Invoke(result);

                return;
            }

            // Toggle off existing latched direction
            if ((currentDirections & direction) != 0)
            {
                currentDirections &= ~direction;

                OnDirectionLatchStateChanged?.Invoke(direction, false);

                return;
            }

            // Remove opposing direction
            TransformDirections opposing = (TransformDirections)(1 << (directionValue ^ 1));

            if ((currentDirections & opposing) != 0)
            {
                currentDirections &= ~opposing;

                OnDirectionLatchStateChanged?.Invoke(opposing, false);
            }

            // Enable new direction
            currentDirections |= direction;

            OnDirectionLatchStateChanged?.Invoke(direction, true);
        }

        public bool ToggleEmitter(int emitterId)
        {
            TransformEmitters emitter = (TransformEmitters)(1 << emitterId);

            activeEmitters ^= emitter;

            return (activeEmitters & emitter) != 0;
        }

        public bool ToggleLatch()
        {
            isLatched = !isLatched;

            if (!isLatched)
                currentDirections = TransformDirections.None;

            return isLatched;
        }

        public virtual void Reset()
        {
            ClearLatch();
            activeEmitters = TransformEmitters.All;
        }

        public void ClearLatch()
        {
            isLatched = false;
            currentDirections = TransformDirections.None;
        }

        public bool IsLatched { get => isLatched; }
        public TransformDirections AllowedDirections { get => allowedDirections; }
    }
}