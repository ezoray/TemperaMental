using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Midi.Core;
using UnityEngine;

namespace TemperaMental.Midi.IO
{
    public class FileEventController : MonoBehaviour
    {
        [SerializeField] MidiManager midiManager;
        [SerializeField] MidiFileService midiFileService;
        [SerializeField] FrameManager frameManager;

        public void OnClickAppendMidiFileAsFrames()
        {
            try
            {
                if (midiFileService.TryOpenMidiFile(out MidiFile midiFile))
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
