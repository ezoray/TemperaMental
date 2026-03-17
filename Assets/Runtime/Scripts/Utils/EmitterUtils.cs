using UnityEngine;

namespace Tempera.Mental.Utils
{
    public static class EmitterUtils
    {
        static Color[] colors = { new Color(0, 0.6f, 1f), Color.red, Color.yellow, Color.green };

        public static Color GetColor(int emitterId)
        {
            return colors[emitterId];
        }
    }
}
