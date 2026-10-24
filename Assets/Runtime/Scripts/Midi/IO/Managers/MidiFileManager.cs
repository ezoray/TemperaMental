using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Midi.Transforms;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.IO
{
    public class MidiFileManager : MonoBehaviour
    {
        [SerializeField] MidiFileService fileService;
        [SerializeField] MidiTransformService transformService;

        const string appendLog = "appended";
        const string loadLog = "loaded";

        [SerializeField] UnityEvent<int> onBpmLoaded;
        [SerializeField] UnityEvent<List<Frame>, bool> onFramesLoaded;

        // a loaded file's canvas (Program Change), or -1; not raised when appending
        [SerializeField] UnityEvent<int> onCanvasLoaded = new UnityEvent<int>();
        [SerializeField] UnityEvent onFramesSaved;

        // canvas: Program Change 0–127 saved with the frames, or -1 for none
        public void SaveAsMidiFile(IReadOnlyList<Frame> frames, int bpm, int canvas)
        {
            try
            {
                MidiFile midiFile = transformService.FromFramesToMidiFile(frames, bpm, canvas);

                if (fileService.TrySaveMidiFile(midiFile))
                {
                    LogMan.Log("File saved");

                    onFramesSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Error saving file: {ex}");
            }
        }

        public void LoadMidiFile(bool isAppend)
        {
            try
            {
                if (fileService.TryOpenMidiFile(out MidiFile midiFile, isAppend))
                {
                    List<Frame> frames = transformService.FromMidiFileToFrames(midiFile);
                    int bpm = MidiUtils.GetBpmFromMidiFile(midiFile);

                    LogMan.Log($"File " + (isAppend ? appendLog : loadLog));

                    onBpmLoaded?.Invoke(bpm);
                    onFramesLoaded?.Invoke(frames, isAppend);

                    if (!isAppend) onCanvasLoaded?.Invoke(MidiUtils.GetProgramFromMidiFile(midiFile));
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Error loading file: {ex}");
            }
        }
    }
}
