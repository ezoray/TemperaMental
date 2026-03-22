using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Midi.IO;
using TemperaMental.Midi.Transforms;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Core
{
    public class MidiEventController : MonoBehaviour
    {
        [SerializeField] MidiFileService midiFileService;
        [SerializeField] FrameManager frameManager;
        [SerializeField] TransformService transformService;

        [SerializeField] UnityEvent<int> onSetBpm;

        public void OnClickAppendMidiFileAsFrames()
        {
            try
            {
                if(midiFileService.TryOpenMidiFile(out MidiFile midiFile))
                {
                    List<Frame> frames = transformService.FromMidiFileToFrames(midiFile);
                    frameManager.AppendFrames(frames);

                    LogMan.Log("File appended");
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError($"{ex}");
            }
        }

        public void OnClickLoadMidiFileAsFrames()
        {
            try
            {
                if (midiFileService.TryOpenMidiFile(out MidiFile midiFile))
                {

                    List<Frame> frames = transformService.FromMidiFileToFrames(midiFile);
                    frameManager.SetFrames(frames);

                    int bpm = MidiUtils.GetBpmFromMidiFile(midiFile);

                    onSetBpm?.Invoke(bpm);

                    LogMan.Log("File loaded");
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError($"{ex}");
            }
        }

        public void OnClickSaveFramesAsMidiFile()
        {
            try
            {
                MidiFile midiFile = transformService.FromFramesToMidiFile(frameManager.GetFrames());
                if(midiFileService.TrySaveMidiFile(midiFile))
                {
                    LogMan.Log("File saved");
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError($"{ex}");
            }
        }

        public void OnClickConvertFramesToMidiFile()
        {
            try
            {
                MidiFile midiFile = transformService.FromFramesToMidiFile(frameManager.GetFrames());
            }
            catch (Exception ex)
            {
                LogMan.LogError($"{ex}");
            }
        }

        public void ActionOnBpmValueChanged(float bpm)
        {
            transformService.SetBpm((int)bpm);
        }
    }
}
