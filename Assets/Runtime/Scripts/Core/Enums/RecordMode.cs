namespace TemperaMental.Core
{
    public enum RecordMode
    {
        Replace,    // frames are rewritten as the playhead passes, and added past the end
        Overdub     // the frames play as usual, and what's played on the Tempera is added to them
    }
}
