using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Midi.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.Midi.IO
{
    // key isPressed check is required as the file system dialog box steals focus from the app without it knowing which
    // can lead to held keys having their state inverted
    // due to this there are no keyboard shortcuts for loading and saving
    public class FileEventController : MonoBehaviour
    {
        [SerializeField] MidiManager midiManager;
        [SerializeField] MidiFileService midiFileService;
        [SerializeField] FrameManager frameManager;

        public void OnClickAppendMidiFileAsFrames()
        {
            if (Keyboard.current.anyKey.isPressed) return;

            try
            {
                if (midiFileService.TryOpenMidiFile(out MidiFile midiFile, true))
                {
                    List<Frame> frames = midiManager.FromMidiFileToFrames(midiFile);
                    frameManager.AppendFrames(frames);

                    LogMan.Log("File appended");
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Error appending file: {ex}");
            }
        }

        public void OnClickLoadMidiFileAsFrames()
        {
            if (Keyboard.current.anyKey.isPressed) return;

            try
            {
                if (midiFileService.TryOpenMidiFile(out MidiFile midiFile))
                {
                    List<Frame> frames = midiManager.FromMidiFileToFrames(midiFile);
                    frameManager.SetFrames(frames);

                    LogMan.Log("File loaded");
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Error loading file: {ex}");
            }
        }

        public void OnClickSaveFramesAsMidiFile()
        {
            if (Keyboard.current.anyKey.isPressed) return;

            try
            {
                MidiFile midiFile = midiManager.FromFramesToMidiFile(frameManager.GetFrames());

                if (midiFileService.TrySaveMidiFile(midiFile))
                {
                    LogMan.Log("File saved");
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Error saving file: {ex}");
            }
        }
    }
}
