using Tempera.Mental.Logs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tempera.Mental.UI
{
    // due to button text not dimming when button is disabled override it and handle it ourselves
    public class DimmableTextButton : Button
    {
        const float DISABLED_ALPHA = 0.3f;

        // hack this needs to be wired under Debug in the inspector as the field does not show normally
        [SerializeField] TextMeshProUGUI buttonText;


        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if(state == SelectionState.Normal || state == SelectionState.Disabled) {

                float targetAlpha = (state == SelectionState.Disabled) ? DISABLED_ALPHA : 1f;

                Color c = buttonText.color;
                c.a = targetAlpha;
                buttonText.color = c;
            }
        }
    }
}
