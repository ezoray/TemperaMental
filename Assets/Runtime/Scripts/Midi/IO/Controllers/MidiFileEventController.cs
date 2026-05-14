using System.Collections.Generic;
using TemperaMental.Frames;
using TemperaMental.Midi.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.Midi.IO
{
    // key isPressed check is required as the file system open/save dialog box steals focus from the app without it
    // knowing which can lead to held keys having their state inverted
    // due to this there are no keyboard shortcuts for loading and saving
    public class MidiFileEventController : MonoBehaviour
    {
        [SerializeField] MidiTempoManager midiTempoManager;
        [SerializeField] FrameManager frameManager;
        [SerializeField] MidiFileManager fileManager;

        private bool IsKeyHeld => Keyboard.current.anyKey.isPressed;


        public void OnClickAppendMidiFileAsFrames()
        {
            if (IsKeyHeld) return;

            fileManager.LoadMidiFile(true);
        }

        public void OnClickLoadMidiFileAsFrames()
        {
            if (IsKeyHeld) return;

            fileManager.LoadMidiFile(false);
        }

        public void OnClickSaveFramesAsMidiFile()
        {
            if (IsKeyHeld) return;

            int bpm = midiTempoManager.GetBpm();
            IReadOnlyList<Frame> frames = frameManager.GetFrames();

            fileManager.SaveAsMidiFile(frames, bpm);
        }
    }
}
