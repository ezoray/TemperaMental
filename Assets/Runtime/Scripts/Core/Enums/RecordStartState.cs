namespace TemperaMental.Core
{
    // what the Tempera and the first recorded frame start from, for a Replace recording started from stopped
    public enum RecordStartState
    {
        Clear,  // both cleared when armed
        Send,   // the current frame is sent to the Tempera when armed
        Keep    // whatever the Tempera is known to show
    }
}
