using System;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Emitters
{
    public abstract class TransformBaseService : MonoBehaviour
    {
        protected TransformDirections allowedDirections;
        protected TransformDirections currentDirections;
        protected bool isLatched;
        protected TransformEmitters activeEmitters;

        public event Action<TransformDirections, bool> OnDirectionLatchStateChanged;
        public event Action<ulong[]> OnEmittersTransformed;

        protected abstract ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction);


        protected virtual void Awake()
        {
            activeEmitters = TransformEmitters.All;    
        }

        public virtual ulong[] DoTransform(ulong[] groups)
        {
            ulong[] transformedGroups = groups;

            for (int i = 0; i < 4; i++)
            {
                TransformDirections direction = (TransformDirections)(1 << i);

                if (currentDirections.HasFlag(direction))
                {
                    transformedGroups = DoSingleTransform(transformedGroups, direction);
                }
            }

            return transformedGroups;
        }

        public EmitterTransformDetail GetTransformDetail()
        {
            return new EmitterTransformDetail(activeEmitters, isLatched, currentDirections);
        }

        public void HandleDirectionChange(ulong[] emitterGroup, int directionValue)
        {
            TransformDirections direction = (TransformDirections)(1 << directionValue);

            if (!allowedDirections.HasFlag(direction)) return;

            if (!isLatched)
            {
                ulong[] transformedGroups = DoSingleTransform(emitterGroup, direction);
                OnEmittersTransformed?.Invoke(transformedGroups);
                return;
            }

            if (currentDirections.HasFlag(direction))
            {
                currentDirections &= ~direction;
                OnDirectionLatchStateChanged?.Invoke(direction, false);
                return;
            }

            TransformDirections opposing = (TransformDirections)(1 << (directionValue ^ 1));
            currentDirections &= ~opposing;
            OnDirectionLatchStateChanged?.Invoke(opposing, false);

            currentDirections |= direction;
            OnDirectionLatchStateChanged?.Invoke(direction, true);
        }

        public bool ToggleEmitter(int emitterId)
        {
            TransformEmitters emitter = (TransformEmitters)(1 << emitterId);

            activeEmitters ^= emitter;

            return activeEmitters.HasFlag(emitter);
        }

        public bool ToggleLatch()
        {
            isLatched = !isLatched;

            if (!isLatched) currentDirections = TransformDirections.None;

            return isLatched;
        }

        public void ClearLatch()
        {
            isLatched = false;
            currentDirections = 0;
        }

        public bool IsLatched { get => isLatched; set => isLatched = value; }
        public TransformDirections CurrentDirections => currentDirections;
        public TransformDirections AllowedDirections { get => allowedDirections; set => allowedDirections = value; }
    }
}