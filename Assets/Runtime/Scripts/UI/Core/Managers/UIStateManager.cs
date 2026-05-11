using System.Collections.Generic;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.UI.Core
{
    public class UIStateManager : MonoBehaviour
    {
        [Header("Order: Device, File, Mode, Frame, Transform, Direction, Record, Playback, Create, Select")]
        [SerializeField] List<CanvasGroup> panels;

        private void ApplyState(UIStates uiState)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] == null) continue;

                bool isEnabled = ((int)uiState & (1 << i)) != 0;

                panels[i].interactable = isEnabled;
            }
        }

        public void ActionOnPlaybackStateChanged(PlaybackState playbackState)
        {
            switch (playbackState)
            {
                case PlaybackState.Reset:
                    ApplyState(UIStates.All);
                    break;

                case PlaybackState.Playing:
                    ApplyState(UIStates.Playing);
                    break;

                case PlaybackState.Paused:
                    ApplyState(UIStates.Paused);
                    break;

                case PlaybackState.Stopped:
                    ApplyState(UIStates.All);
                    break;
            }
        }
    }
}
