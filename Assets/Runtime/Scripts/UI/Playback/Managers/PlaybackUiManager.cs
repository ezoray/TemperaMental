using TemperaMental.Applications.Config;
using TemperaMental.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Playbacks
{
    public class PlaybackUIManager : MonoBehaviour
    {
        [SerializeField] Image loopButtonImage;
        [SerializeField] Image reverseButtonImage;

        Color defaultOffColor;
        Color loopOnColor;
        Color reverseOnColor;

        int minBpm;
        int maxBpm;

        [Header("Order: Play, Pause, Stop")]
        [SerializeField] Button[] controlButtons;

        [SerializeField] Slider bpmSlider;

        private void Awake()
        {
            defaultOffColor = ConfigRegistry.UI.DefaultOffColor;
            loopOnColor = ConfigRegistry.UI.LoopOnColor;
            reverseOnColor = ConfigRegistry.UI.ReverseOnColor;

            minBpm = ConfigRegistry.Midi.MinBpm;
            maxBpm = ConfigRegistry.Midi.MaxBpm;

            bpmSlider.minValue = minBpm;
            bpmSlider.maxValue = maxBpm;
        }

        public void OnClickIncrementBpm()
        {
            int newBpm = Mathf.RoundToInt(bpmSlider.value) + 1;

            if (newBpm <= maxBpm)
            {
                bpmSlider.value = newBpm;
            }
        }

        public void OnClickDecrementBpm()
        {
            int newBpm = Mathf.RoundToInt(bpmSlider.value) - 1;

            if (newBpm >= minBpm)
            {
                bpmSlider.value = newBpm;
            }
        }

        public void ActionReverseStateChanged(bool isOn)
        {
            reverseButtonImage.color = isOn ? reverseOnColor : defaultOffColor;
        }

        public void ActionLoopStateChanged(bool isOn)
        {
            loopButtonImage.color = isOn ? loopOnColor : defaultOffColor;
        }

        public void ActionOnBpmChanged(int newBpm)
        {
            if (Mathf.RoundToInt(bpmSlider.value) == newBpm) return;

            bpmSlider.value = newBpm;
        }

        public void ActionOnPlaybackStateChanged(PlaybackState playbackState)
        {
            switch (playbackState)
            {
                case PlaybackState.Idle:
                    ApplyState(PlaybackUIFlags.Idle);
                    break;

                case PlaybackState.Playing: 
                    ApplyState(PlaybackUIFlags.Playing);
                    break;

                case PlaybackState.Paused:
                    ApplyState(PlaybackUIFlags.Paused);
                    break;

                case PlaybackState.Stopped:
                    ApplyState(PlaybackUIFlags.Stopped);
                    break;
            }
        }

        private void ApplyState(PlaybackUIFlags playbackFlags)
        {
            for (int i = 0; i < controlButtons.Length; i++)
            {
                bool isEnabled = ((int)playbackFlags & (1 << i)) != 0;

                controlButtons[i].interactable = isEnabled;
            }
        }
    }
}
