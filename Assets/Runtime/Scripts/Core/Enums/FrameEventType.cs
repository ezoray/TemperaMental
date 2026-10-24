namespace TemperaMental.Core
{
    // a change made during a frame's beat
    public enum FrameEventType
    {
        Select,     // selects an emitter colour on the device; changes nothing on the grid
        Place,      // places an emitter at a cell
        Remove,     // removes whatever emitter is at a cell
        ClearAll    // removes every emitter
    }
}
