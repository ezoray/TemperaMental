namespace TemperaMental.Midi.Core
{
    public enum MidiMessageKind
    {
        Other = 0,

        // channel messages
        NoteOn,
        NoteOff,
        ControlChange,
        ProgramChange,
        PitchBend,
        ChannelPressure,
        PolyPressure,

        // system common
        SongPosition,
        SysEx,

        // system realtime
        TimingClock,
        Start,
        Continue,
        Stop,
        ActiveSensing,
        Reset
    }
}
