using System;

namespace TemperaMental.UI.State
{
    [Flags]
    public enum UIStates
    {
        // canvas group bitmask
        None = 0,
        Device = 1,
        File = 2,
        Frame = 4,
        Transform = 8,
        Direction = 16,
        Record = 32,
        Playback = 64,
        Creation = 128,
        Selection = 256,

        // presets
        All = Device | File | Frame | Transform | Direction | Record | Playback | Creation | Selection,
        Playing = Playback | Device | Frame | Creation | Selection | Record,
        Paused = Playback | Device | Frame | Creation | Selection | Record,

        Recording = Device | File | Frame | Transform | Direction | Record | Creation | Selection,

        // recording from the Tempera writes the frames, so frame edits, files and transforms wait
        TemperaRecording = Playback | Device | Frame | Record,
    }
}
