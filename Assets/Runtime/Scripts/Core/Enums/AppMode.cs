using System;

namespace TemperaMental.Core
{
    // the contextual modes: each shows its own controls around the fixed core (header, display, grid and transport)
    public enum AppMode
    {
        Play,
        Transform,
        Edit,
        Record,
        Sound,
        Settings
    }

    // the modes a group of controls is shown in
    [Flags]
    public enum AppModes
    {
        None = 0,
        Play = 1 << AppMode.Play,
        Transform = 1 << AppMode.Transform,
        Edit = 1 << AppMode.Edit,
        Record = 1 << AppMode.Record,
        Sound = 1 << AppMode.Sound,
        Settings = 1 << AppMode.Settings,

        All = Play | Transform | Edit | Record | Sound | Settings
    }
}
