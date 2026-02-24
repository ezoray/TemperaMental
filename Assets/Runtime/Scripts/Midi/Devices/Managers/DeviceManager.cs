using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Multimedia;
using Tempera.Mental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace Tempera.Mental.Midi.Devices
{
    public class DeviceManager : MonoBehaviour
    {
        Dictionary<string, MidiDevice> outputDevices;

        [SerializeField] UnityEvent<List<string>> onDevicesAvailable;
        [SerializeField] UnityEvent<string> onDeviceAdded;
        [SerializeField] UnityEvent<string> onDeviceRemoved;

        MidiDevice transientDevice;

        bool isDeviceRemoved;


        private void Start()
        {
            LogMan.Log("DeviceManager Start");

            DevicesWatcher.Instance.DeviceAdded += OnDeviceAdded;
            DevicesWatcher.Instance.DeviceRemoved += OnDeviceRemoved;

            outputDevices = new Dictionary<string, MidiDevice>();
            List<string> deviceNames = new List<string>();

            try
            {
                foreach (var device in OutputDevice.GetAll())
                {
                    LogMan.Log("Device: " + device.Name);

                    outputDevices.Add(device.Name, device);
                    deviceNames.Add(device.Name);
                }

                deviceNames.Sort((a, b) =>
                {
                    if (a == "Tempera") return -1; // a comes first
                    if (b == "Tempera") return 1;  // b comes first
                    return a.CompareTo(b);         // otherwise, normal alphabetical sort
                });

                onDevicesAvailable?.Invoke(deviceNames);
            }
            catch(System.Exception ex)
            {
                LogMan.LogError("Error getting device list: " + ex);
            }

        }

        private void Update()
        {
            if(isDeviceRemoved)
            {
                LogMan.Log("Update Device Removed");

                isDeviceRemoved = false;

                SyncDictionaryWithAvailableDevices();
            }
        }

        private void SyncDictionaryWithAvailableDevices()
        {
            var currentHardware = OutputDevice.GetAll().Select(d => d.Name).ToList();
            List<string> namesToRemove = new List<string>();

            // Find keys in our dictionary that no longer exist in hardware
            foreach (var storedName in outputDevices.Keys)
            {
                if (!currentHardware.Contains(storedName))
                {
                    namesToRemove.Add(storedName);
                }
            }

            foreach (var deviceName in namesToRemove)
            {
                LogMan.Log("Remove device: " + deviceName);

                outputDevices[deviceName].Dispose();
                outputDevices.Remove(deviceName);

                onDeviceRemoved?.Invoke(deviceName);
            }
        }

        private void OnDeviceRemoved(object sender, DeviceAddedRemovedEventArgs eventArgs)
        {
            LogMan.Log("OnDeviceRemoved");

            isDeviceRemoved = true;
        }

        private void OnDeviceAdded(object sender, DeviceAddedRemovedEventArgs eventArgs)
        {
            Debug.Log("OnDeviceAdded Device: " + eventArgs.Device.Name);

            MidiDevice device = eventArgs.Device;

            if(outputDevices.TryAdd(device.Name, device))
            {
                onDeviceAdded?.Invoke(device.Name);
            }
        }

        public bool TryGetOutputDevice(string deviceName, out MidiDevice outputDevice)
        {
            return outputDevices.TryGetValue(deviceName, out outputDevice);
        }

        private void OnDisable()
        {

            DevicesWatcher.Instance.DeviceAdded -= OnDeviceAdded;
            DevicesWatcher.Instance.DeviceRemoved -= OnDeviceRemoved;

            foreach (OutputDevice device in outputDevices.Values)
            {
                device?.Dispose();     
            }

            outputDevices.Clear();
        }
    }
}
