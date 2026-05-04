using System;

namespace TemperaMental.UI.Core
{
    [Flags]
    public enum UIStates
    {
        // canvas group indexes
        None = 0,
        Device = 1,
        File = 2,
        Mode = 4,
        Frame = 8,
        Transform = 16,
        Direction = 32,
        Record = 64,
        Playback = 128,
        Creation = 256,
        Selection = 512,

        // presets
        All = Device | File | Mode | Frame | Transform | Direction | Record | Playback | Creation | Selection,
        Playing = Playback | Device | Frame | Creation | Selection,
        Paused = Playback | Device | Frame | Creation | Selection,

        Recording = Device | File | Mode | Frame | Transform | Direction | Record | Creation | Selection,
    }
}
