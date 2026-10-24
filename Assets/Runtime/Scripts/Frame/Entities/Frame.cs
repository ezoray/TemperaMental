using System;
using System.Collections.Generic;
using System.Linq;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Settings;
using TemperaMental.Utils;
using UnityEngine;

namespace TemperaMental.Frames
{
    // One beat of the sequence: the emitters shown when the beat begins (the start groups), plus timed events
    // played during the beat. Drawing and transforms edit the start groups; the events are kept in tick order.
    public class Frame
    {
        // slightly dangerous to assume ConfigRegistry is initialised here
        static readonly int EmitterCount = ConfigRegistry.Grid.EmitterCount;
        static readonly int CellCount = ConfigRegistry.Grid.GridWidth * ConfigRegistry.Grid.GridHeight;
        static readonly int TicksPerBeat = ConfigRegistry.Midi.TicksPerBeat;

        readonly ulong[] emitterGroups;

        // never changed in place, so it can be shared between frames and snapshots
        FrameEvent[] events = FrameEvent.None;

        // read-only copy for other threads, rebuilt only after the frame changes
        FrameSnapshot snapshot;

        public Frame()
        {
            emitterGroups = new ulong[EmitterCount];
        }

        public Frame(Frame otherFrame)
        {
            emitterGroups = new ulong[EmitterCount];
            Array.Copy(otherFrame.emitterGroups, emitterGroups, EmitterCount);
            events = otherFrame.events;
        }

        public Frame(ulong[] emitterGroups)
        {
            this.emitterGroups = (ulong[])emitterGroups.Clone();
        }

        public Frame(FrameSnapshot content)
        {
            emitterGroups = (ulong[])content.StartGroups.Clone();
            events = Normalise(content.Events);
        }

        public void SetEmitterGroups(ulong[] groups)
        {
            Array.Copy(groups, emitterGroups, emitterGroups.Length);
            snapshot = null;
        }

        // replaces the start groups and the events
        public void SetContent(FrameSnapshot content)
        {
            Array.Copy(content.StartGroups, emitterGroups, emitterGroups.Length);
            events = Normalise(content.Events);
            snapshot = null;
        }

        public void SetEvents(IEnumerable<FrameEvent> newEvents)
        {
            events = Normalise(newEvents);
            snapshot = null;
        }

        // return reference to emitter array for direct access to up-to-date values
        public ulong[] GetEmitterGroups()
        {
            return emitterGroups;
        }

        public IReadOnlyList<FrameEvent> Events => events;

        public bool HasEvents => events.Length > 0;

        // copy that is never modified after it's returned, safe to hand to the clock thread
        public FrameSnapshot GetSnapshot()
        {
            if (snapshot == null)
            {
                snapshot = new FrameSnapshot((ulong[])emitterGroups.Clone(), events);
            }

            return snapshot;
        }

        public int AddEmitter(Vector2Int position, int emitterId, bool enforceTwoLane = true)
        {
            int pos = EmitterUtils.PositionToIndex(position);

            if (enforceTwoLane)
            {
                int laneOwner = EmitterUtils.GetLaneOwner(pos, EmitterSettingsManager.CurrentTwoLanes);
                if (laneOwner != -1)
                {
                    emitterId = laneOwner;
                }
            }

            for (int i = 0; i < EmitterCount; i++)
            {
                if (i == emitterId) continue;
                emitterGroups[i] &= ~(1UL << pos);
            }

            emitterGroups[emitterId] |= 1UL << pos;
            snapshot = null;

            return emitterId;
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
                    snapshot = null;
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

        // removes the emitters and the events
        public void Clear()
        {
            for (int i = 0; i < EmitterCount; i++)
            {
                emitterGroups[i] = 0;
            }

            events = FrameEvent.None;
            snapshot = null;
        }

        // events in tick order, keeping the order of events on the same tick. Ticks outside the beat are clamped
        // into it, and events with an emitter or cell out of range are dropped
        private static FrameEvent[] Normalise(IEnumerable<FrameEvent> source)
        {
            if (source == null) return FrameEvent.None;

            var valid = new List<FrameEvent>();
            bool isSorted = true;
            int lastTick = 0;

            foreach (FrameEvent frameEvent in source)
            {
                if (!IsValid(frameEvent)) continue;

                FrameEvent clamped = frameEvent.Tick < 0 || frameEvent.Tick >= TicksPerBeat
                    ? frameEvent.WithTick(Mathf.Clamp(frameEvent.Tick, 0, TicksPerBeat - 1))
                    : frameEvent;

                if (clamped.Tick < lastTick) isSorted = false;
                lastTick = clamped.Tick;

                valid.Add(clamped);
            }

            if (valid.Count == 0) return FrameEvent.None;

            // OrderBy is a stable sort, so events on the same tick keep their order
            return isSorted ? valid.ToArray() : valid.OrderBy(e => e.Tick).ToArray();
        }

        private static bool IsValid(FrameEvent frameEvent)
        {
            bool isEmitterValid = frameEvent.EmitterId >= 0 && frameEvent.EmitterId < EmitterCount;
            bool isCellValid = frameEvent.Cell >= 0 && frameEvent.Cell < CellCount;

            return frameEvent.Type switch
            {
                FrameEventType.Select => isEmitterValid,
                FrameEventType.Place => isEmitterValid && isCellValid,
                FrameEventType.Remove => isCellValid,
                FrameEventType.ClearAll => true,
                _ => false
            };
        }
    }
}
