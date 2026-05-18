using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Applications.Config;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Devices
{
    // poll for midi device changes in Windows only, use callbacks in macOS
    public class DeviceManager : MonoBehaviour
    {
        string primaryDevice;
        string currentDeviceName;

        List<string> connectedDevices;
        OutputDevice currentDevice;

        bool isInitialSync = true;
        volatile bool isDeviceChange;
        float pollingInterval; 

        bool isWatcherSubscribed;

        [SerializeField] UnityEvent<string> onInitialDeviceFound;
        [SerializeField] UnityEvent<List<string>, string> onDevicesUpdated;
        [SerializeField] UnityEvent<OutputDevice> onDeviceSelected;
        [SerializeField] UnityEvent onCurrentDeviceRemoved;

        private void Awake()
        {
            connectedDevices = new List<string>();

            primaryDevice = ConfigRegistry.Midi.PrimaryDevice;
            pollingInterval = ConfigRegistry.Midi.PollingInterval;
        }

        private void OnEnable()
        {
#if !UNITY_STANDALONE_WIN
            if (DevicesWatcher.Instance != null && !isWatcherSubscribed)
            {
                DevicesWatcher.Instance.DeviceAdded += OnDeviceChange;
                DevicesWatcher.Instance.DeviceRemoved += OnDeviceChange;
                isWatcherSubscribed = true;
            }
#endif
        }

        private void Start()
        {
#if UNITY_STANDALONE_WIN
    StartCoroutine(PollDevices());
#else
            if (DevicesWatcher.Instance != null && !isWatcherSubscribed)
            {
                DevicesWatcher.Instance.DeviceAdded += OnDeviceChange;
                DevicesWatcher.Instance.DeviceRemoved += OnDeviceChange;
                isWatcherSubscribed = true;
            }
#endif
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

            foreach (string name in added)
            {
                LogMan.Log($"Device connected: {name}");
            }

            connectedDevices = hardware;

            onDevicesUpdated?.Invoke(connectedDevices, currentDeviceName);

            if (isInitialSync)
            {
                isInitialSync = false;
                HandleInitialSelection();
            }
        }

        private List<string> GetHardwareDeviceNames()
        {
            var names = new List<string>();

            IEnumerable<OutputDevice> devices;

            try
            {
                devices = OutputDevice.GetAll();
            }
            catch (Exception ex)
            {
                LogMan.LogWarning($"Failed to enumerate MIDI devices: {ex.Message}");
                return names;
            }

            foreach (var device in devices)
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

            string targetDevice = connectedDevices.Contains(primaryDevice) ? primaryDevice : connectedDevices[0];

            onInitialDeviceFound?.Invoke(targetDevice);
        }

        public void SetOutputDevice(string deviceName)
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

#if UNITY_STANDALONE_WIN
        private IEnumerator PollDevices()
        {
            var wait = new WaitForSeconds(pollingInterval);
            List<string> previousDevices = new List<string>();

            while (true)
            {
                yield return wait;
                List<string> current = GetHardwareDeviceNames();
                if (!current.SequenceEqual(previousDevices))
                {
                    previousDevices = current;
                    isDeviceChange = true;
                }
            }
        }
#else
        private void OnDeviceChange(object sender, DeviceAddedRemovedEventArgs eventArgs)
        {
            isDeviceChange = true;
        }
#endif

        private void DisposeCurrentDevice()
        {
            if (currentDevice == null) return;
            try
            {
                currentDevice.Dispose();
            }
            catch (Exception ex)
            {
                LogMan.LogWarning($"Error disposing device: {ex.Message}");
            }
            finally
            {
                currentDevice = null;
                currentDeviceName = null;
            }
        }

        private void OnDisable()
        {
#if !UNITY_STANDALONE_WIN
            if (DevicesWatcher.Instance != null && isWatcherSubscribed)
            {
                DevicesWatcher.Instance.DeviceAdded -= OnDeviceChange;
                DevicesWatcher.Instance.DeviceRemoved -= OnDeviceChange;
                isWatcherSubscribed = false;
            }
#endif
            DisposeCurrentDevice();
        }

        private void OnDestroy()
        {
            DisposeCurrentDevice();
        }
    }
}