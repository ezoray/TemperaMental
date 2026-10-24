namespace TemperaMental.Midi.Clock
{
    // a clock step between beat boundaries (a MIDI clock pulse, or a tick asked for with ScheduleTick),
    // raised by the ClockEngine on its own thread
    public readonly struct ClockTick
    {
        // the restart this step belongs to, as for ClockBeat
        public readonly long Epoch;

        // ticks since the last restart (TicksPerBeat per beat)
        public readonly long Tick;

        // Stopwatch timestamp the step was scheduled for
        public readonly long Timestamp;

        public ClockTick(long epoch, long tick, long timestamp)
        {
            Epoch = epoch;
            Tick = tick;
            Timestamp = timestamp;
        }
    }
}
