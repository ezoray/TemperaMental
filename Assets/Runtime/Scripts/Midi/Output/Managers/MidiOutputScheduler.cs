using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Applications.Config;
using TemperaMental.Midi.Clock;
using TemperaMental.Midi.Core;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Output
{
    // The single path for all MIDI sent to the output device.
    // Any thread can queue commands; only the clock thread (ClockEngine) sends them, spaced EventIntervalMS apart.
    // Frames are sent as a diff against what the device currently shows, and back-to-back frame commands are
    // collapsed so only the newest one is sent.
    public class MidiOutputScheduler : MonoBehaviour
    {
        // 4 emitter selects + 64 cells is the largest frame diff
        const int MaxBurstEvents = 68;

        enum CommandType
        {
            Frame,
            AddEmitter,
            RemoveEmitter,
            SelectEmitter,
            ResetDevice
        }

        readonly struct OutputCommand
        {
            public readonly CommandType Type;
            public readonly ulong[] Groups;
            public readonly byte EmitterId;
            public readonly byte CellIndex;

            public OutputCommand(CommandType type, ulong[] groups = null, byte emitterId = 0, byte cellIndex = 0)
            {
                Type = type;
                Groups = groups;
                EmitterId = emitterId;
                CellIndex = cellIndex;
            }
        }

        int activateCC;
        int placeCC;
        int removeCC;
        byte clearEmittersValue;
        int emitterCount;
        int gridSize;
        long intervalTicks;

        // any IOutputDevice, so tests can capture what would be sent without a real MIDI port
        volatile IOutputDevice outputDevice;

        // clock and transport messages go to the Out device, a separate Clock to device, or nowhere
        volatile IOutputDevice clockOutputDevice;
        volatile int clockDestination;

        volatile byte midiChannel;
        volatile bool isMidiChannelSet;
        volatile EventWaitHandle wakeHandle;

        readonly ConcurrentQueue<OutputCommand> commands = new ConcurrentQueue<OutputCommand>();

        // owned by the clock thread
        ulong[] deviceGroups;
        ControlChangeEvent[] burst;
        int burstCount;
        int burstIndex;
        long nextSendTime;

        // realtime messages carry no data, so single instances are reused
        readonly TimingClockEvent timingClockEvent = new TimingClockEvent();
        readonly StartEvent startEvent = new StartEvent();
        readonly ContinueEvent continueEvent = new ContinueEvent();
        readonly StopEvent stopEvent = new StopEvent();

        // called on the clock thread after every message sent (used by the MIDI monitor)
        public event Action<MidiMessageData> MessageSent;

        [SerializeField] UnityEvent<bool> onPlaybackReadyStateChanged;

        private void Awake()
        {
            activateCC = ConfigRegistry.Midi.ActivateCC;
            placeCC = ConfigRegistry.Midi.PlaceCC;
            removeCC = ConfigRegistry.Midi.RemoveCC;
            clearEmittersValue = ConfigRegistry.Midi.ClearEmittersValue;
            emitterCount = ConfigRegistry.Grid.EmitterCount;
            gridSize = ConfigRegistry.Grid.GridWidth * ConfigRegistry.Grid.GridHeight;
            intervalTicks = (long)(Stopwatch.Frequency * ConfigRegistry.Midi.EventIntervalMS);

            // the saved channel may already have been set by MidiSettingsManager, whose Awake can run first
            if (!isMidiChannelSet)
                midiChannel = (byte)(ConfigRegistry.Midi.DefaultMidiChannel - 1);

            deviceGroups = new ulong[emitterCount];

            burst = new ControlChangeEvent[MaxBurstEvents];
            for (int i = 0; i < burst.Length; i++)
            {
                burst[i] = new ControlChangeEvent((SevenBitNumber)0, (SevenBitNumber)0);
            }
        }

        // the clock engine passes its wake signal so queued commands are sent without waiting for the next beat
        public void SetWakeHandle(EventWaitHandle handle)
        {
            wakeHandle = handle;
        }

        public void SetMidiChannel(int channel)
        {
            midiChannel = (byte)(channel - 1);
            isMidiChannelSet = true;
        }

        public void SetOutputDevice(IOutputDevice device)
        {
            outputDevice = device;
            Enqueue(new OutputCommand(CommandType.ResetDevice));

            onPlaybackReadyStateChanged?.Invoke(true);
        }

        public void SetClockDestination(ClockDestination destination, IOutputDevice separateDevice)
        {
            clockOutputDevice = destination == ClockDestination.Separate ? separateDevice : null;
            clockDestination = (int)destination;
        }

        public void ClearOutputDevice()
        {
            outputDevice = null;

            while (commands.TryDequeue(out _)) { }

            onPlaybackReadyStateChanged?.Invoke(false);
        }

        // the groups are copied, so callers can pass live arrays
        public void EnqueueFrame(ulong[] emitterGroups)
        {
            Enqueue(new OutputCommand(CommandType.Frame, (ulong[])emitterGroups.Clone()));
        }

        public void EnqueueAddEmitter(int emitterId, int cellIndex)
        {
            Enqueue(new OutputCommand(CommandType.AddEmitter, emitterId: (byte)emitterId, cellIndex: (byte)cellIndex));
        }

        public void EnqueueRemoveEmitter(int cellIndex)
        {
            Enqueue(new OutputCommand(CommandType.RemoveEmitter, cellIndex: (byte)cellIndex));
        }

        public void EnqueueSelectEmitter(int emitterId)
        {
            Enqueue(new OutputCommand(CommandType.SelectEmitter, emitterId: (byte)emitterId));
        }

        private void Enqueue(OutputCommand command)
        {
            commands.Enqueue(command);
            wakeHandle?.Set();
        }

        // --- clock thread only below ---

        public bool HasWork => burstIndex < burstCount || !commands.IsEmpty;

        public long NextSendTime => nextSendTime;

        // sends the next due message; returns false if nothing was sent
        public bool TrySendNext(long now)
        {
            if (burstIndex >= burstCount && !BuildNextBurst()) return false;

            if (now < nextSendTime) return false;

            ControlChangeEvent controlChange = burst[burstIndex++];

            if (Send(controlChange))
            {
                MessageSent?.Invoke(new MidiMessageData(Stopwatch.GetTimestamp(), MidiMessageKind.ControlChange,
                    controlChange.Channel, controlChange.ControlNumber, controlChange.ControlValue));
            }

            nextSendTime = now + intervalTicks;

            return true;
        }

        // sends a clock or transport message straight away, ahead of any queued CCs.
        // only TimingClock, Start, Continue and Stop are supported
        public void SendRealtime(MidiMessageKind kind)
        {
            MidiEvent realtimeEvent = kind switch
            {
                MidiMessageKind.TimingClock => timingClockEvent,
                MidiMessageKind.Start => startEvent,
                MidiMessageKind.Continue => continueEvent,
                MidiMessageKind.Stop => stopEvent,
                _ => null
            };

            if (realtimeEvent == null) return;

            bool isSent = clockDestination switch
            {
                (int)ClockDestination.SameAsOut => Send(realtimeEvent),
                (int)ClockDestination.Separate => SendToClockOutput(realtimeEvent),
                _ => false
            };

            if (isSent)
            {
                MessageSent?.Invoke(new MidiMessageData(Stopwatch.GetTimestamp(), kind));
            }
        }

        private bool Send(MidiEvent midiEvent)
        {
            IOutputDevice device = outputDevice;

            if (device == null) return false;

            try
            {
                device.SendEvent(midiEvent);
                return true;
            }
            catch (ObjectDisposedException)
            {
                // only drop the device we failed on, not one selected in the meantime
                Interlocked.CompareExchange(ref outputDevice, null, device);
            }
            catch (MidiDeviceException)
            {
                Interlocked.CompareExchange(ref outputDevice, null, device);
            }

            return false;
        }

        private bool SendToClockOutput(MidiEvent midiEvent)
        {
            IOutputDevice device = clockOutputDevice;

            if (device == null) return false;

            try
            {
                device.SendEvent(midiEvent);
                return true;
            }
            catch (ObjectDisposedException)
            {
                Interlocked.CompareExchange(ref clockOutputDevice, null, device);
            }
            catch (MidiDeviceException)
            {
                Interlocked.CompareExchange(ref clockOutputDevice, null, device);
            }

            return false;
        }

        private bool BuildNextBurst()
        {
            burstCount = 0;
            burstIndex = 0;

            while (burstCount == 0 && commands.TryDequeue(out OutputCommand command))
            {
                switch (command.Type)
                {
                    case CommandType.Frame:
                        // a newer frame is already waiting, so skip straight to it
                        if (commands.TryPeek(out OutputCommand next) && next.Type == CommandType.Frame) continue;
                        BuildFrameDiff(command.Groups);
                        break;

                    case CommandType.AddEmitter:
                        BuildAddEmitter(command.EmitterId, command.CellIndex);
                        break;

                    case CommandType.RemoveEmitter:
                        BuildRemoveEmitter(command.CellIndex);
                        break;

                    case CommandType.SelectEmitter:
                        AddToBurst(activateCC, command.EmitterId);
                        break;

                    case CommandType.ResetDevice:
                        BuildResetDevice();
                        break;
                }
            }

            return burstCount > 0;
        }

        private void BuildFrameDiff(ulong[] emitterGroups)
        {
            // a cell taken over by another emitter is simply placed; removing it first is unnecessary
            ulong allAdds = 0;
            for (int emitterId = 0; emitterId < emitterCount; emitterId++)
                allAdds |= emitterGroups[emitterId] & ~deviceGroups[emitterId];

            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                ulong current = emitterGroups[emitterId];
                ulong previous = deviceGroups[emitterId];

                ulong toRemove = (previous & ~current) & ~allAdds;
                ulong toAdd = current & ~previous;

                if (toRemove == 0 && toAdd == 0) continue;

                AddToBurst(activateCC, emitterId);

                for (byte pos = 0; pos < gridSize; pos++)
                {
                    if ((toRemove & (1UL << pos)) != 0)
                        AddToBurst(removeCC, pos);
                    else if ((toAdd & (1UL << pos)) != 0)
                        AddToBurst(placeCC, pos);
                }
            }

            Array.Copy(emitterGroups, deviceGroups, emitterCount);
        }

        private void BuildAddEmitter(byte emitterId, byte cellIndex)
        {
            ulong bit = 1UL << cellIndex;

            for (int i = 0; i < emitterCount; i++)
                deviceGroups[i] &= ~bit;

            deviceGroups[emitterId] |= bit;

            AddToBurst(activateCC, emitterId);
            AddToBurst(placeCC, cellIndex);
        }

        private void BuildRemoveEmitter(byte cellIndex)
        {
            ulong bit = 1UL << cellIndex;

            for (int i = 0; i < emitterCount; i++)
                deviceGroups[i] &= ~bit;

            AddToBurst(removeCC, cellIndex);
        }

        private void BuildResetDevice()
        {
            for (byte i = 0; i < emitterCount; i++)
            {
                deviceGroups[i] = 0;

                AddToBurst(activateCC, i);
                AddToBurst(removeCC, clearEmittersValue);
            }
        }

        private void AddToBurst(int cc, byte value)
        {
            ControlChangeEvent controlChange = burst[burstCount++];
            controlChange.ControlNumber = (SevenBitNumber)cc;
            controlChange.ControlValue = (SevenBitNumber)value;
            controlChange.Channel = (FourBitNumber)midiChannel;
        }
    }
}
