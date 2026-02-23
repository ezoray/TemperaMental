using System.Collections.Generic;
using Tempera.Mental.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tempera.Mental.UI
{
    public class EmitterTilemapManager : MonoBehaviour
    {
        [SerializeField] Tilemap emitterTilemap;
        [SerializeField] TileBase tile;

        public void RemoveTile(Vector3Int position)
        {
            emitterTilemap.SetTile(position, null);
        }

        public void AddTiles(List<VisualEmitterDetail> visualDetails)
        {
            foreach (var visualDetail in visualDetails)
            {
                AddTile(visualDetail.Position, visualDetail.Color);
            }
        }

        public void ClearTiles()
        {
            emitterTilemap.ClearAllTiles();
        }

        public void AddTile(Vector3Int position, Color color)
        {
            // 1. Place the tile at the coordinate
            emitterTilemap.SetTile(position, tile);

            // 2. Remove the "Lock Color" flag so we can apply a custom color
            emitterTilemap.SetTileFlags(position, TileFlags.None);

            // 3. Apply your specific emitter color
            emitterTilemap.SetColor(position, color);
        }
    }
}
