using System;
using TemperaMental.Core;

namespace TemperaMental.Frames
{
    // a timed change inside a frame. Tick is how far into the frame's beat it happens, 0 to TicksPerBeat - 1
    public readonly struct FrameEvent : IEquatable<FrameEvent>
    {
        public static readonly FrameEvent[] None = new FrameEvent[0];

        static readonly string[] EmitterNames = { "Blue", "Red", "Yellow", "Green" };

        public readonly int Tick;
        public readonly FrameEventType Type;
        public readonly int EmitterId;  // Select and Place, otherwise -1
        public readonly int Cell;       // Place and Remove (Tempera cell index), otherwise -1

        FrameEvent(int tick, FrameEventType type, int emitterId, int cell)
        {
            Tick = tick;
            Type = type;
            EmitterId = emitterId;
            Cell = cell;
        }

        public static FrameEvent Select(int tick, int emitterId) => new FrameEvent(tick, FrameEventType.Select, emitterId, -1);

        public static FrameEvent Place(int tick, int emitterId, int cell) => new FrameEvent(tick, FrameEventType.Place, emitterId, cell);

        public static FrameEvent Remove(int tick, int cell) => new FrameEvent(tick, FrameEventType.Remove, -1, cell);

        public static FrameEvent ClearAll(int tick) => new FrameEvent(tick, FrameEventType.ClearAll, -1, -1);

        public FrameEvent WithTick(int tick) => new FrameEvent(tick, Type, EmitterId, Cell);

        // the same event for another emitter and cell, used when a transform moves it
        public FrameEvent MovedTo(int emitterId, int cell) => new FrameEvent(Tick, Type, emitterId, cell);

        // applies the event to a set of emitter groups, as the device would
        public void ApplyTo(ulong[] groups)
        {
            switch (Type)
            {
                case FrameEventType.Place:
                    ulong placed = 1UL << Cell;

                    for (int i = 0; i < groups.Length; i++)
                        groups[i] &= ~placed;

                    groups[EmitterId] |= placed;
                    break;

                case FrameEventType.Remove:
                    ulong removed = 1UL << Cell;

                    for (int i = 0; i < groups.Length; i++)
                        groups[i] &= ~removed;
                    break;

                case FrameEventType.ClearAll:
                    Array.Clear(groups, 0, groups.Length);
                    break;
            }
        }

        public static bool AreSame(FrameEvent[] a, FrameEvent[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (!a[i].Equals(b[i])) return false;
            }

            return true;
        }

        public bool Equals(FrameEvent other) =>
            Tick == other.Tick && Type == other.Type && EmitterId == other.EmitterId && Cell == other.Cell;

        public override bool Equals(object obj) => obj is FrameEvent other && Equals(other);

        public override int GetHashCode() => ((Tick * 31 + (int)Type) * 31 + EmitterId) * 131 + Cell;

        public override string ToString()
        {
            string emitter = EmitterId >= 0 && EmitterId < EmitterNames.Length ? EmitterNames[EmitterId] : EmitterId.ToString();

            return Type switch
            {
                FrameEventType.Select => $"t{Tick} select {emitter}",
                FrameEventType.Place => $"t{Tick} place {emitter}@{Cell}",
                FrameEventType.Remove => $"t{Tick} remove @{Cell}",
                _ => $"t{Tick} clear all"
            };
        }
    }
}
