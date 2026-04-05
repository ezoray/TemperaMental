using TemperaMental.Applications.Config;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Grid
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] BaseTilemapManager baseTilemapManager;
        [SerializeField] EmitterTilemapManager emitterTilemapManager;

        BoundsInt gridBounds;

        private void OnEnable()
        {
             gridBounds = new BoundsInt(0, 0, 0, ConfigRegistry.Grid.GridWidth, ConfigRegistry.Grid.GridHeight, 1);
        }

        private void Start()
        {
            baseTilemapManager.FillBackground(gridBounds);
        }

        public bool TryGetCellPositionInGrid(Vector3 worldPoint, out Vector3Int cellPosition)
        {
            cellPosition = baseTilemapManager.BaseTilemap.WorldToCell(worldPoint);

            return gridBounds.Contains(cellPosition);
        }

        public void ActionOnFrameChanged(FrameDetail frameDetail)
        {
            emitterTilemapManager.ClearTiles();
            emitterTilemapManager.AddTiles(frameDetail.EmitterGroups);
        }

        public void ActionOnRemoveEmitter(Vector2Int position)
        {
            emitterTilemapManager.RemoveTile(position);
        }


        public void ActionOnAddEmitter(EmitterDetail emitterDetail)
        {
            emitterTilemapManager.AddTile(emitterDetail);
        }
    }
}
