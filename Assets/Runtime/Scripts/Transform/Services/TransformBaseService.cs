using System;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Transforms
{
    public abstract class TransformBaseService : MonoBehaviour
    {
        protected TransformDirections allowedDirections;
        protected TransformLatchableDirections latchableDirections;

        TransformMode transformMode;
        bool isLatched;

        int tickCounter;

        SimpleModeState simpleModeState;
        IndividualModeState individualModeState;
        ITransformModeState currentModeState;

        protected ulong[] transformedGroups;

        public event Action<TransformDirections, bool> OnDirectionLatchStateChanged;
        public event Action<ulong[]> OnEmittersTransformed;

        [SerializeField] UnityEvent<int, TransformDetail> onEmitterSelected;
        [SerializeField] UnityEvent<bool> onLatchStateChanged;
        [SerializeField] UnityEvent<int> onTransformRateChanged;

        protected abstract ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction);
        public abstract ulong[] DoTransform(ulong[] groups);

        // immediate (unlatched) single-press transform for Individual mode — applies
        // 'direction' to the currently selected emitter only (IndividualEmitter) and
        // returns the result. Mirrors DoSingleTransform's role for Simple mode, since
        // Individual mode's per-emitter direction state is only populated via latching
        // and is otherwise empty for a one-shot unlatched press, leaving ActiveEmitters
        // empty and DoSingleTransform's active-emitter loop with nothing to process.
        protected abstract ulong[] DoSingleTransformForSelectedEmitter(ulong[] groups, TransformDirections direction);

        protected virtual void Awake()
        {
            transformedGroups = new ulong[ConfigRegistry.Grid.EmitterCount];

            simpleModeState = new SimpleModeState(ConfigRegistry.Transform.DefaultRate);
            individualModeState = new IndividualModeState(ConfigRegistry.Transform.DefaultRate);

            // Start in simple mode
            currentModeState = simpleModeState;
        }

        public TransformDetail GetTransformDetail()
        {
            return new TransformDetail(
                transformMode,
                currentModeState.GetEmitter(),
                isLatched,
                allowedDirections,
                latchableDirections,
                currentModeState.GetDirections(),
                currentModeState.GetRate());
        }

        public void HandleDirectionChange(ulong[] emitterGroup, int directionValue)
        {
            TransformDirections direction = (TransformDirections)(1 << directionValue);

            if ((allowedDirections & direction) == 0)
                return;

            TransformLatchableDirections latchableDirection = (TransformLatchableDirections)(1 << directionValue);
            bool canLatch = (latchableDirections & latchableDirection) != 0;

            // non-latched or non-latchable direction:
            // perform immediate transform
            if (!isLatched || !canLatch)
            {
                ulong[] result = transformMode == TransformMode.Individual
                    ? DoSingleTransformForSelectedEmitter(emitterGroup, direction)
                    : DoSingleTransform(emitterGroup, direction);

                OnEmittersTransformed?.Invoke(result);
                return;
            }

            TransformDirections activeDirections = currentModeState.GetDirections();

            // toggle off existing latched direction
            if ((activeDirections & direction) != 0)
            {
                activeDirections &= ~direction;
                currentModeState.SetDirections(activeDirections);
                OnDirectionLatchStateChanged?.Invoke(direction, false);
                return;
            }

            // remove opposing direction
            TransformDirections opposing = (TransformDirections)(1 << (directionValue ^ 1));

            if ((activeDirections & opposing) != 0)
            {
                activeDirections &= ~opposing;
                currentModeState.SetDirections(activeDirections);
                OnDirectionLatchStateChanged?.Invoke(opposing, false);
            }

            // enable new direction
            activeDirections |= direction;
            currentModeState.SetDirections(activeDirections);
            OnDirectionLatchStateChanged?.Invoke(direction, true);
        }

        public void SetTransformRate(int rate)
        {
            currentModeState.SetRate(rate);
        }

        public bool TickAndCheck()
        {
            tickCounter++;
            return currentModeState.ShouldFire(tickCounter);
        }

        public TransformActiveEmitters GetActiveEmitters()
        {
            return currentModeState.GetEmitter();
        }

        public void SelectEmitter(int emitterId)
        {
            currentModeState.SetEmitter(emitterId);

            onEmitterSelected?.Invoke(emitterId, GetTransformDetail());
            onTransformRateChanged?.Invoke(currentModeState.GetRate());
        }

        public void ToggleLatch()
        {
            isLatched = !isLatched;

            if (!isLatched)
                currentModeState.SetDirections(TransformDirections.None);

            onLatchStateChanged?.Invoke(isLatched);
        }

        public void ClearLatch()
        {
            isLatched = false;
            currentModeState.SetDirections(TransformDirections.None);

            onLatchStateChanged?.Invoke(isLatched);
        }

        public virtual void ResetTransform()
        {
            isLatched = false;

            simpleModeState.Reset();
            individualModeState.Reset();

            tickCounter = 0;
        }

        public TransformMode ChangeTransformMode()
        {
            transformMode = transformMode == TransformMode.Simple
                ? TransformMode.Individual
                : TransformMode.Simple;

            currentModeState = transformMode == TransformMode.Simple
                ? simpleModeState
                : individualModeState;

            return transformMode;
        }

        public bool IsLatched => isLatched;
        public TransformMode TransformMode => transformMode;

        protected TransformActiveEmitters ActiveEmitters => currentModeState.GetActiveEmitters();
        protected bool ShouldEmitterFire(int emitterId) => individualModeState.ShouldEmitterFire(emitterId, tickCounter);
        protected TransformDirections GetEmitterDirections(int emitterId) => individualModeState.GetDirections(emitterId);
        protected TransformDirections GetDirections() => currentModeState.GetDirections();
        protected int IndividualEmitter => individualModeState.SelectedEmitter;
    }
}