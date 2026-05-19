using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TemperaMental.Grid
{
    public class EmitterTilemapManager : MonoBehaviour
    {
        [SerializeField] Tilemap emitterTilemap;
        [SerializeField] TileBase tile;

        Color[] emitterColors;
        int gridSize;

        private void Awake()
        {
            emitterColors = new Color[]
            {
                ConfigRegistry.Grid.EmitterBlue,
                ConfigRegistry.Grid.EmitterRed,
                ConfigRegistry.Grid.EmitterYellow,
                ConfigRegistry.Grid.EmitterGreen
            };

            gridSize = ConfigRegistry.Grid.GridWidth * ConfigRegistry.Grid.GridHeight;
        }

        public void ClearTiles()
        {
            emitterTilemap.ClearAllTiles();
        }

        public void RemoveTile(Vector2Int position)
        {
            emitterTilemap.SetTile(new Vector3Int(position.x, position.y), null);
        }

        public void AddTile(EmitterDetail emitterDetail)
        {
            SetTile(emitterDetail.Position, emitterDetail.EmitterId);
        }

        // draw all emitters from ulong groups
        public void AddTiles(ulong[] emitterGroups)
        {
            for (byte emitterId = 0; emitterId < emitterGroups.Length; emitterId++)
            {
                ulong group = emitterGroups[emitterId];
                if (group == 0) continue;
                for (byte pos = 0; pos < gridSize; pos++)
                {
                    if ((group & (1UL << pos)) != 0)
                    {
                        SetTile(EmitterUtils.IndexToPosition(pos), emitterId);
                    }
                }
            }
        }

        private void SetTile(Vector2Int position, int emitterId)
        {
            Vector3Int tilePosition = new Vector3Int(position.x, position.y);
            emitterTilemap.SetTile(tilePosition, tile);
            emitterTilemap.SetTileFlags(tilePosition, TileFlags.None);
            emitterTilemap.SetColor(tilePosition, emitterColors[emitterId]);
        }
    }
}