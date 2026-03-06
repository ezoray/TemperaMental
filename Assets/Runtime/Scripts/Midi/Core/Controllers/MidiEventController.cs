using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Tempera.Mental.Frames;
using Tempera.Mental.Logs;
using Tempera.Mental.Midi.IO;
using Tempera.Mental.Midi.Transforms;
using Tempera.Mental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Tempera.Mental.Midi.Core
{
    // hack loading and saving are currently blocking, use their async counterparts if need be
    public class MidiEventController : MonoBehaviour
    {
        [SerializeField] MidiFileService midiFileService;
        [SerializeField] FrameManager frameManager;
        [SerializeField] TransformService transformService;

        [SerializeField] UnityEvent<int> onSetBpm;

        public void OnClickAppendMidiFileAsFrames()
        {
            LogMan.Log("OnClickAppendMidiFileAsFrames");

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
            LogMan.Log("OnClickLoadMidiFileAsFrames");

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
            LogMan.Log("OnClickSaveFramesAsMidiFile");

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

        public void ActionOnBpmValueChange(int bpm)
        {
            Debug.Log("OnBpmValueChanged: " + bpm);

            transformService.SetBpm(bpm);
        }

        public void OnClickConvertFramesToMidiFile()
        {
            try
            {
                MidiFile midiFile = transformService.FromFramesToMidiFile(frameManager.GetFrames());
            }
            catch (Exception ex)
            {
                Debug.LogError("MidiEventController OnClickConvertFramesToMidiFile: " + ex);
            }
        }
    }
}
