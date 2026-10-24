using System;
using System.Collections.Generic;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.UI.State
{
    public class UIStateManager : MonoBehaviour
    {
        [Header("Order: Device, File, Frame, Transform, Direction, Record, Playback, Create, Select, Clock")]
        [SerializeField] List<CanvasGroup> panels;

        // more panels gated by one of the states above, e.g. Transform mode's Simple / Individual buttons sit apart
        // from the transform column but follow its state
        [Serializable]
        struct GatedPanel
        {
            public UIStates state;
            public CanvasGroup panel;
        }

        [SerializeField] List<GatedPanel> extraPanels = new List<GatedPanel>();

        private void ApplyState(UIStates uiState)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] == null) continue;

                bool isEnabled = ((int)uiState & (1 << i)) != 0;

                panels[i].interactable = isEnabled;
            }

            for (int i = 0; i < extraPanels.Count; i++)
            {
                if (extraPanels[i].panel == null) continue;

                extraPanels[i].panel.interactable = (uiState & extraPanels[i].state) != 0;
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
