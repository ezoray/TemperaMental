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
        Dictionary<string, OutputDevice> outputDevices;
        List<string> deviceNames;

        [SerializeField] UnityEvent<List<string>> onDevicesUpdated;
        [SerializeField] UnityEvent<string> onPrimaryDeviceFound;
        [SerializeField] UnityEvent<string> onDeviceAdded;
        [SerializeField] UnityEvent<string> onDeviceRemoved;

        bool isDeviceChange;

        private void OnEnable()
        {
            DevicesWatcher.Instance.DeviceAdded += OnDeviceAdded;
            DevicesWatcher.Instance.DeviceRemoved += OnDeviceRemoved;

            outputDevices = new Dictionary<string, OutputDevice>();
            deviceNames = new List<string>();
        }

        private void Start()
        {
            try
            {
                UpdateAvailableDevices();
                SetPrimaryDeviceAsFirstDevice();

                onDevicesUpdated?.Invoke(deviceNames);

                if (deviceNames.Contains("Tempera"))
                {
                    LogMan.Log("Tempera found on Startup. Triggering initial selection.");
                    onPrimaryDeviceFound?.Invoke("Tempera");
                }
            }
            catch(System.Exception ex)
            {
                LogMan.LogError("Error getting device list: " + ex);
            }
        }

        // DevicesWatcher runs on a separate thread so signal a device change to pick up in Update
        private void Update()
        {
            if(isDeviceChange)
            {
                isDeviceChange = false;
                UpdateAvailableDevices();                
            }
        }

        public bool TryGetOutputDevice(string deviceName, out OutputDevice outputDevice)
        {
            return outputDevices.TryGetValue(deviceName, out outputDevice);
        }

        private void UpdateAvailableDevices()
        {
            try
            {
                // 1. Get a fresh snapshot of what the OS sees right now
                List<OutputDevice> currentDevices = OutputDevice.GetAll().ToList();
                deviceNames = currentDevices.Select(d => d.Name).ToList();

                // remove disconnected devices
                List<string> namesToRemove = outputDevices.Keys
                    .Where(name => !deviceNames.Contains(name))
                    .ToList();

                foreach (string name in namesToRemove)
                {
                    LogMan.Log("Device disconnected " + name);

                    // Critically important: Close the handle before removing from dict
                    outputDevices[name]?.Dispose();
                    outputDevices.Remove(name);

                    onDeviceRemoved?.Invoke(name);
                }

                // --- STEP B: ADD NEWLY CONNECTED DEVICES ---
                foreach (var device in currentDevices)
                {
                    // If we don't have it in our dict yet, it's new!
                    if (!outputDevices.ContainsKey(device.Name))
                    {
                        LogMan.Log("New device detected: " + device.Name);

                        // Add to dictionary
                        outputDevices.Add(device.Name, device);

                        onDeviceAdded?.Invoke(device.Name);
                    }
                    else
                    {
                        device.Dispose();
                    }
                }

                SetPrimaryDeviceAsFirstDevice();
                onDevicesUpdated?.Invoke(deviceNames);
            }
            catch (System.Exception ex)
            {
                LogMan.LogError("Error updating device list: " + ex);
            }        
        }


        private void SetPrimaryDeviceAsFirstDevice()
        {
            deviceNames.Sort((a, b) =>
            {
                if (a == "Tempera") return -1;
                if (b == "Tempera") return 1;
                return a.CompareTo(b);
            });
        }

        private void OnDeviceRemoved(object sender, DeviceAddedRemovedEventArgs eventArgs)
        {
            LogMan.Log("OnDeviceRemoved");

            isDeviceChange = true;
        }

        private void OnDeviceAdded(object sender, DeviceAddedRemovedEventArgs eventArgs)
        {
            LogMan.Log("OnDeviceAdded Device: " + eventArgs.Device.Name);

            isDeviceChange = true;
        }

        private void OnDisable()
        {
            if (DevicesWatcher.Instance != null)
            {
                DevicesWatcher.Instance.DeviceAdded -= OnDeviceAdded;
                DevicesWatcher.Instance.DeviceRemoved -= OnDeviceRemoved;
            }

            foreach (OutputDevice device in outputDevices.Values)
            {
                device?.Dispose();
            }
            outputDevices.Clear();
        }
    }
}
