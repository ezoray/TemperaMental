using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Applications.Config;
using TemperaMental.Logs;
using TemperaMental.Midi.Clock;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Devices
{
    // Finds MIDI devices and opens the chosen output (emitter CCs, clock out) and input (Tempera notes, clock in).
    // Device changes are polled under Windows, which has no callbacks, and reported by DevicesWatcher elsewhere.
    // The last chosen devices are remembered, selected at start-up and selected again when they reconnect.
    // Initial and reconnect selection go through the dropdowns (onInitial...Found), the same path as a user's choice.
    public class DeviceManager : MonoBehaviour
    {
        string primaryDevice;
        float pollingInterval;
        string outputPrefsKey;
        string inputPrefsKey;

        List<string> connectedOutputs = new List<string>();
        List<string> connectedInputs = new List<string>();

        OutputDevice currentOutput;
        string currentOutputName;

        InputDevice currentInput;
        string currentInputName;

        // Clock from / Clock to: "" means the same device as In / Out, ClockToNone means no clock out
        string clockInputChoice = "";
        string clockOutputChoice = "";
        string clockToNone;

        InputDevice clockInput;
        string clockInputName;
        OutputDevice clockOutput;
        string clockOutputName;

        bool isInitialSync = true;
        volatile bool isDeviceChange;

        bool isWatcherSubscribed;

        [Header("Output")]
        [SerializeField] UnityEvent<string> onInitialDeviceFound;
        [SerializeField] UnityEvent<List<string>, string> onDevicesUpdated;
        [SerializeField] UnityEvent<OutputDevice> onDeviceSelected;
        [SerializeField] UnityEvent onCurrentDeviceRemoved;

        [Header("Input")]
        [SerializeField] UnityEvent<string> onInitialInputDeviceFound;
        [SerializeField] UnityEvent<List<string>, string> onInputDevicesUpdated;
        [SerializeField] UnityEvent<InputDevice> onInputDeviceSelected;
        [SerializeField] UnityEvent onInputDeviceClosed;

        [Header("Clock")]
        [SerializeField] UnityEvent<ClockRouting> onClockRoutingChanged = new UnityEvent<ClockRouting>();

        private void Awake()
        {
            primaryDevice = ConfigRegistry.Midi.PrimaryDevice;
            pollingInterval = ConfigRegistry.Midi.PollingInterval;
            outputPrefsKey = ConfigRegistry.Midi.OutputDevicePrefsKey;
            inputPrefsKey = ConfigRegistry.Midi.InputDevicePrefsKey;
            clockToNone = ConfigRegistry.Midi.ClockToNoneValue;
        }

        public IReadOnlyList<string> ConnectedInputs => connectedInputs;
        public IReadOnlyList<string> ConnectedOutputs => connectedOutputs;

        private void OnEnable()
        {
#if !UNITY_STANDALONE_WIN
            SubscribeWatcher();
#endif
        }

        private void Start()
        {
#if UNITY_STANDALONE_WIN
            StartCoroutine(PollDevices());
#else
            SubscribeWatcher();
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
            List<string> outputs = GetOutputNames();
            List<string> inputs = GetInputNames();

            List<string> removedOutputs = connectedOutputs.Except(outputs).ToList();
            List<string> addedOutputs = outputs.Except(connectedOutputs).ToList();
            List<string> removedInputs = connectedInputs.Except(inputs).ToList();
            List<string> addedInputs = inputs.Except(connectedInputs).ToList();

            // most devices have an input and an output with the same name, so report each name once
            foreach (string name in removedOutputs.Union(removedInputs))
                LogMan.Log($"Device disconnected: {name}");

            foreach (string name in addedOutputs.Union(addedInputs))
                LogMan.Log($"Device connected: {name}");

            if (currentOutputName != null && removedOutputs.Contains(currentOutputName))
            {
                CloseOutput();
                onCurrentDeviceRemoved?.Invoke();
            }

            if (currentInputName != null && removedInputs.Contains(currentInputName))
            {
                CloseInput();
            }

            if (clockInputName != null && removedInputs.Contains(clockInputName))
            {
                CloseClockInput();
            }

            if (clockOutputName != null && removedOutputs.Contains(clockOutputName))
            {
                CloseClockOutput();
            }

            connectedOutputs = outputs;
            connectedInputs = inputs;

            onDevicesUpdated?.Invoke(connectedOutputs, currentOutputName);
            onInputDevicesUpdated?.Invoke(connectedInputs, currentInputName);

            if (isInitialSync)
            {
                isInitialSync = false;
                HandleInitialSelection();
            }
            else
            {
                ReselectReconnected(addedOutputs, addedInputs);
            }

            // reopens chosen clock devices that have (re)appeared
            ApplyClockRouting();
        }

        // "" for the same device as In, otherwise an input name
        public void SetClockInput(string choice)
        {
            clockInputChoice = choice ?? "";
            ApplyClockRouting();
        }

        // "" for the same device as Out, ClockToNoneValue for none, otherwise an output name
        public void SetClockOutput(string choice)
        {
            clockOutputChoice = choice ?? "";
            ApplyClockRouting();
        }

        // opens or closes the separate clock devices to match the choices, then tells listeners where clock goes
        private void ApplyClockRouting()
        {
            // a separate device only when one is chosen and it isn't already the In / Out device
            string wantedInput = clockInputChoice == "" || clockInputChoice == currentInputName ? null : clockInputChoice;
            bool isOutputNone = clockOutputChoice == clockToNone;
            string wantedOutput = clockOutputChoice == "" || isOutputNone || clockOutputChoice == currentOutputName ? null : clockOutputChoice;

            if (wantedInput != clockInputName)
            {
                CloseClockInput();

                if (wantedInput != null && connectedInputs.Contains(wantedInput))
                {
                    try
                    {
                        clockInput = InputDevice.GetByName(wantedInput);
                        clockInputName = wantedInput;
                        LogMan.Log($"Clock from '{wantedInput}'");
                    }
                    catch (Exception ex)
                    {
                        clockInput = null;
                        clockInputName = null;
                        LogMan.LogError($"Failed to open clock input {wantedInput}: {ex.Message}");
                    }
                }
            }

            if (wantedOutput != clockOutputName)
            {
                CloseClockOutput();

                if (wantedOutput != null && connectedOutputs.Contains(wantedOutput))
                {
                    try
                    {
                        clockOutput = OutputDevice.GetByName(wantedOutput);
                        clockOutput.PrepareForEventsSending();
                        clockOutputName = wantedOutput;
                        LogMan.Log($"Clock to '{wantedOutput}'");
                    }
                    catch (Exception ex)
                    {
                        clockOutput = null;
                        clockOutputName = null;
                        LogMan.LogError($"Failed to open clock output {wantedOutput}: {ex.Message}");
                    }
                }
            }

            RaiseClockRouting();
        }

        private void RaiseClockRouting()
        {
            ClockDestination destination = clockOutputChoice == clockToNone ? ClockDestination.None
                : clockOutput != null ? ClockDestination.Separate
                : ClockDestination.SameAsOut;

            string sourceName = clockInput != null ? clockInputName : currentInputName;
            string destinationName = destination == ClockDestination.Separate ? clockOutputName
                : destination == ClockDestination.SameAsOut ? currentOutputName
                : null;

            // never forward clock back to the device it comes from
            bool isForwardingAllowed = destinationName != null && destinationName != sourceName;

            onClockRoutingChanged?.Invoke(new ClockRouting(clockInput, destination, clockOutput, isForwardingAllowed));
        }

        // listeners stop using the clock input before it is disposed
        private void CloseClockInput()
        {
            if (clockInput == null) return;

            InputDevice device = clockInput;
            clockInput = null;
            clockInputName = null;

            RaiseClockRouting();

            try
            {
                device.Dispose();
            }
            catch (Exception ex)
            {
                LogMan.LogWarning($"Error closing clock input: {ex.Message}");
            }
        }

        private void CloseClockOutput()
        {
            if (clockOutput == null) return;

            OutputDevice device = clockOutput;
            clockOutput = null;
            clockOutputName = null;

            RaiseClockRouting();

            try
            {
                device.Dispose();
            }
            catch (Exception ex)
            {
                LogMan.LogWarning($"Error closing clock output: {ex.Message}");
            }
        }

        private void HandleInitialSelection()
        {
            if (connectedOutputs.Count == 0)
            {
                LogMan.Log("No MIDI devices detected");
            }
            else
            {
                string savedOutput = PlayerPrefs.GetString(outputPrefsKey, "");

                string targetOutput = connectedOutputs.Contains(savedOutput) ? savedOutput
                    : connectedOutputs.Contains(primaryDevice) ? primaryDevice
                    : connectedOutputs[0];

                onInitialDeviceFound?.Invoke(targetOutput);
            }

            // inputs are only opened automatically when they were chosen before or are the primary device
            string savedInput = PlayerPrefs.GetString(inputPrefsKey, "");

            string targetInput = connectedInputs.Contains(savedInput) ? savedInput
                : connectedInputs.Contains(primaryDevice) ? primaryDevice
                : null;

            if (targetInput != null)
            {
                onInitialInputDeviceFound?.Invoke(targetInput);
            }
        }

        private void ReselectReconnected(List<string> addedOutputs, List<string> addedInputs)
        {
            string savedOutput = PlayerPrefs.GetString(outputPrefsKey, "");

            if (currentOutput == null && addedOutputs.Contains(savedOutput))
            {
                onInitialDeviceFound?.Invoke(savedOutput);
            }

            string savedInput = PlayerPrefs.GetString(inputPrefsKey, "");

            if (currentInput == null && addedInputs.Contains(savedInput))
            {
                onInitialInputDeviceFound?.Invoke(savedInput);
            }
        }

        public void SetOutputDevice(string deviceName)
        {
            try
            {
                CloseOutput();

                // the port is about to become the Out device, so it no longer needs its own clock handle
                // (Windows only lets a port be opened once)
                if (clockOutputName == deviceName) CloseClockOutput();

                currentOutput = OutputDevice.GetByName(deviceName);
                currentOutput.PrepareForEventsSending();
                currentOutputName = deviceName;

                PlayerPrefs.SetString(outputPrefsKey, deviceName);
                PlayerPrefs.Save();

                onDeviceSelected?.Invoke(currentOutput);
                LogMan.Log($"Output '{deviceName}' selected");
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Failed to open output {deviceName}: {ex.Message}");

                currentOutput = null;
                currentOutputName = null;

                connectedOutputs.Remove(deviceName);
                onDevicesUpdated?.Invoke(connectedOutputs, currentOutputName);
            }

            ApplyClockRouting();
        }

        public void SetInputDevice(string deviceName)
        {
            try
            {
                CloseInput();

                // the port is about to become the In device, so it no longer needs its own clock handle
                if (clockInputName == deviceName) CloseClockInput();

                currentInput = InputDevice.GetByName(deviceName);
                currentInputName = deviceName;

                PlayerPrefs.SetString(inputPrefsKey, deviceName);
                PlayerPrefs.Save();

                onInputDeviceSelected?.Invoke(currentInput);
                LogMan.Log($"Input '{deviceName}' selected");
            }
            catch (Exception ex)
            {
                LogMan.LogError($"Failed to open input {deviceName}: {ex.Message}");

                currentInput = null;
                currentInputName = null;

                connectedInputs.Remove(deviceName);
                onInputDevicesUpdated?.Invoke(connectedInputs, currentInputName);
            }

            ApplyClockRouting();
        }

        private List<string> GetOutputNames()
        {
            var names = new List<string>();
            IEnumerable<OutputDevice> devices;

            try
            {
                devices = OutputDevice.GetAll();
            }
            catch (Exception ex)
            {
                LogMan.LogWarning($"Failed to enumerate MIDI outputs: {ex.Message}");
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
                    LogMan.LogWarning($"Skipped unreadable output during enumeration: {ex.Message}");
                }
                finally
                {
                    device.Dispose();
                }
            }

            return names;
        }

        private List<string> GetInputNames()
        {
            var names = new List<string>();
            IEnumerable<InputDevice> devices;

            try
            {
                devices = InputDevice.GetAll();
            }
            catch (Exception ex)
            {
                LogMan.LogWarning($"Failed to enumerate MIDI inputs: {ex.Message}");
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
                    LogMan.LogWarning($"Skipped unreadable input during enumeration: {ex.Message}");
                }
                finally
                {
                    device.Dispose();
                }
            }

            return names;
        }

        private void CloseOutput()
        {
            if (currentOutput == null) return;

            try
            {
                currentOutput.Dispose();
            }
            catch (Exception ex)
            {
                LogMan.LogWarning($"Error closing output {currentOutputName}: {ex.Message}");
            }
            finally
            {
                currentOutput = null;
                currentOutputName = null;
            }
        }

        // listeners stop using the input before it is disposed
        private void CloseInput()
        {
            if (currentInput == null) return;

            onInputDeviceClosed?.Invoke();

            try
            {
                currentInput.Dispose();
            }
            catch (Exception ex)
            {
                LogMan.LogWarning($"Error closing input {currentInputName}: {ex.Message}");
            }
            finally
            {
                currentInput = null;
                currentInputName = null;
            }
        }

        private void SubscribeWatcher()
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

#if UNITY_STANDALONE_WIN
        private IEnumerator PollDevices()
        {
            var wait = new WaitForSeconds(pollingInterval);
            List<string> previousOutputs = new List<string>();
            List<string> previousInputs = new List<string>();

            while (true)
            {
                yield return wait;

                List<string> outputs = GetOutputNames();
                List<string> inputs = GetInputNames();

                if (!outputs.SequenceEqual(previousOutputs) || !inputs.SequenceEqual(previousInputs))
                {
                    previousOutputs = outputs;
                    previousInputs = inputs;
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
            CloseClockInput();
            CloseClockOutput();
            CloseInput();
            CloseOutput();
        }

        private void OnDestroy()
        {
            CloseClockInput();
            CloseClockOutput();
            CloseInput();
            CloseOutput();
        }
    }
}
