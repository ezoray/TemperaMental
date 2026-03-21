using System;

namespace Tempera.Mental.Core.UI
{
    [Flags]
    public enum UiState
    {
        None = 0,
        Device = 1,
        File = 2,
        Mode = 4,
        Playback = 8,
        Creation = 16,
        Selection = 32,

        All = Device | File | Mode | Playback | Creation | Selection
    }
}
