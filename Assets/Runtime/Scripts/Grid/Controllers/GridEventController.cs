using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Grid
{
    public class GridEventController : MonoBehaviour
    {
        [SerializeField] GridManager gridManager;

        public void ActionOnAddEmitter(EmitterDetail emitterDetail)
        {
            gridManager.AddTile(emitterDetail);
        }

        public void ActionOnRemoveEmitter(Vector2Int position, int emitterCount)
        {
            gridManager.RemoveTile(position);
        }

        public void ActionOnFrameChanged(FrameDetail frameDetail)
        {
            gridManager.DrawFrame(frameDetail);
        }
    }
}
