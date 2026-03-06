using System;

namespace Tempera.Mental.Core
{
    [Flags]
    public enum PlaybackFlags
    {
        Play = 1,
        Pause = 2,
        Stop = 4,

        Playing = Pause | Stop,
        Paused = Play | Stop,
        Stopped = Play
    }
}
