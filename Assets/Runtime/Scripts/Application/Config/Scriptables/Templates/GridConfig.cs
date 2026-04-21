using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Scriptable Objects/GridConfig")]
    public class GridConfig : ScriptableObject
    {
        public int GridWidth = 8;
        public int GridHeight = 8;

        public Color EmitterBlue = new Color(0f, 0.6f, 1f, 1f);  // lightened for dark background
        public Color EmitterRed = Color.red;
        public Color EmitterYellow = Color.yellow;
        public Color EmitterGreen = Color.green;

        public byte BlueEmitterId = 0;
        public byte RedEmitterId = 1;
        public byte YellowEmitterId = 2;
        public byte GreenEmitterId = 3;
        public int EmitterCount = 4;

        public int DefaultEmitterId = 0;

        public int MaxEmitters = 64;
    }
}
