using UnityEngine;
using UnityEngine.Tilemaps;

namespace TemperaMental.UI
{
    [CreateAssetMenu(fileName = "Emitter", menuName = "Tempera/EmitterDefinition")]
    public class EmitterDefinition : ScriptableObject
    {
        public string emitterName;       // Optional descriptive name
        public TileBase tile;            // The tile used for the tilemap
        public Color color = Color.white; // Default tint
        public int midiNote;             // Optional, for MIDI mapping later
    }
}
