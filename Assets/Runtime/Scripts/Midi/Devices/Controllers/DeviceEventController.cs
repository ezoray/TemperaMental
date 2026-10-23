using UnityEngine;

namespace TemperaMental.Midi.Devices
{
    public class DeviceEventController : MonoBehaviour
    {
        [SerializeField] DeviceManager deviceManager;


        // output dropdown
        public void ActionOnDeviceSelected(string deviceName)
        {
            deviceManager.SetOutputDevice(deviceName);
        }

        // input dropdown
        public void ActionOnInputDeviceSelected(string deviceName)
        {
            deviceManager.SetInputDevice(deviceName);
        }

        // clock devices view: "" means same as In / Out
        public void ActionOnClockFromChanged(string choice) => deviceManager.SetClockInput(choice);

        public void ActionOnClockToChanged(string choice) => deviceManager.SetClockOutput(choice);
    }
}
