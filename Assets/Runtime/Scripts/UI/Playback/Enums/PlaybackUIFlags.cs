using System;

namespace TemperaMental.UI.Playbacks
{
    [Flags]
    public enum PlaybackUIFlags
    {
        // button interactable flags
        PlayPause = 1,
        Stop = 4,

        // state presets
        Idle = PlayPause | Stop,
        Playing = PlayPause | Stop,
        Paused = PlayPause | Stop,
        Stopped = PlayPause | Stop
    }
}
