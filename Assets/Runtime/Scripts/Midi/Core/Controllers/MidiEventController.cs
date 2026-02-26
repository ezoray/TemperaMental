using System;
using System.Collections.Generic;
using System.IO;
using Melanchall.DryWetMidi.Core;
using SFB;
using Tempera.Mental.Frames;
using Tempera.Mental.Logs;
using Tempera.Mental.Midi.Transforms;
using UnityEngine;

namespace Tempera.Mental.Midi.Core
{
    // todo loading and saving are currently blocking, use their async counterparts
    public class MidiEventController : MonoBehaviour
    {
        [SerializeField] FrameManager frameManager;
        [SerializeField] TransformService midiTransformService;

        ExtensionFilter[] loadFileExtensions = new[] { new ExtensionFilter("Midi","mid", "midi") };
        ExtensionFilter[] saveFileExtensions = new[] { new ExtensionFilter("Midi", "mid") };

        public void OnClickLoadMidiFileAndAppendFrames()
        {
            Debug.Log("OnClickLoadMidiFileAndAppendFrames");

            try
            {
                string[] paths = StandaloneFileBrowser.OpenFilePanel("Open Midi File", "", loadFileExtensions, false);

                if (paths != null && paths.Length > 0)
                {
                    if (!string.IsNullOrEmpty(paths[0]))
                    {
                        MidiFile midiFile = MidiFile.Read(paths[0]);

                        List<Frame> frames = midiTransformService.FromMidiFileToFrames(midiFile);
                        frameManager.AppendFrames(frames);

                        LogMan.Log("File appended: " + paths[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError(ex.Message);
            }
        }

        public void OnClickLoadMidiFileAsFrames()
        {
            Debug.Log("OnClickLoadMidiFileAsFrames");

            try
            {
                string[] paths = StandaloneFileBrowser.OpenFilePanel("Open Midi File", "", loadFileExtensions, false);

                if (paths != null && paths.Length > 0)
                {
                    if (!string.IsNullOrEmpty(paths[0]))
                    {
                        MidiFile midiFile = MidiFile.Read(paths[0]);

                        List<Frame> frames = midiTransformService.FromMidiFileToFrames(midiFile);
                        frameManager.AddFrames(frames);

                        LogMan.Log("File loaded: " + paths[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError(ex.Message);
            }
        }

        public void OnClickSaveFramesAsMidiFile()
        {
            Debug.Log("OnClickSaveFramesAsMidiFile");

            try
            {
                string savePath = StandaloneFileBrowser.SaveFilePanel("Save Midi File", "", "", saveFileExtensions);

                if(!string.IsNullOrEmpty(savePath))
                {
                    MidiFile midiFile = midiTransformService.FromFramesToMidiFile(frameManager.Frames);
                    midiFile.Write(savePath, true);

                    LogMan.Log("File saved: " + savePath);
                }

            }
            catch (Exception ex)
            {
                LogMan.LogError(ex.Message);
            }
        }
        public void OnSliderSetBpmValue(float bpm)
        {
            Debug.Log("OnSliderSetBpmValue: " + bpm);

            midiTransformService.SetBpm((int)bpm);
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
