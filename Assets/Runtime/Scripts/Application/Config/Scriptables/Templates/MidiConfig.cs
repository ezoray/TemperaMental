using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "MidiConfig", menuName = "Scriptable Objects/MidiConfig")]
    public class MidiConfig : ScriptableObject
    {
        public string PrimaryDevice = "Tempera";

        // time between checking for device connection changes under Windows;
        public float PollingInterval = 0.5f;

        // midi file io
        public string FilterName = "Midi";
        public string FilterType = "mid";
        public string AppendTitle = "Append Midi File";
        public string LoadTitle = "Open Midi File";
        public string SaveTitle = "Save Midi File";

        public int DefaultBpm = 128;
        public int MinBpm = 10;
        public int MaxBpm = 1000;

        // tick resolution and interval between ticks
        public short TicksPerFrame = 120;
        public float EventIntervalMS = 0.0005f;

        // cc numbers for emitters
        public int ActivateCC = 10;
        public int PlaceCC = 11;
        public int RemoveCC = 12;

        public byte ClearEmittersValue = 64;

        // markers in midifile
        public string FrameStartPrefix = "START_FRAME_NO_";
        public string SeqEndMarker = "END_SEQ";
    }
}
