using System;
using TemperaMental.Core;

namespace TemperaMental.Frames
{
    // a timed change inside a frame. Tick is how far into the frame's beat it happens, 0 to TicksPerBeat - 1
    public readonly struct FrameEvent : IEquatable<FrameEvent>
    {
        public static readonly FrameEvent[] None = new FrameEvent[0];

        static readonly string[] EmitterNames = { "Blue", "Red", "Yellow", "Green" };
        static readonly string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        public readonly int Tick;
        public readonly FrameEventType Type;
        public readonly int EmitterId;  // Select and Place; the MIDI channel (0–15) for notes; otherwise -1
        public readonly int Cell;       // Place and Remove (Tempera cell index); the note number for notes; otherwise -1
        public readonly int Value;      // velocity of a Note On, otherwise 0

        FrameEvent(int tick, FrameEventType type, int emitterId, int cell, int value = 0)
        {
            Tick = tick;
            Type = type;
            EmitterId = emitterId;
            Cell = cell;
            Value = value;
        }

        public bool IsNote => Type == FrameEventType.NoteOn || Type == FrameEventType.NoteOff;

        public int Channel => EmitterId;
        public int Note => Cell;
        public int Velocity => Value;

        public static FrameEvent Select(int tick, int emitterId) => new FrameEvent(tick, FrameEventType.Select, emitterId, -1);

        public static FrameEvent Place(int tick, int emitterId, int cell) => new FrameEvent(tick, FrameEventType.Place, emitterId, cell);

        public static FrameEvent Remove(int tick, int cell) => new FrameEvent(tick, FrameEventType.Remove, -1, cell);

        public static FrameEvent ClearAll(int tick) => new FrameEvent(tick, FrameEventType.ClearAll, -1, -1);

        // channel 0–15, note 0–127, velocity 1–127
        public static FrameEvent NoteOn(int tick, int channel, int note, int velocity) =>
            new FrameEvent(tick, FrameEventType.NoteOn, channel, note, velocity);

        public static FrameEvent NoteOff(int tick, int channel, int note) => new FrameEvent(tick, FrameEventType.NoteOff, channel, note);

        public FrameEvent WithTick(int tick) => new FrameEvent(tick, Type, EmitterId, Cell, Value);

        // the same event for another emitter and cell, used when a transform moves it
        public FrameEvent MovedTo(int emitterId, int cell) => new FrameEvent(Tick, Type, emitterId, cell, Value);

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
            Tick == other.Tick && Type == other.Type && EmitterId == other.EmitterId && Cell == other.Cell && Value == other.Value;

        public override bool Equals(object obj) => obj is FrameEvent other && Equals(other);

        public override int GetHashCode() => (((Tick * 31 + (int)Type) * 31 + EmitterId) * 131 + Cell) * 131 + Value;

        public override string ToString()
        {
            string emitter = EmitterId >= 0 && EmitterId < EmitterNames.Length ? EmitterNames[EmitterId] : EmitterId.ToString();

            return Type switch
            {
                FrameEventType.Select => $"t{Tick} select {emitter}",
                FrameEventType.Place => $"t{Tick} place {emitter}@{Cell}",
                FrameEventType.Remove => $"t{Tick} remove @{Cell}",
                FrameEventType.NoteOn => $"t{Tick} note on ch{Channel + 1} {NoteName(Note)} v{Velocity}",
                FrameEventType.NoteOff => $"t{Tick} note off ch{Channel + 1} {NoteName(Note)}",
                _ => $"t{Tick} clear all"
            };
        }

        // as the Tempera manual names them: note 0 is C-1, 60 is C4 (middle C)
        public static string NoteName(int note)
        {
            return $"{NoteNames[note % 12]}{note / 12 - 1}";
        }
    }
}
