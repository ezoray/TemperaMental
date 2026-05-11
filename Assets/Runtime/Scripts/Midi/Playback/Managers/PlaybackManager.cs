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
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Playbacks
{
    // todo tempo handling needs to be moved into its own class
    public class PlaybackManager : MonoBehaviour
    {
        OutputDevice outputDevice;

        int activateCC;
        int placeCC;
        int removeCC;
        byte clearEmittersValue;
        int emitterCount;
        int gridSize;

        long intervalTicks;
        long frameDurationTicks;
        public bool IsFramePlaybackActive => isFramePlaybackActive;

        ulong[] previousGroups;
        List<ControlChangeEvent> pendingEvents;

        Thread playbackThread;
        ManualResetEventSlim workReady;
        ManualResetEventSlim cancelSignal;
        readonly object pendingLock = new object();
        volatile bool isFramePlaybackActive;
        volatile bool fireCallback;
        volatile bool isRunning;

        public event Action OnFramePlaybackCompleted;

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

            previousGroups = new ulong[emitterCount];
        }

        private void OnEnable()
        {
            isRunning = true;
            workReady = new ManualResetEventSlim(false);
            cancelSignal = new ManualResetEventSlim(false);
            playbackThread = new Thread(ThreadLoop);
            playbackThread.Priority = System.Threading.ThreadPriority.Highest;
            playbackThread.IsBackground = true;
            playbackThread.Start();
        }

        private void SendClearAllEmitters()
        {
            for (byte i = 0; i < emitterCount; i++)
            {
                outputDevice?.SendEvent(CreateCC(activateCC, i));
                outputDevice?.SendEvent(CreateCC(removeCC, clearEmittersValue));
            }

            // reset previousGroups to match
            for (int i = 0; i < emitterCount; i++)
                previousGroups[i] = 0;
        }

        public bool PlayFrame(ulong[] emitterGroups, long frameDurationTicks = 0, bool fireCallback = false)
        {
            if (isFramePlaybackActive) return true;

            this.frameDurationTicks = frameDurationTicks;
            this.fireCallback = fireCallback;

            ulong[] snapshot = new ulong[emitterCount];
            Array.Copy(emitterGroups, snapshot, emitterCount);

            List<ControlChangeEvent> events = BuildFrameEvents(snapshot);

            Array.Copy(snapshot, previousGroups, emitterCount);

            lock(pendingLock)
            {
                pendingEvents = events;
            }

            isFramePlaybackActive = true;
            cancelSignal.Reset();
            workReady.Set();

            return true;
        }

        public void CancelFrame()
        {
            cancelSignal.Set();
        }

        public void SetOutputDevice(OutputDevice device)
        {
            outputDevice = device;
            SendClearAllEmitters();

            onPlaybackReadyStateChanged?.Invoke(true);
        }

        public void ClearOutputDevice()
        {
            CancelFrame();
            outputDevice = null;

            onPlaybackReadyStateChanged?.Invoke(false);
        }

        public void AddEmitter(EmitterDetail emitterDetail)
        {
      //      if (isFramePlaybackActive) return;

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
       //     if (isFramePlaybackActive) return;

            byte pos = (byte)EmitterUtils.PositionToIndex(position);

            for (byte i = 0; i < emitterCount; i++)
                previousGroups[i] &= ~(1UL << pos);

            outputDevice?.SendEvent(CreateCC(removeCC, pos));
        }

        public void SetEmitterType(int emitterId)
        {
            outputDevice?.SendEvent(CreateCC(activateCC, (byte)emitterId));
        }

        private void ThreadLoop()
        {
            while (isRunning)
            {
                workReady.Wait();
                workReady.Reset();

                if (!isRunning) break;

                List<ControlChangeEvent> events;
                lock(pendingLock)
                {
                    events = pendingEvents;
                }

                SendEvents(events);
            }
        }

        private void SendEvents(List<ControlChangeEvent> events)
        {
            Stopwatch frameStopwatch = Stopwatch.StartNew();

            for (int i = 0; i < events.Count; i++)
            {
                if (!isRunning || cancelSignal.IsSet) break;

                long waitUntil = Stopwatch.GetTimestamp() + intervalTicks;
                while (Stopwatch.GetTimestamp() < waitUntil)
                    if (!isRunning || cancelSignal.IsSet) break;

                outputDevice?.SendEvent(events[i]);
            }

            // hold until frame duration has elapsed if set
            if (frameDurationTicks > 0)
            {
                long endTicks = Stopwatch.GetTimestamp() - frameStopwatch.ElapsedTicks + frameDurationTicks;
                long remainingMs = (endTicks - Stopwatch.GetTimestamp()) * 1000 / Stopwatch.Frequency;

                if (remainingMs > 0)
                {
                    cancelSignal.Wait((int)remainingMs);
                }   
            }

            if (!isRunning) return;

            isFramePlaybackActive = false;

            if(fireCallback) OnFramePlaybackCompleted?.Invoke();
        }

        private List<ControlChangeEvent> BuildFrameEvents(ulong[] emitterGroups)
        {
            var events = new List<ControlChangeEvent>();

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

                events.Add(CreateCC(activateCC, emitterId));

                for (byte pos = 0; pos < gridSize; pos++)
                {
                    if ((toRemove & (1UL << pos)) != 0)
                        events.Add(CreateCC(removeCC, pos));
                    else if ((toAdd & (1UL << pos)) != 0)
                        events.Add(CreateCC(placeCC, pos));
                }
            }

            return events;
        }

        private ControlChangeEvent CreateCC(int cc, byte value)
        {
            return new ControlChangeEvent((SevenBitNumber)cc, (SevenBitNumber)value);
        }

        private void OnDisable()
        {
            isRunning = false;
            cancelSignal.Set();
            workReady.Set();
            playbackThread?.Join();
            playbackThread = null;
        }
    }
}