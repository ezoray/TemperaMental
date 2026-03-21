using Tempera.Mental.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tempera.Mental.UI.Playbacks
{
    public class PlaybackUiManager : MonoBehaviour
    {
        const int BPM_MIN = 1;
        const int BPM_MAX = 2000;

        [Header("Order: PlayPosition, Play, Pause, Stop")]
        [SerializeField] Button[] controlButtons;

        [SerializeField] Slider bpmSlider;

        public void OnClickIncrementBpm()
        {
            int newBpm = (int)bpmSlider.value +1;

            if (newBpm <= BPM_MAX)
            {
                bpmSlider.value = newBpm;
            }
        }

        public void OnClickDecrementBpm()
        {
            int newBpm = (int)bpmSlider.value -1;

            if (newBpm >= BPM_MIN)
            {
                bpmSlider.value = newBpm;
            }
        }

        public void ActionOnSetBpm(int bpm)
        {
            bpmSlider.value = bpm;
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
