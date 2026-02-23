using System;
using System.Collections.Generic;
using System.IO;
using Melanchall.DryWetMidi.Core;
using Tempera.Mental.Frames;
using Tempera.Mental.Midi.Transforms;
using UnityEngine;

namespace Tempera.Mental.Midi.Core
{
    public class MidiEventController : MonoBehaviour
    {
        [SerializeField] FrameManager frameManager;
        [SerializeField] TransformService midiTransformService;


        public void OnSliderSetBpmValue(float bpm)
        {
            Debug.Log("OnSliderSetBpmValue: " + bpm);

            midiTransformService.SetBpm((int)bpm);
        }

        public void OnClickLoadMidiFileAndAppendFrames()
        {
            Debug.Log("OnClickLoadMidiFileAndAppendFrames: " + Application.persistentDataPath);

            try
            {
                string fullPath = Path.Combine(Application.persistentDataPath, "demo.mid");
                MidiFile midiFile = MidiFile.Read(fullPath);

                List<Frame> frames = midiTransformService.FromMidiFileToFrames(midiFile);

                frameManager.AppendFrames(frames);
            }
            catch (Exception ex)
            {
                Debug.LogError("OnClickLoadMidiFileAndAppendFrames: " + ex);
            }
        }

        public void OnClickLoadMidiFileAsFrames()
        {
            Debug.Log("OnClickLoadMidiFileAsFrames: " + Application.persistentDataPath);

            try
            {
                string fullPath = Path.Combine(Application.persistentDataPath, "demo.mid");
                MidiFile midiFile = MidiFile.Read(fullPath);

                List<Frame> frames = midiTransformService.FromMidiFileToFrames(midiFile);

                frameManager.AddFrames(frames);
            }
            catch(Exception ex)
            {
                Debug.LogError("OnClickLoadMidiFileAsFrames: " + ex);
            }
        }

        public void OnClickSaveFramesAsMidiFile()
        {
            Debug.Log("OnClickSaveFramesAsMidiFile: " + Application.persistentDataPath);
            try
            {
                MidiFile midiFile = midiTransformService.FromFramesToMidiFile(frameManager.Frames);

                string fullPath = Path.Combine(Application.persistentDataPath, "demo.mid");
                midiFile.Write(fullPath,true);
            }
            catch (Exception ex)
            {
                Debug.LogError("OnClickSaveFramesAsMidiFile: " + ex);
            }     
        }

        public void OnClickConvertFramesToMidiFile()
        {
            try
            {
                MidiFile midiFile = midiTransformService.FromFramesToMidiFile(frameManager.Frames);
            }
            catch (Exception ex)
            {
                Debug.LogError("MidiEventController OnClickConvertFramesToMidiFile: " + ex);
            }
        }
    }
}
