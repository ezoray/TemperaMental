using System;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Applications.Config;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Devices
{
    public class DeviceManager : MonoBehaviour
    {
        string primaryDevice;
        string currentDeviceName;

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
            primaryDevice = ConfigRegistry.Midi.PrimaryDevice;
        }

        private void Start()
        {
            DevicesWatcher.Instance.DeviceAdded += OnDeviceChange;
            DevicesWatcher.Instance.DeviceRemoved += OnDeviceChange;
            SyncDeviceList();
        }

        private void Update()
        {
            if (!isDeviceChange) return;

            isDeviceChange = false;
            SyncDeviceList();
        }

        private void SyncDeviceList()
        {
            List<string> hardware = GetHardwareDeviceNames();
            List<string> removed = connectedDevices.Except(hardware).ToList();
            List<string> added = hardware.Except(connectedDevices).ToList();

            foreach (string name in removed)
            {
                LogMan.Log($"Device disconnected: {name}");
                if (currentDevice != null && currentDeviceName == name)
                {
                    try
                    {
                        currentDevice.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogMan.LogWarning($"Error disposing device {name}: {ex.Message}");
                    }
                    finally
                    {
                        currentDevice = null;
                        currentDeviceName = null;
                    }

                    onCurrentDeviceRemoved?.Invoke();
                }
            }

            foreach (var name in added)
            {
                LogMan.Log($"Device connected: {name}");
            }

            connectedDevices = hardware;
            onDevicesUpdated?.Invoke(connectedDevices);

            if (isInitialSync)
            {
                isInitialSync = false;
                HandleInitialSelection();
            }
        }

        private List<string> GetHardwareDeviceNames()
        {
            var names = new List<string>();

            foreach (var device in OutputDevice.GetAll())
            {
                try
                {
                    names.Add(device.Name);
                }
                catch (Exception ex)
                {
                    LogMan.LogWarning($"Skipped unreadable device during enumeration: {ex.Message}");
                }
                finally
                {
                    device.Dispose();
                }
            }

            return names;
        }

        private void HandleInitialSelection()
        {
            if (connectedDevices.Count == 0)
            {
                LogMan.Log("No MIDI devices detected");
                return;
            }

            string target = connectedDevices.Contains(primaryDevice) ? primaryDevice : connectedDevices[0];

            ActionOnDeviceSelected(target);

            onAutoSelectDevice?.Invoke(target);
        }

        private void OnDeviceChange(object sender, DeviceAddedRemovedEventArgs eventArgs)
        {
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
                    currentDeviceName = null;
                }

                currentDevice = OutputDevice.GetByName(deviceName);
                currentDevice.PrepareForEventsSending();
                currentDeviceName = deviceName;

                onDeviceSelected?.Invoke(currentDevice);
                LogMan.Log($"Device '{deviceName}' selected");
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Failed to open device {deviceName}: {ex.Message}");
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