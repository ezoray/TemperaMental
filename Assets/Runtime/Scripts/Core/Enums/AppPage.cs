using System;

namespace TemperaMental.Core
{
    // the app's pages, one per job: each shows what that job needs around the fixed core (header with the page tabs,
    // the display and its row, the grid, and Play / Stop / Hold)
    public enum AppPage
    {
        Compose,
        Play,
        Transform,
        Record,
        Setup
    }

    // the pages a group of controls is shown on
    [Flags]
    public enum AppPages
    {
        None = 0,
        Compose = 1 << AppPage.Compose,
        Play = 1 << AppPage.Play,
        Transform = 1 << AppPage.Transform,
        Record = 1 << AppPage.Record,
        Setup = 1 << AppPage.Setup,

        All = Compose | Play | Transform | Record | Setup
    }
}
