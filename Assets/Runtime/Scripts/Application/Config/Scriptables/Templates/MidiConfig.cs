using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "MidiConfig", menuName = "Scriptable Objects/MidiConfig")]
    public class MidiConfig : ScriptableObject
    {
        public string PrimaryDevice = "Tempera";
        public string FilterName = "Midi";
        public string FilterType = "mid";

        public Color DefaultOffColor = new Color(0.55f, 0.55f, 0.55f);
        public Color LoopOnColor = new Color(0.4f, 0.8f, 0.4f);
        public Color ReverseOnColor = new Color(1.0f, 0.5f, 1f);

        public int DefaultBpm = 400;
        public int MinBpm = 10;
        public int MaxBpm = 2000;

        public short TicksPerFrame = 960;
        public int ActivateCC = 10;
        public int PlaceCC = 11;
        public int RemoveCC = 12;
        public byte ClearEmittersValue = 64;

        public string FrameStartPrefix = "START_FRAME_NO_";
        public string SeqEndMarker = "END_SEQ";
    }
}
