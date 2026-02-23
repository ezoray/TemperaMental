using System.Collections.Generic;
using Tempera.Mental.Core;
using UnityEngine;

namespace Tempera.Mental.UI
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

        public void ActionOnChangeFrame(List<VisualEmitterDetail> visualDetail)
        {
            emitterTilemapManager.ClearTiles();
            emitterTilemapManager.AddTiles(visualDetail);
        }

        public void ActionOnRemoveEmitter(Vector3Int position)
        {
            emitterTilemapManager.RemoveTile(position);
        }


        public void ActionOnAddEmitter(Vector3Int position, Color color)
        {
            emitterTilemapManager.AddTile(position, color);
        }
    }
}
