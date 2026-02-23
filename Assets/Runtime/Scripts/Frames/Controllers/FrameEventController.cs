using Tempera.Mental.UI;
using UnityEngine;

namespace Tempera.Mental.Frames
{
    public class FrameEventController : MonoBehaviour
    {
        [SerializeField] Camera mainCamera;
        [SerializeField] GridManager gridManager;
        [SerializeField] FrameManager frameManager;


        void Start()
        {
            Debug.Log("Start");
        }

        public void OnClickDeleteFrame()
        {
            frameManager.DeleteFrame();
        }

        public void OnCLickClearAllFrames()
        {
            frameManager.ClearAllFrames();
        }

        public void OnClickDuplicateFrame()
        {
            frameManager.DuplicateFrame();
        }

        public void OnClickNextFrame()
        {
            frameManager.GoToNextFrame();
        }

        public void OnClickPreviousFrame()
        {
            frameManager.GoToPreviousFrame();
        }

        public void OnClickChangeEmitter(int emitterId)
        {
            frameManager.SetEmitter(emitterId);
        }

        public void OnClickNewFrame()
        {
            frameManager.AddFrame();
        }

        public void ActionOnFrameChanged(int newIndex)
        {
            frameManager.GoToFrame(newIndex);
        }

        public void OnMouseLeftClick(Vector2 mousePosition)
        {
            Debug.Log("MatrixEventController OnMouseLeftClick " + mousePosition);

            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mousePosition);

            if(gridManager.TryGetCellPositionInGrid(worldPoint, out var cellPosition))
            {
                frameManager.AddEmitterAtPosition(cellPosition);
            }
        }

        public void OnMouseRightClick(Vector2 mousePosition)
        {
            Debug.Log("MatrixEventController OnMouseRightClick " + mousePosition);

            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mousePosition);

            if (gridManager.TryGetCellPositionInGrid(worldPoint, out var cellPosition))
            {
                frameManager.RemoveEmitterFromCell(cellPosition);
            }
        }
    }
}
