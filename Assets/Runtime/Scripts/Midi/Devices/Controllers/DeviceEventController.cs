using UnityEngine;

namespace TemperaMental.Midi.Devices
{
    public class DeviceEventController : MonoBehaviour
    {
        [SerializeField] DeviceManager deviceManager;


        public void ActionOnDeviceSelected(string deviceName)
        {
            deviceManager.SetOutputDevice(deviceName);
        }
    }
}
