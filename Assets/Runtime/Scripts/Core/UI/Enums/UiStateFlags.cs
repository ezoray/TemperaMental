using System;

namespace TemperaMental.Core.UI
{
    [Flags]
    public enum UiStateFlags
    {
        // canvas group indexes
        None = 0,
        Device = 1,
        File = 2,
        Mode = 4,
        Playback = 8,
        Creation = 16,
        Selection = 32,

        // presets
        All = Device | File | Mode | Playback | Creation | Selection,
        Playing = Playback | Selection
    }
}
