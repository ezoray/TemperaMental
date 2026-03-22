using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "MidiConfig", menuName = "Scriptable Objects/MidiConfig")]
    public class MidiConfig : ScriptableObject
    {
        public string PrimaryDevice = "Tempera";
        public string FilterName = "Midi";
        public string FilterType = "mid";

        public int Bpm = 400;
        public int MinBpm = 40;
        public int MaxBpm = 2000;

        public short TicksPerFrame = 480;
        public int ActivateCC = 10;
        public int PlaceCC = 11;
        public int RemoveCC = 12;

        public string FrameNumberPrefix = "FRAME_NO_";
    }
}
