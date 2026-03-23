using TemperaMental.Applications.Config;
using TemperaMental.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Playbacks
{
    public class PlaybackUiManager : MonoBehaviour
    {
        int minBpm;
        int maxBpm;

        [Header("Order: PlayPosition, Play, Pause, Stop")]
        [SerializeField] Button[] controlButtons;

        [SerializeField] Slider bpmSlider;

        private void Awake()
        {
            minBpm = ConfigRegistry.Midi.MinBpm;
            maxBpm = ConfigRegistry.Midi.MaxBpm;
        }

        public void OnClickIncrementBpm()
        {
            int newBpm = Mathf.RoundToInt(bpmSlider.value) +1;

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
                    ApplyState(PlaybackFlags.Stopped);
                    break;

                case PlaybackState.Playing: 
                    ApplyState(PlaybackFlags.Playing);
                    break;

                case PlaybackState.Paused:
                    ApplyState(PlaybackFlags.Paused);
                    break;
            }
        }

        private void ApplyState(PlaybackFlags playbackFlags)
        {
            for (int i = 0; i < controlButtons.Length; i++)
            {
                bool isEnabled = ((int)playbackFlags & (1 << i)) != 0;

                controlButtons[i].interactable = isEnabled;
            }
        }
    }
}
