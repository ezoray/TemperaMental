using UnityEngine;

namespace Tempera.Mental.Utils
{
    public static class EmitterUtils
    {
        static Color[] colors = { Color.blue, Color.red, Color.yellow, Color.green };


        public static Color GetColor(int emitterId)
        {
            return colors[emitterId];
        }
    }
}
