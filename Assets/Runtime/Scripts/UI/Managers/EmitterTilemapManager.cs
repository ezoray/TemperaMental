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
            emitterTilemap.SetTile(position, tile);

            emitterTilemap.SetTileFlags(position, TileFlags.None);
            emitterTilemap.SetColor(position, color);
        }
    }
}
