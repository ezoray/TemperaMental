using Melanchall.DryWetMidi.Core;

namespace TemperaMental.Midi.Core
{
    // a plain copy of one MIDI message, safe to pass between threads
    // (DryWetMIDI event objects are reused by the output scheduler, so they can't be queued directly)
    public readonly struct MidiMessageData
    {
        public readonly long Timestamp;   // Stopwatch timestamp
        public readonly MidiMessageKind Kind;
        public readonly byte Channel;     // 0–15 for channel messages
        public readonly int Data1;        // note, controller, program; 14-bit value for pitch bend and song position
        public readonly int Data2;        // velocity, controller value, pressure

        public MidiMessageData(long timestamp, MidiMessageKind kind, byte channel = 0, int data1 = 0, int data2 = 0)
        {
            Timestamp = timestamp;
            Kind = kind;
            Channel = channel;
            Data1 = data1;
            Data2 = data2;
        }

        public bool IsChannelMessage => Kind >= MidiMessageKind.NoteOn && Kind <= MidiMessageKind.PolyPressure;

        public static MidiMessageData FromEvent(MidiEvent midiEvent, long timestamp)
        {
            switch (midiEvent)
            {
                case NoteOnEvent noteOn:
                    // a Note On with velocity 0 is a Note Off
                    return noteOn.Velocity == 0
                        ? new MidiMessageData(timestamp, MidiMessageKind.NoteOff, noteOn.Channel, noteOn.NoteNumber, 0)
                        : new MidiMessageData(timestamp, MidiMessageKind.NoteOn, noteOn.Channel, noteOn.NoteNumber, noteOn.Velocity);

                case NoteOffEvent noteOff:
                    return new MidiMessageData(timestamp, MidiMessageKind.NoteOff, noteOff.Channel, noteOff.NoteNumber, noteOff.Velocity);

                case ControlChangeEvent controlChange:
                    return new MidiMessageData(timestamp, MidiMessageKind.ControlChange, controlChange.Channel, controlChange.ControlNumber, controlChange.ControlValue);

                case ProgramChangeEvent programChange:
                    return new MidiMessageData(timestamp, MidiMessageKind.ProgramChange, programChange.Channel, programChange.ProgramNumber);

                case PitchBendEvent pitchBend:
                    return new MidiMessageData(timestamp, MidiMessageKind.PitchBend, pitchBend.Channel, pitchBend.PitchValue);

                case ChannelAftertouchEvent channelPressure:
                    return new MidiMessageData(timestamp, MidiMessageKind.ChannelPressure, channelPressure.Channel, channelPressure.AftertouchValue);

                case NoteAftertouchEvent polyPressure:
                    return new MidiMessageData(timestamp, MidiMessageKind.PolyPressure, polyPressure.Channel, polyPressure.NoteNumber, polyPressure.AftertouchValue);

                case SongPositionPointerEvent songPosition:
                    return new MidiMessageData(timestamp, MidiMessageKind.SongPosition, data1: songPosition.PointerValue);

                case SysExEvent sysEx:
                    return new MidiMessageData(timestamp, MidiMessageKind.SysEx, data1: sysEx.Data?.Length ?? 0);

                case TimingClockEvent _:
                    return new MidiMessageData(timestamp, MidiMessageKind.TimingClock);

                case StartEvent _:
                    return new MidiMessageData(timestamp, MidiMessageKind.Start);

                case ContinueEvent _:
                    return new MidiMessageData(timestamp, MidiMessageKind.Continue);

                case StopEvent _:
                    return new MidiMessageData(timestamp, MidiMessageKind.Stop);

                case ActiveSensingEvent _:
                    return new MidiMessageData(timestamp, MidiMessageKind.ActiveSensing);

                case ResetEvent _:
                    return new MidiMessageData(timestamp, MidiMessageKind.Reset);

                default:
                    return new MidiMessageData(timestamp, MidiMessageKind.Other);
            }
        }
    }
}
