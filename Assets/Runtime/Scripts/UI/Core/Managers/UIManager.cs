using System.Collections.Generic;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.UI.Core
{
    public class UIManager : MonoBehaviour
    {
        [Header("Order: Device, File, Mode, Frame, FrameShift, Playback, Create, Select")]
        [SerializeField] List<CanvasGroup> panels;

        private void ApplyState(UiStateFlags uiState)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                bool isEnabled = ((int)uiState & (1 << i)) != 0;

                panels[i].interactable = isEnabled;
            }
        }

        public void ActionOnPlaybackStateChanged(PlaybackState playbackState)
        {
            switch (playbackState)
            {
                case PlaybackState.Reset:
                    ApplyState(UiStateFlags.All);
                    break;

                case PlaybackState.Playing:
                    ApplyState(UiStateFlags.Playing);
                    break;

                case PlaybackState.Paused:
                    ApplyState(UiStateFlags.Paused);
                    break;

                case PlaybackState.Stopped:
                    ApplyState(UiStateFlags.All);
                    break;
            }
        }

        private void OnDestroy()
        {
            panels.Clear();
        }
    }
}
