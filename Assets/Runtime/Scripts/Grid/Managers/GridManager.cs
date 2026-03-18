using Tempera.Mental.Core;
using UnityEngine;

namespace Tempera.Mental.Grid
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] BaseTilemapManager baseTilemapManager;
        [SerializeField] EmitterTilemapManager emitterTilemapManager;

        BoundsInt gridBounds = new BoundsInt(0, 0, 0, 8, 8, 1);

        private void Start()
        {
            baseTilemapManager.FillBackground(gridBounds);
        }

        public bool TryGetCellPositionInGrid(Vector3 worldPoint, out Vector3Int cellPosition)
        {
            Debug.Log("GridManager TryGetCellPositionInGrid " + worldPoint);

            cellPosition = baseTilemapManager.BaseTilemap.WorldToCell(worldPoint);

            return gridBounds.Contains(cellPosition);
        }

        public void ActionOnChangeFrame(VisualFrameDetail frameDetail)
        {
            emitterTilemapManager.ClearTiles();
            emitterTilemapManager.AddTiles(frameDetail.EmitterDetails);
        }

        public void ActionOnRemoveEmitter(Vector2Int position)
        {
            emitterTilemapManager.RemoveTile(new Vector3Int(position.x, position.y));
        }


        public void ActionOnAddEmitter(Vector2Int position, Color color)
        {
            emitterTilemapManager.AddTile(new Vector3Int(position.x, position.y), color);
        }
    }
}
