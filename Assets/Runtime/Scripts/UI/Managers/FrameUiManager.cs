using Tempera.Mental.Core;
using Tempera.Mental.Logs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tempera.Mental.UI
{
    public class FrameUiManager : MonoBehaviour
    {
        [SerializeField] BaseTilemapManager baseTilemapManager;
        [SerializeField] EmitterTilemapManager emitterTilemapManager;
        [SerializeField] TextMeshProUGUI frameText;
        [SerializeField] Slider frameSlider;

        // background highlight image for selected colour
        [SerializeField] RectTransform selectionRing;

        BoundsInt gridBounds = new BoundsInt(0, 0, 0, 8, 8, 1);

        [SerializeField] UnityEvent<int> onFrameNumberChanged;

        private void Start()
        {
            baseTilemapManager.FillBackground(gridBounds);
        }

        public bool TryGetCellPositionInGrid(Vector3 worldPoint, out Vector3Int cellPosition)
        {
       //     Debug.Log("GridManager TryGetCellPositionInGrid " + worldPoint);

            cellPosition = baseTilemapManager.BaseTilemap.WorldToCell(worldPoint);

            return gridBounds.Contains(cellPosition);
        }

        // move highlight image to behind selected colour button
        public void OnClickSelectColor(Button button)
        {
            RectTransform buttonRect = button.GetComponent<RectTransform>();

            selectionRing.position = buttonRect.position;
        }

        // frame slider
        public void OnFrameNumberChanged(float frameNumber)
        {
            onFrameNumberChanged?.Invoke((int)frameNumber);
        }

        public void ActionOnChangeFrame(VisualFrameDetail frameDetail)
        {
            emitterTilemapManager.ClearTiles();
            emitterTilemapManager.AddTiles(frameDetail.EmitterDetails);

            frameSlider.maxValue = frameDetail.FrameTotal;
            frameSlider.value = frameDetail.FrameNumber;

            frameText.text = $"{frameDetail.FrameNumber} / {frameDetail.FrameTotal}";
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
