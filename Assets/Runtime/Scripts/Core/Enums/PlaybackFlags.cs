using System;

namespace Tempera.Mental.Core
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
        Playing = Pause | Stop,
        Paused = Play | Stop,
        Stopped = PlayPosition | Play
    }
}
