using TemperaMental.Grid;
using UnityEngine;

namespace TemperaMental.Frames
{
    public class FrameEventController : MonoBehaviour
    {
        [SerializeField] Camera mainCamera;
        [SerializeField] GridManager gridManager;
        [SerializeField] FrameManager frameManager;


        public void OnClickEndFrame() => frameManager.GoToEndFrame();

        public void OnClickStartFrame() => frameManager.GoToStartFrame();

        public void OnClickDeleteFrame() => frameManager.DeleteFrame();

        public void OnClickDeleteAllFrames() => frameManager.DeleteAllFrames();

        public void OnClickClearFrame() => frameManager.ClearFrame();

        public void OnClickDuplicateFrame() => frameManager.DuplicateFrame();

        public void OnClickPasteFrame() => frameManager.PasteOntoFrame();

        public void OnClickCopyFrame() => frameManager.CopyFrame();

        public void OnClickNextFrame() => frameManager.GoToNextFrame();

        public void OnClickPreviousFrame() => frameManager.GoToPreviousFrame();

        public void OnClickChangeEmitter(int emitterId) => frameManager.SetEmitter(emitterId);

        public void OnClickNewFrame() => frameManager.InsertFrame();

        // frame slider
        public void ActionOnSelectedFrameChanged(float selectedFrame) => frameManager.GoToFrame((int)selectedFrame);

        public void ActionOnPlayingFrameChanged(int newFrame) => frameManager.GoToFrame(newFrame);

        public void OnMouseLeftClick(Vector2 mousePosition)
        {
            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mousePosition);

            if(gridManager.TryGetCellPositionInGrid(worldPoint, out var cellPosition))
            {
                frameManager.AddEmitterAtPosition(new Vector2Int(cellPosition.x, cellPosition.y));
            }
        }

        public void OnMouseRightClick(Vector2 mousePosition)
        {
            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mousePosition);

            if (gridManager.TryGetCellPositionInGrid(worldPoint, out var cellPosition))
            {
                frameManager.RemoveEmitterAtPosition(new Vector2Int(cellPosition.x, cellPosition.y));
            }
        }
    }
}
