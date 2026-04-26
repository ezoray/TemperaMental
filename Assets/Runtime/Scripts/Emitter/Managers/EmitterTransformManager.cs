using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Frames;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Emitters
{
    public class EmitterTransformManager : MonoBehaviour
    {
        [Header("Order: Random, Flip, Rotate, Swap, Shift")]
        [SerializeField] List<TransformBaseService> transformServices;
        [SerializeField] FrameManager frameManager;
        [SerializeField] RandomTransformService randomTransformService;
        [SerializeField] ShiftTransformService shiftTransformService;

        EmitterTransformMode transformMode;
        TransformEmitterFlags activeEmitters;

        bool isLatched;
        bool doWrap;
        TransformDirectionFlags directionFlags;

        ulong[] transformedGroup;

        int bpm;
        float nextEventTime;
        float repeatRate;

        [SerializeField] UnityEvent<EmitterTransformMode> onTransformModeChanged;
        [SerializeField] UnityEvent<int, bool> onTransformEmitterChanged;
        [SerializeField] UnityEvent<ulong[]> onEmittersTransformed;

        [SerializeField] UnityEvent<bool> onLatchStateChanged;
        [SerializeField] UnityEvent<bool> onWrapStateChanged;
        [SerializeField] UnityEvent<int, bool> onDirectionLatchStateChanged;


        private void Awake()
        {
            bpm = ConfigRegistry.Midi.DefaultBpm;
            repeatRate = 60f / bpm;
            nextEventTime = Time.time + repeatRate;
        }

        private void OnEnable()
        {
            transformMode = EmitterTransformMode.Shift;
            activeEmitters = TransformEmitterFlags.All;
        }

        void Update()
        {
            if (!isLatched || directionFlags == TransformDirectionFlags.None) return;

            if (Time.time >= nextEventTime)
            {
                // current frame may change during playback so get current frame emitters each time
                ulong[] emitterGroup = frameManager.GetCurrentFrameEmitters();

                DoDirectionTransform(emitterGroup, directionFlags);
                nextEventTime = Time.time + repeatRate;
            }
        }

        // transform by direction
        public void DoTransform(ulong[] emitterGroup, int direction)
        {
            // use flags to allow two directions at once if applicable
            TransformDirectionFlags directionFlag = (TransformDirectionFlags)(1 << direction);

            // if not latched do transform immediately
            if (!isLatched)
            {
                DoDirectionTransform(emitterGroup, directionFlag);
                return;
            }

            // latched enabled and direction already latched, clear it
            if (directionFlags.HasFlag(directionFlag))
            {
                directionFlags &= ~directionFlag;

                onDirectionLatchStateChanged?.Invoke(direction, false);
                return;
            }

            // otherwise direction not already latched, set it and clear opposing direction
            directionFlags &= ~(TransformDirectionFlags)(1 << (direction ^ 1));
            onDirectionLatchStateChanged?.Invoke(direction ^ 1, false);

            directionFlags |= directionFlag;
            onDirectionLatchStateChanged?.Invoke(direction, true);

            nextEventTime = Time.time;
        }

        public void ToggleWrapping()
        {
            doWrap = !doWrap;

            shiftTransformService.Wrap = doWrap;

            onWrapStateChanged?.Invoke(doWrap);
        }

        public void ToggleLatch()
        {
            isLatched = !isLatched;

            if (!isLatched) directionFlags = TransformDirectionFlags.None;

            onLatchStateChanged?.Invoke(isLatched);
        }       

        public void ActionOnBpmChanged(int newBpm)
        {
            bpm = newBpm;
            repeatRate = 60f / bpm;
            nextEventTime = Time.time + repeatRate;
        }

        public void RandomiseEmitters(ulong[] emitterGroup, int targetCount)
        {
            randomTransformService.DoRandomTransform(emitterGroup, targetCount, activeEmitters);
            onEmittersTransformed?.Invoke(emitterGroup);
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

                onTransformModeChanged?.Invoke(transformMode);
            }
        }

        private void DoDirectionTransform(ulong[] emitterGroup, TransformDirectionFlags direction)
        {
            transformedGroup = transformServices[(int)transformMode].DoTransform(emitterGroup, activeEmitters, direction);

            onEmittersTransformed?.Invoke(transformedGroup);
        }
    }
}
