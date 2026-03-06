using System;
using Melanchall.DryWetMidi.Core;
using SFB;
using Tempera.Mental.Logs;
using UnityEngine;

namespace Tempera.Mental.Midi.IO
{
    public class MidiFileService : MonoBehaviour
    {
        ExtensionFilter[] loadFileExtensions = new[] { new ExtensionFilter("Midi", "mid") };
        ExtensionFilter[] saveFileExtensions = new[] { new ExtensionFilter("Midi", "mid") };


        public bool TrySaveMidiFile(MidiFile midiFile)
        {
            try
            {
                string savePath = StandaloneFileBrowser.SaveFilePanel("Save Midi File", "", "", saveFileExtensions);

                if (string.IsNullOrEmpty(savePath))
                {
                    return false;
                }

                midiFile.Write(savePath, true);
                return true;         

            }
            catch (Exception ex)
            {
                LogMan.LogError($"Failed to save MIDI: {ex}");
                return false;
            }
        }

        public bool TryOpenMidiFile(out MidiFile midiFile)
        {
            midiFile = null;

            try
            {
                string[] paths = StandaloneFileBrowser.OpenFilePanel("Open Midi File", "", loadFileExtensions, false);

                if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                {
                    return false;
                }

                midiFile = MidiFile.Read(paths[0]);
                return true;
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Failed to load MIDI: {ex}");
                return false;
            }
        }
    }
}
