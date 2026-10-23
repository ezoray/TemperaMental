namespace TemperaMental.Midi.Clock
{
    // a beat boundary raised by the ClockEngine on its own thread
    public readonly struct ClockBeat
    {
        // increments every time the clock is restarted, so listeners can ignore beats from before a restart
        public readonly long Epoch;

        // beats since the last restart, starting at 0
        public readonly long Beat;

        // Stopwatch timestamp the beat was scheduled for
        public readonly long Timestamp;

        public ClockBeat(long epoch, long beat, long timestamp)
        {
            Epoch = epoch;
            Beat = beat;
            Timestamp = timestamp;
        }
    }
}
