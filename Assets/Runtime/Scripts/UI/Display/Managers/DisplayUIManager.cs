using UnityEngine;
using TMPro;
using TemperaMental.Core;
using TemperaMental.Applications.Config;

namespace TemperaMental.UI.Display
{
    public class DisplayUIManager : MonoBehaviour
    {
        const string LOOP_ON = "ON";
        const string LOOP_OFF = "OFF";

        [SerializeField] TextMeshProUGUI bpmText;
        [SerializeField] TextMeshProUGUI frameText;
        [SerializeField] TextMeshProUGUI loopText;

        private void Awake()
        {
            bpmText.text = ConfigRegistry.Midi.DefaultBpm.ToString();
        }

        public void ActionOnLoopStateChanged(bool isLooping)
        {
            loopText.text = isLooping ? LOOP_ON : LOOP_OFF;
        }

        public void ActionOnFrameChanged(VisualFrameDetail frameDetail)
        {
            frameText.text = $"{frameDetail.FrameNumber} / {frameDetail.FrameTotal}";
        }

        public void ActionOnBpmChanged(int bpm)
        {
            bpmText.text = bpm.ToString();
        }

    }
}
