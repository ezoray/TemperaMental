namespace TemperaMental.Core
{
    public enum PlaybackState
    {
        Idle,       // no playback object exists
        Playing,    // actively running
        Paused,     // stopped mid-way through
        Stopped
    }
}
