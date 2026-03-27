using System;

namespace TemperaMental.Core
{
    [Flags]
    public enum PlaybackFlags
    {
        // button flags
        PlayPosition = 1,
        Play = 2,
        Pause = 4,
        Stop = 8,

        // state presets
        Playing = PlayPosition | Play | Pause | Stop,
        Paused = PlayPosition | Play | Stop,
        Stopped = PlayPosition | Play
    }
}
