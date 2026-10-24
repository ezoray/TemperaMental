using System.Collections.Generic;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.UI.State
{
    public class UIStateManager : MonoBehaviour
    {
        [Header("Order: Device, File, Frame, Transform, Direction, Record, Playback, Create, Select, Clock")]
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

        PlaybackState playbackState;
        RecordState recordState;

        public void ActionOnPlaybackStateChanged(PlaybackState newPlaybackState)
        {
            playbackState = newPlaybackState;
            Refresh();
        }

        public void ActionOnTemperaRecordStateChanged(RecordState newRecordState)
        {
            recordState = newRecordState;
            Refresh();
        }

        private void Refresh()
        {
            if (recordState == RecordState.Recording)
            {
                ApplyState(UIStates.TemperaRecording);
                return;
            }

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
