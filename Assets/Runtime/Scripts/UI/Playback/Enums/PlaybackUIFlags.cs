using System;

namespace TemperaMental.UI.Playbacks
{
    [Flags]
    public enum PlaybackUIFlags
    {
        // button interactable flags
        Play = 1,
        Pause = 2,
        Stop = 4,

        // state presets
        Idle = Play,
        Playing = Play | Pause | Stop,
        Paused =  Play | Stop,
        Stopped = Play | Stop
    }
}
