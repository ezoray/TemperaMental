using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Tempera.Mental.Midi.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Midi.IO;
using TemperaMental.Midi.Transforms;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiEventController : MonoBehaviour
    {
        [SerializeField] MidiManager midiManager;
        [SerializeField] MidiFileService midiFileService;
        [SerializeField] FrameManager frameManager;
        [SerializeField] TransformService transformService;

        public void OnClickAppendMidiFileAsFrames()
        {
            try
            {
                if(midiFileService.TryOpenMidiFile(out MidiFile midiFile))
                {
                    List<Frame> frames = midiManager.FromMidiFileToFrames(midiFile);
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
                    List<Frame> frames = midiManager.FromMidiFileToFrames(midiFile);
                    frameManager.SetFrames(frames);

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
                MidiFile midiFile = midiManager.FromFramesToMidiFile(frameManager.GetFrames(), 1);

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

        public void ActionOnBpmValueChanged(float bpm)
        {
            midiManager.SetBpm(Mathf.RoundToInt(bpm));
        }
    }
}
