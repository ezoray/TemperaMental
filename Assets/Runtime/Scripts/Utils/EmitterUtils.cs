using UnityEngine;

namespace Tempera.Mental.Utils
{
    public static class EmitterUtils
    {
        // blue is lighter as it doesn't show up well against a dark background
        static Color blue = new Color(0, 0.6f, 1f);

        static Color[] colors = { blue, Color.red, Color.yellow, Color.green };

        public static Color GetColor(int emitterId)
        {
            return colors[emitterId];
        }
    }
}
