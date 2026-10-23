using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Midi.Clock
{
    public class ClockEventController : MonoBehaviour
    {
        [SerializeField] ClockEngine clockEngine;

        public void ActionOnBpmChanged(int newBpm) => clockEngine.SetBpm(newBpm);

        public void ActionOnClockModeChanged(ClockMode clockMode) => clockEngine.SetClockMode(clockMode);

        public void ActionOnSendStartStopChanged(bool isSent) => clockEngine.SetTransportSent(isSent);
    }
}
