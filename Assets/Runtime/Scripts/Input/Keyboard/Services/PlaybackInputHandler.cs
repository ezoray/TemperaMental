using TemperaMental.Midi.Playbacks;
using TemperaMental.UI.Playbacks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.Input.Keyboards
{
    public class PlaybackInputHandler : InputHandlerBase
    {
        [SerializeField] PlaybackEventController playbackEventController;
        [SerializeField] PlaybackUIManager playbackUIManager;

        TemperaMentalInputActions.PlaybackActions playbackActions;

        private void OnPlusBpm(InputAction.CallbackContext ctx) => playbackUIManager.OnClickIncrementBpm();
        private void OnMinusBpm(InputAction.CallbackContext ctx) => playbackUIManager.OnClickDecrementBpm();
        private void OnPlayPosition(InputAction.CallbackContext ctx) => playbackEventController.OnClickPlayPosition();
        private void OnPlay(InputAction.CallbackContext ctx) => playbackEventController.OnClickPlay();
        private void OnPause(InputAction.CallbackContext ctx) => playbackEventController.OnClickPause();
        private void OnStop(InputAction.CallbackContext ctx) => playbackEventController.OnClickStop();
        private void OnLoop(InputAction.CallbackContext ctx) => playbackEventController.OnClickChangeLoopState();

        public override void SetInputActions(TemperaMentalInputActions inputActions)
        {
            base.actionMap = playbackActions = inputActions.Playback;

            playbackActions.PlusBpm.performed += OnPlusBpm;
            playbackActions.MinusBpm.performed += OnMinusBpm;

            playbackActions.PlayPosition.performed += OnPlayPosition;
            playbackActions.Play.performed += OnPlay;
            playbackActions.Pause.performed += OnPause;
            playbackActions.Stop.performed += OnStop;

            playbackActions.Loop.performed += OnLoop;
        }

        private void OnDisable()
        {
            playbackActions.PlusBpm.performed -= OnPlusBpm;
            playbackActions.MinusBpm.performed -= OnMinusBpm;

            playbackActions.PlayPosition.performed -= OnPlayPosition;
            playbackActions.Play.performed -= OnPlay;
            playbackActions.Pause.performed -= OnPause;
            playbackActions.Stop.performed -= OnStop;

            playbackActions.Loop.performed -= OnLoop;

            playbackActions.Disable();
        }
    }
}
