using System.Collections.Generic;
using TemperaMental.Applications.Config;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TemperaMental.Grid
{
    public class BaseTilemapManager : MonoBehaviour
    {
        const int ColumnsPerLane = 2;

        [SerializeField] Tilemap baseTilemap;
        [SerializeField] TileBase tile;

        int gridWith, gridHeight;
        BoundsInt gridBounds;

        Color defaultColor;
        Dictionary<int, Color> columnColours;


        private void Awake()
        {
            gridWith = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;

            defaultColor = ConfigRegistry.Grid.DefaultBgTileColor;
            columnColours = new Dictionary<int, Color>
            {
                { 0, ConfigRegistry.Grid.BgTileBlue },
                { 1, ConfigRegistry.Grid.BgTileRed },
                { 2, ConfigRegistry.Grid.BgTileYellow },
                { 3, ConfigRegistry.Grid.BgTileGreen }
            };
        }

        private void Start()
        {
            gridBounds = new BoundsInt(0, 0, 0, gridWith, gridHeight, 1);

            FillBackground(gridBounds);
        }

        public void SetTileBlockColor(int blockId, bool isSet)
        {
            Color tileColor = isSet ? columnColours[blockId] : defaultColor;
            int startColumn = blockId * ColumnsPerLane;

            for (int col = startColumn; col < startColumn + ColumnsPerLane; col++)
                for (int row = 0; row < gridHeight; row++)
                {
                    var pos = new Vector3Int(col, row, 0);
                    baseTilemap.SetTileFlags(pos, TileFlags.None);
                    baseTilemap.SetColor(pos, tileColor);
                }
        }

        private void FillBackground(BoundsInt gridBounds)
        {
            var tiles = new TileBase[gridBounds.xMax * gridBounds.yMax];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = tile;

            baseTilemap.SetTilesBlock(gridBounds, tiles);
        }

        public Tilemap BaseTilemap { get => baseTilemap; set => baseTilemap = value; }
    }
}
