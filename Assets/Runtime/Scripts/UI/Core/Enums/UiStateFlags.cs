using System;

namespace TemperaMental.UI.Core
{
    [Flags]
    public enum UiStateFlags
    {
        // canvas group indexes
        None = 0,
        Device = 1,
        File = 2,
        Mode = 4,
        Frame = 8,
        FrameShift = 16,
        Playback = 32,
        Creation = 64,
        Selection = 128,

        // presets
        All = Device | File | Mode | Frame | FrameShift | Playback | Creation | Selection,
        Playing = Playback | Device | Frame | FrameShift | Selection,
        Paused = Playback | Device | Frame | FrameShift | Selection
    }
}
