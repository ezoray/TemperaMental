using System;
using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Frames;
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

        // scratch buffers for transforming a frame's events, used under the manager's transformLock like everything else
        ulong[] inputGroups;
        ulong[] eventGroups;
        ulong[] probeGroups;
        readonly List<FrameEvent> transformedEvents = new List<FrameEvent>();
        Func<ulong[], ulong[]> latchedTransform;

        public event Action<TransformDirections, bool> OnDirectionLatchStateChanged;
        public event Action<FrameSnapshot> OnEmittersTransformed;

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
            inputGroups = new ulong[ConfigRegistry.Grid.EmitterCount];
            eventGroups = new ulong[ConfigRegistry.Grid.EmitterCount];
            probeGroups = new ulong[ConfigRegistry.Grid.EmitterCount];
            latchedTransform = DoTransform;

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

        public void HandleDirectionChange(FrameSnapshot frame, int directionValue)
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
                Func<ulong[], ulong[]> transform = transformMode == TransformMode.Individual
                    ? groups => DoSingleTransformForSelectedEmitter(groups, direction)
                    : groups => DoSingleTransform(groups, direction);

                // the snapshot is shared, so the transforms work on a copy
                Array.Copy(frame.StartGroups, inputGroups, inputGroups.Length);
                ulong[] result = (ulong[])transform(inputGroups).Clone();

                FrameEvent[] events = TransformEvents(frame.StartGroups, result, frame.Events, transform);

                OnEmittersTransformed?.Invoke(new FrameSnapshot(result, events));
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

        // a latched step: call straight after DoTransform with the groups it was given and its result, so the events
        // move with the same settings and per-emitter rates
        public FrameEvent[] TransformEventsLatched(ulong[] startGroups, ulong[] resultGroups, FrameEvent[] events)
        {
            return TransformEvents(startGroups, resultGroups, events, latchedTransform);
        }

        // moves a frame's events with its emitters. Each placement or removal is passed through the transform as a
        // lone emitter of its colour, so it lands where that emitter would (including 2-Lane reassignment), without
        // being blocked by the frame's other emitters; one moved off the grid is dropped. A removal takes the colour
        // of the emitter at its cell when it plays. Colour selects only change where a transform swaps colours.
        // Returns the same array when nothing moved
        protected virtual FrameEvent[] TransformEvents(ulong[] startGroups, ulong[] resultGroups, FrameEvent[] events,
            Func<ulong[], ulong[]> transform)
        {
            if (events.Length == 0) return events;

            Array.Copy(startGroups, eventGroups, eventGroups.Length);
            transformedEvents.Clear();
            bool isChanged = false;

            foreach (FrameEvent frameEvent in events)
            {
                FrameEvent moved = frameEvent;
                bool isKept = true;

                switch (frameEvent.Type)
                {
                    case FrameEventType.Place:
                        isKept = TryMoveEmitter(frameEvent.EmitterId, frameEvent.Cell, transform, out int placeEmitter, out int placeCell);
                        moved = frameEvent.MovedTo(placeEmitter, placeCell);
                        break;

                    case FrameEventType.Remove:
                        int removedEmitter = GetEmitterAt(eventGroups, frameEvent.Cell);
                        isKept = false;

                        // nothing there to remove, so nothing to move
                        if (removedEmitter >= 0 &&
                            TryMoveEmitter(removedEmitter, frameEvent.Cell, transform, out _, out int removeCell))
                        {
                            isKept = true;
                            moved = frameEvent.MovedTo(-1, removeCell);
                        }
                        break;

                    case FrameEventType.Select:
                        moved = frameEvent.MovedTo(TransformSelectedEmitter(frameEvent.EmitterId, transform), -1);
                        break;
                }

                frameEvent.ApplyTo(eventGroups);

                if (!isKept || !moved.Equals(frameEvent)) isChanged = true;
                if (isKept) transformedEvents.Add(moved);
            }

            return isChanged ? transformedEvents.ToArray() : events;
        }

        // the colour a colour select becomes; only transforms that swap colours change it
        protected virtual int TransformSelectedEmitter(int emitterId, Func<ulong[], ulong[]> transform)
        {
            return emitterId;
        }

        // passes one emitter through the transform on an otherwise empty grid
        protected bool TryMoveEmitter(int emitterId, int cell, Func<ulong[], ulong[]> transform, out int newEmitterId, out int newCell)
        {
            Array.Clear(probeGroups, 0, probeGroups.Length);
            probeGroups[emitterId] = 1UL << cell;

            ulong[] result = transform(probeGroups);

            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] == 0) continue;

                newEmitterId = i;
                newCell = LowestBit(result[i]);
                return true;
            }

            newEmitterId = -1;
            newCell = -1;
            return false;
        }

        private static int GetEmitterAt(ulong[] groups, int cell)
        {
            ulong bit = 1UL << cell;

            for (int i = 0; i < groups.Length; i++)
            {
                if ((groups[i] & bit) != 0) return i;
            }

            return -1;
        }

        private static int LowestBit(ulong value)
        {
            int index = 0;

            while ((value & 1UL) == 0)
            {
                value >>= 1;
                index++;
            }

            return index;
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