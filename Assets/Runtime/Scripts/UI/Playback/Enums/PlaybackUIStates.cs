using System;

namespace TemperaMental.UI.Playbacks
{
    [Flags]
    public enum PlaybackUIStates
    {
        // button interactable flags
        PlayPause = 1,
        Stop = 2,

        // state presets
        Reset = PlayPause,
        Playing = PlayPause | Stop,
        Paused = PlayPause | Stop,
        Stopped = PlayPause | Stop
    }
}
