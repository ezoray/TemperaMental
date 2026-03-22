using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TemperaMental.Grid
{
    public class BaseTilemapManager : MonoBehaviour
    {
        [SerializeField] Tilemap baseTilemap;
        [SerializeField] TileBase tile;

    
        public void FillBackground(BoundsInt gridBounds)
        {
            var tiles = new TileBase[gridBounds.xMax * gridBounds.yMax];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = tile;

            baseTilemap.SetTilesBlock(gridBounds, tiles);
        }

        public Tilemap BaseTilemap { get => baseTilemap; set => baseTilemap = value; }
    }
}
