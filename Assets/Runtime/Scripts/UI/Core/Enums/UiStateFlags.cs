using System;

namespace TemperaMental.UI.Core
{
    [Flags]
    public enum UIStateFlags
    {
        // canvas group indexes
        None = 0,
        Device = 1,
        File = 2,
        Mode = 4,
        Frame = 8,
        Transform = 16,
        Direction = 32,
        Playback = 64,
        Creation = 128,
        Selection = 256,

        // presets
        All = Device | File | Mode | Frame | Transform | Direction | Playback | Creation | Selection,
        Playing = Playback | Device | Frame | Creation | Selection,
        Paused = Playback | Device | Frame | Creation | Selection
    }
}
