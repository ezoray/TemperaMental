using TemperaMental.Core;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Emitters
{
    public class EmitterTransformManager : MonoBehaviour
    {
        [SerializeField] RandomTransformService randomTransformService;

        EmitterTransformMode transformMode;
        TransformEmitterFlags activeEmitters;

        [SerializeField] UnityEvent<EmitterTransformMode> onTransformModeChanged;
        [SerializeField] UnityEvent<int, bool> onTransformEmitterChanged;
        [SerializeField] UnityEvent<int> onRandomEmitterCountChanged;
        [SerializeField] UnityEvent<ulong[]> onEmittersRandomised;

        private void OnEnable()
        {
            transformMode = EmitterTransformMode.Shift;
            activeEmitters = TransformEmitterFlags.All;
        }

        public void RandomiseEmitters(ulong[] emitterGroup, int targetCount)
        {
            randomTransformService.Randomise(emitterGroup, targetCount, activeEmitters);

            int emitterCount = EmitterUtils.GetEmitterCount(emitterGroup);

            onRandomEmitterCountChanged?.Invoke(emitterCount);
            onEmittersRandomised?.Invoke(emitterGroup);
        }

        public void ToggleEmitter(int emitterId)
        {
            TransformEmitterFlags emitter = (TransformEmitterFlags)(1 << emitterId);

            activeEmitters ^= (TransformEmitterFlags)(1 << emitterId);

            onTransformEmitterChanged?.Invoke(emitterId, activeEmitters.HasFlag(emitter));
        }

        public void SetTransformMode(EmitterTransformMode transformMode)
        {
            if (transformMode != this.transformMode)
            {
                this.transformMode = transformMode;

                switch (transformMode)
                {
                    case EmitterTransformMode.Random:
                        break;
                    case EmitterTransformMode.Shift:
                        break;
                    default:
                        break;
                }

                onTransformModeChanged?.Invoke(transformMode);
            }
        }
    }
}
