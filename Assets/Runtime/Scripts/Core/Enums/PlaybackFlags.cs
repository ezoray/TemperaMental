using System;

namespace TemperaMental.Core
{
    [Flags]
    public enum PlaybackFlags
    {
        // button flags
        Play = 1,
        Pause = 2,
        Stop = 4,

        // state presets
        Playing = Play | Pause | Stop,
        Paused =  Play | Stop,
        Stopped = Play
    }
}
