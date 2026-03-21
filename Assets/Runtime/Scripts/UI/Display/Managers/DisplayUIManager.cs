using UnityEngine;
using TMPro;
using Tempera.Mental.Core;

namespace Tempera.Mental.UI.Display
{
    public class DisplayUIManager : MonoBehaviour
    {
        const string LOOP_ON = "ON";
        const string LOOP_OFF = "OFF";

        [SerializeField] TextMeshProUGUI bpmText;
        [SerializeField] TextMeshProUGUI frameText;
        [SerializeField] TextMeshProUGUI loopText;


        public void ActionOnLoopStateChanged(bool isLooping)
        {
            loopText.text = isLooping ? LOOP_ON : LOOP_OFF;
        }

        public void ActionOnFrameChanged(VisualFrameDetail frameDetail)
        {
            frameText.text = $"{frameDetail.FrameNumber} / {frameDetail.FrameTotal}";
        }

        public void ActionOnBpmChanged(float bpm)
        {
            bpmText.text = bpm.ToString();
        }

    }
}
