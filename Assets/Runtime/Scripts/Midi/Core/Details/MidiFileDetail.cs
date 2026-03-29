using Melanchall.DryWetMidi.Core;

namespace TemperaMental.Midi.Core
{
    public struct MidiFileDetail
    {
        public readonly MidiFile ForwardMidiFile;
        public readonly MidiFile ReverseMidiFile;

        public MidiFileDetail(MidiFile forwardMidiFile, MidiFile reverseMidiFile)
        {
            ForwardMidiFile = forwardMidiFile;
            ReverseMidiFile = reverseMidiFile;
        }
    }
}
