using System.Collections.Generic;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.UI.Core
{
    public class UIManager : MonoBehaviour
    {
        [Header("Order: Device, File, Mode, Frame, Transform, Direction, Playback, Create, Select")]
        [SerializeField] List<CanvasGroup> panels;

        private void ApplyState(UIStateFlags uiState)
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
                    ApplyState(UIStateFlags.All);
                    break;

                case PlaybackState.Playing:
                    ApplyState(UIStateFlags.Playing);
                    break;

                case PlaybackState.Paused:
                    ApplyState(UIStateFlags.Paused);
                    break;

                case PlaybackState.Stopped:
                    ApplyState(UIStateFlags.All);
                    break;
            }
        }

        private void OnDestroy()
        {
            panels.Clear();
        }
    }
}
