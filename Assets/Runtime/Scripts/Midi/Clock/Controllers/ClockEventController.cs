using TemperaMental.Core;
using TemperaMental.Midi.Input;
using TemperaMental.Midi.Output;
using UnityEngine;

namespace TemperaMental.Midi.Clock
{
    public class ClockEventController : MonoBehaviour
    {
        [SerializeField] ClockEngine clockEngine;
        [SerializeField] MidiOutputScheduler outputScheduler;

        // listens to a separately chosen Clock from device
        [SerializeField] MidiInputRouter clockInputRouter;

        public void ActionOnBpmChanged(int newBpm) => clockEngine.SetBpm(newBpm);

        public void ActionOnClockModeChanged(ClockMode clockMode) => clockEngine.SetClockMode(clockMode);

        public void ActionOnSendStartStopChanged(bool isSent) => clockEngine.SetTransportSent(isSent);

        public void ActionOnClockRoutingChanged(ClockRouting routing)
        {
            if (routing.SeparateInput != null)
                clockInputRouter.SetInputDevice(routing.SeparateInput);
            else
                clockInputRouter.ClearInputDevice();

            outputScheduler.SetClockDestination(routing.Destination, routing.SeparateOutput);
            clockEngine.SetClockRouting(routing.SeparateInput != null, routing.Destination, routing.IsForwardingAllowed);
        }
    }
}
