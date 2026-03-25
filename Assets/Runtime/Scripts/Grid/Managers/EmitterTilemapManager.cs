using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TemperaMental.Grid
{
    public class EmitterTilemapManager : MonoBehaviour
    {
        [SerializeField] Tilemap emitterTilemap;
        [SerializeField] TileBase tile;

        List<Color> emitterColors;

        private void Awake()
        {
            emitterColors = new List<Color>
            {
                ConfigRegistry.Grid.EmitterBlue,
                ConfigRegistry.Grid.EmitterRed,
                ConfigRegistry.Grid.EmitterYellow,
                ConfigRegistry.Grid.EmitterGreen
            };
        }

        public void ClearTiles()
        {
            emitterTilemap.ClearAllTiles();
        }

        public void RemoveTile(Vector3Int position)
        {
            emitterTilemap.SetTile(position, null);
        }

        public void AddTile(EmitterDetail emitterDetail)
        {
            Vector3Int position = new Vector3Int(emitterDetail.Position.x, emitterDetail.Position.y);

            emitterTilemap.SetTile(position, tile);

            emitterTilemap.SetTileFlags(position, TileFlags.None);
            emitterTilemap.SetColor(position, emitterColors[emitterDetail.EmitterId]);
        }

        public void AddTiles(List<EmitterDetail> emitterDetails)
        {
            foreach (var emitterDetail in emitterDetails)
            {
                AddTile(emitterDetail);
            }
        }
    }
}
