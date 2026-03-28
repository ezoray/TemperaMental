using TemperaMental.Midi.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.Input
{
    public class FileInputHandler : InputHandlerBase
    {
        [SerializeField] FileEventController fileEventController;

        TemperaMentalInputActions.FileActions fileActions;

        private void OnAppend(InputAction.CallbackContext ctx) => fileEventController.OnClickAppendMidiFileAsFrames();
        private void OnLoad(InputAction.CallbackContext ctx) => fileEventController.OnClickLoadMidiFileAsFrames();
        private void OnSave(InputAction.CallbackContext ctx) => fileEventController.OnClickSaveFramesAsMidiFile();

        public override void SetInputActions(TemperaMentalInputActions inputActions)
        {
            base.actionMap = fileActions = inputActions.File;

            fileActions.Append.performed += OnAppend;
            fileActions.Load.performed += OnLoad;
            fileActions.Save.performed += OnSave;
        }

        private void OnDisable()
        {
            fileActions.Append.performed -= OnAppend;
            fileActions.Load.performed -= OnLoad;
            fileActions.Save.performed -= OnSave;

            fileActions.Disable();
        }
    }
}
