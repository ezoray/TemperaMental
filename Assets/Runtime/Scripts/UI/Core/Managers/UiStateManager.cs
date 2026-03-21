using System.Collections.Generic;
using Tempera.Mental.Core;
using Tempera.Mental.Core.UI;
using UnityEngine;

namespace Tempera.Mental.UI.Core
{
    public class UiStateManager : MonoBehaviour
    {
        [Header("Order: Device, File, Mode, Playback, Create, Select")]
        [SerializeField] List<CanvasGroup> panels;

        private void ApplyState(UiState uiState)
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
                case PlaybackState.Idle:
                    ApplyState(UiState.All);
                    break;

                case PlaybackState.Playing:
                    ApplyState(UiState.Playback);
                    break;

                case PlaybackState.Paused:
                    ApplyState(UiState.All);
                    break;
            }
        }
    }
}
