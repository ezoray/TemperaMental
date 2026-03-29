using UnityEngine;
using TMPro;
using TemperaMental.Core;
using TemperaMental.Applications.Config;

namespace TemperaMental.UI.Display
{
    public class DisplayUIManager : MonoBehaviour
    {
        const string ON = "ON";
        const string OFF = "OFF";

        [SerializeField] TextMeshProUGUI bpmText;
        [SerializeField] TextMeshProUGUI frameText;
        [SerializeField] TextMeshProUGUI reverseText;
        [SerializeField] TextMeshProUGUI loopText;

        private void Awake()
        {
            bpmText.text = ConfigRegistry.Midi.DefaultBpm.ToString();
        }

        public void ActionOnReverseStateChanged(bool isReversed)
        {
            reverseText.text = isReversed ? ON : OFF;
        }

        public void ActionOnLoopStateChanged(bool isLooping)
        {
            loopText.text = isLooping ? ON : OFF;
        }

        public void ActionOnFrameChanged(FrameDetail frameDetail)
        {
            frameText.text = $"{frameDetail.FrameNumber} / {frameDetail.FrameTotal}";
        }

        public void ActionOnBpmChanged(int bpm)
        {
            bpmText.text = bpm.ToString();
        }

    }
}
