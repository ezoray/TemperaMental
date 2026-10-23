namespace TemperaMental.Midi.Clock
{
    // how punctual the clock thread has been since the stats were last taken
    public readonly struct ClockTimingStats
    {
        public readonly long Steps;
        public readonly long LateSteps;       // steps raised more than a millisecond after their scheduled time
        public readonly double MaxLatenessMs;

        public ClockTimingStats(long steps, long lateSteps, double maxLatenessMs)
        {
            Steps = steps;
            LateSteps = lateSteps;
            MaxLatenessMs = maxLatenessMs;
        }
    }
}
