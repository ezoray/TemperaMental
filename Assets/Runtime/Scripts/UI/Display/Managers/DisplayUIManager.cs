using UnityEngine;
using TMPro;
using TemperaMental.Core;
using TemperaMental.Applications.Config;

namespace TemperaMental.UI.Display
{
    public class DisplayUIManager : MonoBehaviour
    {
        public string onText;
        public string offText;

        [SerializeField] TextMeshProUGUI bpmText;
        [SerializeField] TextMeshProUGUI frameText;
        [SerializeField] TextMeshProUGUI reverseText;
        [SerializeField] TextMeshProUGUI loopText;

        private void Awake()
        {
            onText = ConfigRegistry.UI.OnText;
            offText = ConfigRegistry.UI.OffText;

            bpmText.text = ConfigRegistry.Midi.DefaultBpm.ToString();
        }

        public void ActionOnReverseStateChanged(bool isReversed)
        {
            reverseText.text = isReversed ? onText : offText;
        }

        public void ActionOnLoopStateChanged(bool isLooping)
        {
            loopText.text = isLooping ? onText : offText;
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
