using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using TemperaMental.Frames;
using TemperaMental.Midi.Transforms;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Tempera.Mental.Midi.Core
{
    public class MidiManager : MonoBehaviour
    {
        [SerializeField] TransformService transformService;

        int bpm;

        [SerializeField] UnityEvent<int> onBpmChanged;


        public List<Frame> FromMidiFileToFrames(MidiFile midiFile)
        {
            bpm = MidiUtils.GetBpmFromMidiFile(midiFile);

            onBpmChanged?.Invoke(bpm);

            return transformService.FromMidiFileToFrames(midiFile);
        }

        public MidiFile FromFramesToMidiFile(IReadOnlyList<Frame> frames, int startFrame = 1)
        {
            return transformService.FromFramesToMidiFile(frames, bpm, startFrame);
        }
 
        public void SetBpm(int newBpm)
        {
            if (newBpm == bpm) return;

            bpm = newBpm;

            onBpmChanged?.Invoke(bpm);
        }


        public int Bpm { get => bpm; }
    }
}
