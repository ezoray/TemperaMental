using TemperaMental.Frames;
using UnityEngine;

namespace TemperaMental.Midi.Playbacks
{
    public class FrameSequenceEventController : MonoBehaviour
    {
        [SerializeField] FrameSequenceManager sequenceManager;
        [SerializeField] FrameManager frameManager;

        public void ActionOnPlaybackReadyStateChanged(bool isReady)
        {
            if (!isReady)
            {
                sequenceManager.Stop();
            }
        }

        // frame slider
        public void ActionOnSelectedFrameChanged(float selectedFrame) => sequenceManager.SeekToFrame(Mathf.RoundToInt(selectedFrame));

        public void ActionOnBpmChanged(int newBpm) => sequenceManager.SetBpm(newBpm);

        public void OnClickToggleReverse() => sequenceManager.ToggleReverse();

        public void OnClickChangeLoopState() => sequenceManager.ToggleLooping();

        public void OnClickStop() => sequenceManager.Stop();

        public void OnClickTogglePlayPause()
        {
            int initialFrame = frameManager.GetCurrentFrameNumber();
            sequenceManager.TogglePlayPause(initialFrame);
        }
    }
}
