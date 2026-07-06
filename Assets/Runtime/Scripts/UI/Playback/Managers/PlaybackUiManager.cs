using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Playbacks
{
    public class PlaybackUIManager : MonoBehaviour
    {
        [SerializeField] CanvasGroup transportCanvasGroup;
        [SerializeField] Image playPauseButtonImage;
        [SerializeField] Image loopButtonImage;
        [SerializeField] Image reverseButtonImage;

        [Header("Order: Play, Stop")]
        [SerializeField] Button[] controlButtons;
        [SerializeField] TextMeshProUGUI playText;

        [SerializeField] Slider bpmSlider;

        string play;
        string pause;

        Color defaultOffColor;
        Color playOnColor;
        Color pauseOnColor;
        Color loopOnColor;
        Color reverseOnColor;

        int minBpm;
        int maxBpm;
        

        private void Awake()
        {
            play = ConfigRegistry.UI.Play;
            pause = ConfigRegistry.UI.Pause;

            defaultOffColor = ConfigRegistry.UI.DefaultColor;
            playOnColor = ConfigRegistry.UI.GreenColor;
            pauseOnColor = ConfigRegistry.UI.OrangeColor;
            loopOnColor = ConfigRegistry.UI.CyanColor;
            reverseOnColor = ConfigRegistry.UI.PurpleColor;

            minBpm = ConfigRegistry.Midi.MinBpm;

            bpmSlider.minValue = minBpm;
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

        public void ActionOnMaxBpmChanged(int maxBpm)
        {
            this.maxBpm = maxBpm;

            bpmSlider.maxValue = maxBpm;
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
                case PlaybackState.Reset:
                    playText.text = play;
                    playPauseButtonImage.color = defaultOffColor;
                    ApplyTransportState(PlaybackUIStates.Reset);
                    break;

                case PlaybackState.Playing:
                    playText.text = pause;
                    playPauseButtonImage.color = playOnColor;
                    ApplyTransportState(PlaybackUIStates.Playing);
                    break;

                case PlaybackState.Paused:
                    playText.text = play;
                    playPauseButtonImage.color = pauseOnColor;
                    ApplyTransportState(PlaybackUIStates.Paused);
                    break;

                case PlaybackState.Stopped:
                    playText.text = play;
                    playPauseButtonImage.color = defaultOffColor;
                    ApplyTransportState(PlaybackUIStates.Stopped);
                    break;
            }
        }

        public void ActionOnPlaybackReadyStateChanged(bool isReady)
        {
            transportCanvasGroup.interactable = isReady;
        }

        private void ApplyTransportState(PlaybackUIStates playbackFlags)
        {
            for (int i = 0; i < controlButtons.Length; i++)
            {
                bool isEnabled = ((int)playbackFlags & (1 << i)) != 0;

                controlButtons[i].interactable = isEnabled;
            }
        }
    }
}
