namespace TemperaMental.Input.Keyboards
{
    public class ModeInputHandler : InputHandlerBase
    {

        TemperaMentalInputActions.ModeActions modeActions;


        public override void SetInputActions(TemperaMentalInputActions inputActions)
        {
            base.actionMap = modeActions = inputActions.Mode;

            modeActions.Enable();
        }

        private void OnDisable()
        {
   
            modeActions.Disable();
        }
    }
}
