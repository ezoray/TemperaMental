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

        public override void SetEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                modeActions.Enable();
            }   
            else
            {
                modeActions.Disable();
            }
        }

        private void OnDisable()
        {
   
            modeActions.Disable();
        }
    }
}
