using TemperaMental.Frames;
using TemperaMental.Midi.Core;
using TemperaMental.Midi.Devices;
using UnityEngine;

namespace TemperaMental.Midi.Playbacks
{
    public class FrameSequencerEventController : MonoBehaviour
    {
        [SerializeField] DeviceManager deviceManager;
        [SerializeField] FrameSequencer frameSequencer;
        [SerializeField] MidiTempoManager midiManager;
        [SerializeField] FrameManager frameManager;

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
