using System;

namespace TemperaMental.Input.Keyboards
{
    [Flags]
    public enum KeyboardStateFlags
    {
        // input handler indexes
        None = 0,
        Emitter = 1,
        File = 2,
        Mode = 4,
        Playback = 8,
        Creation = 16,
        Selection = 32,

        // presets
        All = Emitter | File | Mode | Playback | Creation | Selection,
        Playing = Playback | Selection,
        Paused = Playback | Selection
    }
}
