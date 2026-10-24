using UnityEngine;

namespace TemperaMental.Midi.Core
{
    // the Tempera surface's canvas and Hold controls, and what they follow
    public class SoundEventController : MonoBehaviour
    {
        [SerializeField] CanvasManager canvasManager;
        [SerializeField] HoldNoteManager holdNoteManager;

        // -1 or +1 from the Canvas buttons
        public void OnClickCanvas(int directionValue) => canvasManager.StepCanvas(directionValue);

        public void OnClickHold() => holdNoteManager.ToggleHold();

        // a file was loaded (not appended): its canvas, or -1
        public void ActionOnCanvasLoaded(int program) => canvasManager.SetCanvasFromFile(program);

        public void ActionOnMidiChannelChanged(int channel) => holdNoteManager.SetMidiChannel(channel);

        public void ActionOnPlaybackReadyStateChanged(bool isReady) => holdNoteManager.ActionOnPlaybackReadyStateChanged(isReady);
    }
}
