using TemperaMental.Frames;
using UnityEngine;

namespace TemperaMental.Midi.Playbacks
{
    public class FrameSequenceEventController : MonoBehaviour
    {
        [SerializeField] FrameSequenceManager frameSequencer;
        [SerializeField] FrameManager frameManager;

        public void ActionOnPlaybackReadyStateChanged(bool isReady)
        {
            if (!isReady)
            {
                frameSequencer.Stop();
            }
        }

        // frame slider
        public void ActionOnSelectedFrameChanged(float selectedFrame) => frameSequencer.SeekToFrame(Mathf.RoundToInt(selectedFrame));

        public void ActionOnBpmChanged(int newBpm) => frameSequencer.SetBpm(newBpm);

        public void OnClickToggleReverse() => frameSequencer.ToggleReverse();

        public void OnClickChangeLoopState() => frameSequencer.ToggleLooping();

        public void OnClickStop() => frameSequencer.Stop();

        public void OnClickTogglePlayPause()
        {
            int initialFrame = frameManager.GetCurrentFrameNumber();
            frameSequencer.TogglePlayPause(initialFrame);
        }
    }
}
