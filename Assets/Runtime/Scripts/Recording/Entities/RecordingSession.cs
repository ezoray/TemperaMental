using TemperaMental.Core;

namespace TemperaMental.Recording
{
    // a recording as the sequencer runs it; a new instance is published when it starts and again when it ends.
    // Ticks are transport ticks of the clock epoch it records in
    public sealed class RecordingSession
    {
        public readonly int Id;
        public readonly RecordMode Mode;
        public readonly long Epoch;

        // the beat it started in, the frame that beat played, and how far into that beat it started (0 at a frame start)
        public readonly long StartBeat;
        public readonly int StartFrame;
        public readonly int StartTick;

        // when it started (the trigger), as a Stopwatch timestamp
        public readonly long StartTimestamp;

        // started while already playing: the first frame keeps what it held before StartTick
        public readonly bool IsPunchIn;

        // beats it records for before ending by itself, 0 for no limit
        public readonly int LengthBeats;

        public readonly bool IsEnded;

        // the transport tick it ended at; long.MaxValue while running, or if unknown
        public readonly long EndTick;

        public RecordingSession(int id, RecordMode mode, long epoch, long startBeat, int startFrame, int startTick,
            long startTimestamp, bool isPunchIn, int lengthBeats, bool isEnded = false, long endTick = long.MaxValue)
        {
            IsPunchIn = isPunchIn;
            Id = id;
            Mode = mode;
            Epoch = epoch;
            StartBeat = startBeat;
            StartFrame = startFrame;
            StartTick = startTick;
            StartTimestamp = startTimestamp;
            LengthBeats = lengthBeats;
            IsEnded = isEnded;
            EndTick = endTick;
        }

        public long StartPosition(int ticksPerBeat) => StartBeat * ticksPerBeat + StartTick;

        public RecordingSession Ended(long endTick) =>
            new RecordingSession(Id, Mode, Epoch, StartBeat, StartFrame, StartTick, StartTimestamp, IsPunchIn, LengthBeats, true, endTick);
    }
}
