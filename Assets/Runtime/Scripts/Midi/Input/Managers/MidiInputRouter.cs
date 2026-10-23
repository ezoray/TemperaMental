using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Logs;
using TemperaMental.Midi.Core;
using UnityEngine;

namespace TemperaMental.Midi.Input
{
    // Listens to the selected MIDI input device.
    // Messages arrive on the driver's thread, are timestamped straight away and passed on in two ways:
    // MessageReceived on the driver thread (for timing-critical listeners such as clock following; keep handlers short,
    // no Unity APIs) and MessageReceivedOnMainThread from Update (for everything else).
    // The DeviceManager opens and disposes the device; this class only starts and stops listening.
    public class MidiInputRouter : MonoBehaviour
    {
        InputDevice inputDevice;

        readonly ConcurrentQueue<MidiMessageData> mainThreadQueue = new ConcurrentQueue<MidiMessageData>();
        readonly ConcurrentQueue<Exception> listenerErrors = new ConcurrentQueue<Exception>();

        public event Action<MidiMessageData> MessageReceived;
        public event Action<MidiMessageData> MessageReceivedOnMainThread;

        public bool IsListening => inputDevice != null;

        public void SetInputDevice(InputDevice device)
        {
            ClearInputDevice();

            try
            {
                device.SilentNoteOnPolicy = SilentNoteOnPolicy.NoteOff;
                device.EventReceived += OnEventReceived;

                if (!device.IsListeningForEvents)
                    device.StartEventsListening();

                inputDevice = device;
            }
            catch (Exception ex)
            {
                device.EventReceived -= OnEventReceived;
                LogMan.LogError($"Failed to listen to input {device.Name}: {ex.Message}");
            }
        }

        public void ClearInputDevice()
        {
            if (inputDevice == null) return;

            try
            {
                inputDevice.EventReceived -= OnEventReceived;

                if (inputDevice.IsListeningForEvents)
                    inputDevice.StopEventsListening();
            }
            catch (Exception ex)
            {
                // expected when the device has been unplugged, so only noted in the console
                UnityEngine.Debug.Log($"Input stopped with: {ex.Message}");
            }
            finally
            {
                inputDevice = null;
            }
        }

        // driver thread
        private void OnEventReceived(object sender, MidiEventReceivedEventArgs eventArgs)
        {
            MidiMessageData message = MidiMessageData.FromEvent(eventArgs.Event, Stopwatch.GetTimestamp());

            Action<MidiMessageData> handlers = MessageReceived;

            if (handlers != null)
            {
                try
                {
                    handlers(message);
                }
                catch (Exception ex)
                {
                    listenerErrors.Enqueue(ex);
                }
            }

            mainThreadQueue.Enqueue(message);
        }

        private void Update()
        {
            while (listenerErrors.TryDequeue(out Exception ex))
            {
                LogMan.LogError($"MIDI input listener error: {ex}");
            }

            while (mainThreadQueue.TryDequeue(out MidiMessageData message))
            {
                MessageReceivedOnMainThread?.Invoke(message);
            }
        }

        private void OnDisable()
        {
            ClearInputDevice();
        }
    }
}
