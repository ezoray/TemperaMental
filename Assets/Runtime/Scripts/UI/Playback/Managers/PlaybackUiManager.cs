using Tempera.Mental.Core;
using Tempera.Mental.Logs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tempera.Mental.Ui.Playbacks
{
    public class PlaybackUiManager : MonoBehaviour
    {
        const int BPM_MIN = 1;
        const int BPM_MAX = 2000;

        [Header("Order: PlayPosition, Play, Pause, Stop")]
        [SerializeField] Button[] controlButtons;

        [SerializeField] TextMeshProUGUI bpmValue;
        [SerializeField] Slider bpmSlider;

        [SerializeField] UnityEvent<int> onBpmValueChanged;
        [SerializeField] UnityEvent<PlaybackEventType> onPlaybackEvent;


        public void OnClickPlayPosition()
        {
            onPlaybackEvent?.Invoke(PlaybackEventType.PlayPosition);
        }

        public void OnClickStop()
        {
            onPlaybackEvent?.Invoke(PlaybackEventType.Stop);
        }

        public void OnClickPause()
        {
            onPlaybackEvent?.Invoke(PlaybackEventType.Pause);
        }

        public void OnClickPlay()
        {
            onPlaybackEvent?.Invoke(PlaybackEventType.Play);
        }

        public void SetPlaybackUiState(PlaybackFlags playbackFlags)
        {
            LogMan.Log("PlaybackFlags: " + playbackFlags);

            for (int i = 0; i < controlButtons.Length; i++)
            {
                if (controlButtons[i] == null) continue;

                int bit = 1 << i;

                bool isEnabled = ((int)playbackFlags & bit) != 0;
                controlButtons[i].interactable = isEnabled;
            }
        }

        public void OnClickBpmPlus()
        {
            int newBpm = (int)bpmSlider.value +1;

            if (newBpm <= BPM_MAX)
            {
                bpmSlider.value = newBpm;
                bpmValue.text = newBpm.ToString();
            }
        }

        public void OnClickBpmMinus()
        {
            int newBpm = (int)bpmSlider.value -1;

            if (newBpm >= BPM_MIN)
            {
                bpmSlider.value = newBpm;
                bpmValue.text = newBpm.ToString();
            }
        }

        public void OnBpmSliderValueChanged(float bpm)
        {
            bpmValue.text = bpm.ToString();

            onBpmValueChanged?.Invoke((int)bpm);
        }

        public void ActionOnSetBPM(int bpm)
        {
            bpmValue.text = bpm.ToString();
            bpmSlider.value = bpm;
        }
    }
}
