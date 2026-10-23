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
    }
}
