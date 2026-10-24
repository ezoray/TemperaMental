namespace TemperaMental.Frames
{
    // an unchanging copy of a frame's content, safe to share with the clock thread.
    // StartGroups are the emitters when the frame's beat begins; Events are played during the beat, in tick order.
    // Neither array may be changed once the snapshot is made
    public sealed class FrameSnapshot
    {
        public readonly ulong[] StartGroups;
        public readonly FrameEvent[] Events;

        public FrameSnapshot(ulong[] startGroups, FrameEvent[] events)
        {
            StartGroups = startGroups;
            Events = events ?? FrameEvent.None;
        }

        public bool HasEvents => Events.Length > 0;

        // the emitters once every event has played
        public ulong[] GetEndGroups()
        {
            ulong[] groups = (ulong[])StartGroups.Clone();

            foreach (FrameEvent frameEvent in Events)
                frameEvent.ApplyTo(groups);

            return groups;
        }

        public bool HasSameContent(FrameSnapshot other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            for (int i = 0; i < StartGroups.Length; i++)
            {
                if (StartGroups[i] != other.StartGroups[i]) return false;
            }

            return FrameEvent.AreSame(Events, other.Events);
        }
    }
}
