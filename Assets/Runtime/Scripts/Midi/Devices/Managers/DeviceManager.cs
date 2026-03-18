using System;
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
        const string PRIMARY_DEVICE = "MidiView";

        List<string> connectedDevices;
        OutputDevice currentDevice;

        bool isInitialSync;
        volatile bool isDeviceChange;

        [SerializeField] UnityEvent<string> onAutoSelectDevice;
        [SerializeField] UnityEvent<OutputDevice> onDeviceSelected;
        [SerializeField] UnityEvent onCurrentDeviceRemoved;
        [SerializeField] UnityEvent<List<string>> onDevicesUpdated;


        private void OnEnable()
        {
            connectedDevices = new List<string>();
            isInitialSync = true;
        }

        private void Start()
        {
            DevicesWatcher.Instance.DeviceAdded += OnDeviceChange;
            DevicesWatcher.Instance.DeviceRemoved += OnDeviceChange;

            SyncDeviceList();
        }

        // DevicesWatcher runs on a separate thread so signal a device change to pick up in Update
        private void Update()
        {
            // todo don't poll so often
            if(isDeviceChange)
            {
                isDeviceChange = false;

                SyncDeviceList();    
            }
        }

        public OutputDevice GetOutputDevice(string deviceName)
        {
            try
            {
                return OutputDevice.GetByName(deviceName);
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Could not open device: {ex.Message}");
                return null;
            }
        }

        private void SyncDeviceList()
        {
            try
            {
                List<string> hardware = OutputDevice.GetAll().Select(d => d.Name).ToList();
                List<string> removed = connectedDevices.Except(hardware).ToList();

                foreach (var name in removed)
                {
                    if (currentDevice != null && currentDevice.Name == name)
                    {
                        currentDevice.Dispose();
                        currentDevice = null;
                        onCurrentDeviceRemoved?.Invoke();
                    }
                }

                connectedDevices = hardware;

                onDevicesUpdated?.Invoke(connectedDevices);

                if (isInitialSync)
                {
                    isInitialSync = false;

                    HandleInitialSelection();
                }
            }
            catch (Exception ex)
            {
                LogMan.LogError(ex.Message);
            }
        }

        private void HandleInitialSelection()
        {
            if (connectedDevices.Count == 0) return;

            string target = connectedDevices.Contains(PRIMARY_DEVICE)
                ? PRIMARY_DEVICE
                : connectedDevices[0];

            ActionOnDeviceSelected(target);

            onAutoSelectDevice?.Invoke(target);
        }

        private void OnDeviceChange(object sender, DeviceAddedRemovedEventArgs eventArgs)
        {
            LogMan.Log("OnDeviceChange Device: " + eventArgs.Device);

            isDeviceChange = true;
        }

        public void ActionOnDeviceSelected(string deviceName)
        {
            try
            {
                if (currentDevice != null)
                {
                    currentDevice.Dispose();
                    currentDevice = null;
                }

                currentDevice = OutputDevice.GetByName(deviceName);

                onDeviceSelected?.Invoke(currentDevice);

                LogMan.Log($"Device {deviceName} successfully opened.");
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Failed to select device {deviceName}: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            if (DevicesWatcher.Instance != null)
            {
                DevicesWatcher.Instance.DeviceAdded -= OnDeviceChange;
                DevicesWatcher.Instance.DeviceRemoved -= OnDeviceChange;
            }
        }
    }
}
