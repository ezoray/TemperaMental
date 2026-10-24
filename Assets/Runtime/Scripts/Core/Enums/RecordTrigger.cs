namespace TemperaMental.Core
{
    // what starts an armed recording
    public enum RecordTrigger
    {
        Start,      // the transport starting: Play, or the clock source's MIDI Start when following clock
        Touch,      // the first placement on the Tempera
        Either,     // whichever of those comes first
        Now         // straight away
    }
}
