using System;
using Melanchall.DryWetMidi.Core;
using SFB;
using TemperaMental.Applications.Config;
using TemperaMental.Logs;
using UnityEngine;

namespace TemperaMental.Midi.IO
{
    public class MidiFileService : MonoBehaviour
    {
        ExtensionFilter[] fileExtensions;


        private void OnEnable()
        {
            fileExtensions = new ExtensionFilter[] { new ExtensionFilter(ConfigRegistry.Midi.FilterName, ConfigRegistry.Midi.FilterType) };
        }

        public bool TrySaveMidiFile(MidiFile midiFile)
        {
            try
            {
                string savePath = StandaloneFileBrowser.SaveFilePanel("Save Midi File", "", "", fileExtensions);

                if (string.IsNullOrEmpty(savePath))
                {
                    return false;
                }

                midiFile.Write(savePath, true);
                return true;         

            }
            catch (Exception ex)
            {
                LogMan.LogError($"Failed to save midi file: {ex}");
                return false;
            }
        }

        public bool TryOpenMidiFile(out MidiFile midiFile)
        {
            midiFile = null;

            try
            {
                string[] paths = StandaloneFileBrowser.OpenFilePanel("Open Midi File", "", fileExtensions, false);

                if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                {
                    return false;
                }

                midiFile = MidiFile.Read(paths[0]);
                return true;
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Failed to load midi file: {ex}");
                return false;
            }
        }
    }
}
