using TemperaMental.Frames;
using UnityEngine;

namespace TemperaMental.Recording
{
    public class RecordEventController : MonoBehaviour
    {
        [SerializeField] FrameManager frameManager;

        public void OnClickToggleRecord() => frameManager.ToggleRecording();
    }
}
