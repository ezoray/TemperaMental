using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Utils;
using TemperaMental.Logs;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiImmediateService : MonoBehaviour
    {
        OutputDevice outputDevice;

        int activateCC;
        int placeCC;
        int removeCC;
        int emitterCount;
        int gridSize;

        long[] eventTimestamps = new long[128]; // set to arbitrary size, greater then ever needed
        long frameStartTimestamp;

        volatile bool playbackFinishedFlag;
        volatile int playedEventCount;
        volatile int totalEventCount;

        PlaybackState playbackState;

        // sub ms time between sending midi events, fast enough to be sent before new frame at max bpm
        // slow enough as to not choke Tempera and midi messages would be lost
        long intervalTicks;
        
        ulong[] previousGroups; // used when comparing new frame data with old, only send out changes

        // double buffer the frame data, one for building in main thread, one for sending in worker thread
        List<ControlChangeEvent> _buildBuffer = new List<ControlChangeEvent>();
        List<ControlChangeEvent> _sendBuffer = new List<ControlChangeEvent>();
        readonly object _lock = new object();

        Thread _workerThread;
        volatile bool _isRunning;
        readonly ManualResetEventSlim _frameReadySignal = new ManualResetEventSlim(false);


        private void OnEnable()
        {
            activateCC = ConfigRegistry.Midi.ActivateCC;
            placeCC = ConfigRegistry.Midi.PlaceCC;
            removeCC = ConfigRegistry.Midi.RemoveCC;
            emitterCount = ConfigRegistry.Grid.EmitterCount;
            gridSize = ConfigRegistry.Grid.GridWidth * ConfigRegistry.Grid.GridHeight;

            previousGroups = new ulong[emitterCount];

            intervalTicks = (long)(Stopwatch.Frequency * ConfigRegistry.Midi.EventIntervalMS);

            // initialise and start the persistent worker thread
            _isRunning = true;
            _workerThread = new Thread(MidiWorkerLoop)
            {
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.AboveNormal,
                Name = "MidiImmediateWorker"
            };
            _workerThread.Start();
        }

        private void Update()
        {
            if (playbackFinishedFlag)
            {
                playbackFinishedFlag = false;
                ProcessDiagnostics();
            }
        }

        public void AddEmitter(EmitterDetail emitterDetail)
        {
            byte emitterId = (byte)emitterDetail.EmitterId;
            byte pos = (byte)EmitterUtils.PositionToIndex(emitterDetail.Position);

            for (byte i = 0; i < emitterCount; i++)
                previousGroups[i] &= ~(1UL << pos);

            previousGroups[emitterId] |= 1UL << pos;

            outputDevice?.SendEvent(CreateCC(activateCC, emitterId));
            outputDevice?.SendEvent(CreateCC(placeCC, pos));
        }

        public void RemoveEmitter(Vector2Int position)
        {
            byte pos = (byte)EmitterUtils.PositionToIndex(position);
            for (byte i = 0; i < emitterCount; i++)
                previousGroups[i] &= ~(1UL << pos);

            outputDevice?.SendEvent(CreateCC(removeCC, pos));
        }

        public void SetEmitterType(int emitterId)
        {
            outputDevice?.SendEvent(CreateCC(activateCC, (byte)emitterId));
        }

        public void SetOutputDevice(OutputDevice device)
        {
            outputDevice = device;
        }

        public void SetPlaybackState(PlaybackState state)
        {
            playbackState = state;
        }

        public void ClearOutputDevice()
        {
            outputDevice = null;
        }

        public void SendFrame(ulong[] emitterGroups)
        {
            if (playbackState == PlaybackState.Playing || playbackState == PlaybackState.Paused) return;

            // check if worker is still busy with the previous frame
            if (totalEventCount > 0 && playedEventCount < totalEventCount)
            {
                LogMan.LogWarning($"Frame overlap! Busy sending {playedEventCount}/{totalEventCount}");
            }

            // build list of events to send
            BuildFrameEvents(emitterGroups);
            if (_buildBuffer.Count == 0) return;

            // replace old comparison data with current
            Array.Copy(emitterGroups, previousGroups, emitterCount);

            playedEventCount = 0;
            totalEventCount = _buildBuffer.Count;
            frameStartTimestamp = Stopwatch.GetTimestamp();

            // switch buffers and wake thread
            lock (_lock)
            {
                var temp = _sendBuffer;
                _sendBuffer = _buildBuffer;
                _buildBuffer = temp;

                _frameReadySignal.Set();
            }
        }

        private void MidiWorkerLoop()
        {
            while (_isRunning)
            {
                // wait for SendFrame to signal start
                _frameReadySignal.Wait();
                _frameReadySignal.Reset();

                if (!_isRunning) break;

                // snapshot reference of events to send
                List<ControlChangeEvent> changeEvents;
                lock (_lock)
                {
                    changeEvents = _sendBuffer;
                }

                for (int i = 0; i < changeEvents.Count; i++)
                {
                    if (!_isRunning) break;

                    // spin-wait cycle sends events at set intervals
                    long waitUntil = Stopwatch.GetTimestamp() + intervalTicks;
                    while (Stopwatch.GetTimestamp() < waitUntil)
                    {
                        if (!_isRunning) break;
                    }

                    outputDevice?.SendEvent(changeEvents[i]);

                    // log for diagnostics
                    if (playedEventCount < eventTimestamps.Length)
                    {
                        eventTimestamps[playedEventCount] = Stopwatch.GetTimestamp();
                    }
                    playedEventCount++;
                }

                playbackFinishedFlag = true;
            }
        }

        private void BuildFrameEvents(ulong[] emitterGroups)
        {
            _buildBuffer.Clear();

            ulong allAdds = 0;
            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
                allAdds |= emitterGroups[emitterId] & ~previousGroups[emitterId];

            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                ulong current = emitterGroups[emitterId];
                ulong previous = previousGroups[emitterId];

                ulong toRemove = (previous & ~current) & ~allAdds;
                ulong toAdd = current & ~previous;

                if (toRemove == 0 && toAdd == 0) continue;

                _buildBuffer.Add(CreateCC(activateCC, emitterId));

                for (byte pos = 0; pos < gridSize; pos++)
                {
                    if ((toRemove & (1UL << pos)) != 0)
                        _buildBuffer.Add(CreateCC(removeCC, pos));
                    else if ((toAdd & (1UL << pos)) != 0)
                        _buildBuffer.Add(CreateCC(placeCC, pos));
                }
            }
        }

        private ControlChangeEvent CreateCC(int cc, byte value)
        {
            return new ControlChangeEvent((SevenBitNumber)cc, (SevenBitNumber)value);
        }

        // log useful test data
        private void ProcessDiagnostics()
        {
            double ticksPerMs = Stopwatch.Frequency / 1000.0;
            int played = playedEventCount;
            if (played == 0) return;

            double sendStartLatency = (eventTimestamps[0] - frameStartTimestamp) / ticksPerMs;
            LogMan.Log($"Start → first event: {sendStartLatency:F3}ms");

            for (int i = 1; i < played; i++)
            {
                double delta = (eventTimestamps[i] - eventTimestamps[i - 1]) / ticksPerMs;
                LogMan.Log($"Event {i - 1} → {i}: {delta:F3}ms");
            }

            double totalDuration = (eventTimestamps[Math.Min(played - 1, eventTimestamps.Length - 1)] - frameStartTimestamp) / ticksPerMs;
            LogMan.Log($"Total frame send duration: {totalDuration:F3}ms");
        }

        private void OnDisable()
        {
            LogMan.Log("OnDisable");

            _isRunning = false;
            _frameReadySignal.Set();
            if (_workerThread != null && _workerThread.IsAlive)
                _workerThread.Join(50);

            outputDevice = null;
        }
    }
}