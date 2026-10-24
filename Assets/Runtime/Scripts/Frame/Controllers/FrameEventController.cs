using System.Collections.Generic;
using TemperaMental.Core;
using TemperaMental.Grid;
using TemperaMental.Logs;
using TemperaMental.Midi.Input;
using TemperaMental.Utils;
using UnityEngine;

namespace TemperaMental.Frames
{
    public class FrameEventController : MonoBehaviour
    {
        [SerializeField] Camera mainCamera;
        [SerializeField] GridManager gridManager;
        [SerializeField] FrameManager frameManager;

        public void OnClickDeleteFrame() => frameManager.DeleteFrame();

        public void OnClickDeleteAllFrames() => frameManager.DeleteAllFrames();

        public void OnClickClearFrame() => frameManager.ClearFrame();

        public void OnClickDuplicateFrame() => frameManager.DuplicateFrame();

        public void OnClickPasteFrame() => frameManager.PasteOntoFrame();

        public void OnClickCopyFrame() => frameManager.CopyFrame();      

        public void OnClickChangeEmitter(int emitterId) => frameManager.SetEmitterType(emitterId);

        public void OnClickNewFrame() => frameManager.InsertFrame();

        public void ActionOnFramesLoaded(List<Frame> frames, bool isAppend)
        {
            if (isAppend)
            {
                frameManager.AppendFrames(frames);
            }
            else
            {
                frameManager.SetFrames(frames);
            }
        }

        public void ActionOnEmittersTransformed(FrameSnapshot result) => frameManager.ReceiveTransformResult(result);

        // Follow Tempera
        public void ActionOnFollowTemperaChanged(bool isFollowing) => frameManager.SetFollowTempera(isFollowing);

        public void ActionOnTemperaControlEvent(TemperaControlEvent controlEvent)
        {
            switch (controlEvent.Kind)
            {
                case TemperaControlKind.SelectEmitter:
                    frameManager.SetEmitterTypeFromTempera(controlEvent.EmitterId);
                    break;

                case TemperaControlKind.PlaceEmitter:
                    frameManager.AddEmitterFromTempera(EmitterUtils.IndexToPosition((byte)controlEvent.Cell), controlEvent.EmitterId);
                    break;

                case TemperaControlKind.RemoveEmitter:
                    frameManager.RemoveEmitterFromTempera(EmitterUtils.IndexToPosition((byte)controlEvent.Cell));
                    break;
            }
        }

        // frame slider
        public void ActionOnSelectedFrameChanged(float selectedFrame) => frameManager.GoToSelectedFrame(Mathf.RoundToInt(selectedFrame));

        public void ActionOnPlaybackStateChanged(PlaybackState playbackState) => frameManager.SetPlaybackState(playbackState);

        public void ActionOnPlaybackFrameChanged(int playbackFrame) => frameManager.GoToPlaybackFrame(playbackFrame);

        public void OnMouseLeftClick(Vector2 mousePosition)
        {
            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mousePosition);

            if(gridManager.TryGetCellPositionInGrid(worldPoint, out var cellPosition))
            {
                frameManager.AddEmitter(new Vector2Int(cellPosition.x, cellPosition.y));
            }
        }

        public void OnMouseRightClick(Vector2 mousePosition)
        {
            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mousePosition);

            if (gridManager.TryGetCellPositionInGrid(worldPoint, out var cellPosition))
            {
                frameManager.RemoveEmitter(new Vector2Int(cellPosition.x, cellPosition.y));
            }
        }
    }
}
