using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tempera.Mental.UI
{
    public class BaseTilemapManager : MonoBehaviour
    {
        [SerializeField] Tilemap baseTilemap;
        [SerializeField] TileBase tile;

    
        public void FillBackground(BoundsInt gridBounds)
        {
            var tiles = new TileBase[8 * 8];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = tile;

            baseTilemap.SetTilesBlock(gridBounds, tiles);
        }

        public Tilemap BaseTilemap { get => baseTilemap; set => baseTilemap = value; }
    }
}
