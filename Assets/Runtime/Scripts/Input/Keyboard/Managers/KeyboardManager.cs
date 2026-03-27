using System.Collections.Generic;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Input.Keyboards
{
    public class KeyboardManager : MonoBehaviour
    {
        [Header("Order: Emitter, File, Mode, Playback, Create, Select")]
        [SerializeField] List<InputHandlerBase> inputHandlers;


        public void InitActions(TemperaMentalInputActions inputActions)
        {
            foreach (var inputHandler in inputHandlers)
            {
                inputHandler.SetInputActions(inputActions);
                inputHandler.SetEnabled(true);
            }
        }

        private void ApplyState(KeyboardStateFlags stateFlags)
        {
            for (int i = 0; i < inputHandlers.Count; i++)
            {
                bool isEnabled = ((int)stateFlags & (1 << i)) != 0;

                inputHandlers[i].SetEnabled(isEnabled);
            }
        }

        public void ActionOnPlaybackStateChanged(PlaybackState playbackState)
        {
            switch (playbackState)
            {
                case PlaybackState.Idle:
                    ApplyState(KeyboardStateFlags.All);
                    break;

                case PlaybackState.Playing:
                    ApplyState(KeyboardStateFlags.Playing);
                    break;

                case PlaybackState.Paused:
                    ApplyState(KeyboardStateFlags.Playback);
                    break;
            }
        }
    }    
}
