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
        [SerializeField] UnityEvent onFramesSaved;

        public void SaveAsMidiFile(IReadOnlyList<Frame> frames, int bpm)
        {
            try
            {
                MidiFile midiFile = transformService.FromFramesToMidiFile(frames, bpm);

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
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Error loading file: {ex}");
            }
        }
    }
}
