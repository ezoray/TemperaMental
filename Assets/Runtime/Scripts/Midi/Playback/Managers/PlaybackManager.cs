using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
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
        ulong[] frameSnapshot;
        List<ControlChangeEvent> frameEvents;
        List<ControlChangeEvent> pendingEvents;
        ControlChangeEvent[] ccPool;
        int ccPoolIndex;

        Thread playbackThread;
        ManualResetEventSlim workReady;
        ManualResetEventSlim cancelSignal;
        ManualResetEventSlim durationChangedSignal;
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
            frameSnapshot = new ulong[emitterCount];
            frameEvents = new List<ControlChangeEvent>(68);

            ccPool = new ControlChangeEvent[68];
            for (int i = 0; i < ccPool.Length; i++)
                ccPool[i] = new ControlChangeEvent((SevenBitNumber)0, (SevenBitNumber)0);
        }

        private void OnEnable()
        {
            isRunning = true;
            workReady = new ManualResetEventSlim(false);
            cancelSignal = new ManualResetEventSlim(false);
            durationChangedSignal = new ManualResetEventSlim(false);
            playbackThread = new Thread(ThreadLoop);
            playbackThread.Priority = System.Threading.ThreadPriority.Highest;
            playbackThread.IsBackground = true;
            playbackThread.Start();
        }

        private void SendClearAllEmitters()
        {
            ccPoolIndex = 0;

            for (byte i = 0; i < emitterCount; i++)
            {
                outputDevice?.SendEvent(PooledCC(activateCC, i));
                outputDevice?.SendEvent(PooledCC(removeCC, clearEmittersValue));
            }

            for (int i = 0; i < emitterCount; i++)
                previousGroups[i] = 0;
        }

        public bool PlayFrame(ulong[] emitterGroups, long duration = 0, bool fireCallback = false)
        {
            if (isFramePlaybackActive) return true;

            Interlocked.Exchange(ref frameDurationTicks, duration);
            this.fireCallback = fireCallback;

            Array.Copy(emitterGroups, frameSnapshot, emitterCount);

            BuildFrameEvents(frameSnapshot);

            Array.Copy(frameSnapshot, previousGroups, emitterCount);

            lock (pendingLock)
            {
                pendingEvents = frameEvents;
            }

            isFramePlaybackActive = true;
            cancelSignal.Reset();
            workReady.Set();

            return true;
        }

        public void NotifyDurationChanged(long duration)
        {
            LogMan.Log("NotifyDurationChanged");

            Interlocked.Exchange(ref frameDurationTicks, duration);
            durationChangedSignal.Set();
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
            byte emitterId = (byte)emitterDetail.EmitterId;
            byte pos = (byte)EmitterUtils.PositionToIndex(emitterDetail.Position);

            for (byte i = 0; i < emitterCount; i++)
                previousGroups[i] &= ~(1UL << pos);
            previousGroups[emitterId] |= 1UL << pos;

            outputDevice?.SendEvent(new ControlChangeEvent((SevenBitNumber)activateCC, (SevenBitNumber)emitterId));
            outputDevice?.SendEvent(new ControlChangeEvent((SevenBitNumber)placeCC, (SevenBitNumber)pos));
        }

        public void RemoveEmitter(Vector2Int position)
        {
            byte pos = (byte)EmitterUtils.PositionToIndex(position);

            for (byte i = 0; i < emitterCount; i++)
                previousGroups[i] &= ~(1UL << pos);

            outputDevice?.SendEvent(new ControlChangeEvent((SevenBitNumber)removeCC, (SevenBitNumber)pos));
        }

        public void SetEmitterType(int emitterId)
        {
            outputDevice?.SendEvent(new ControlChangeEvent((SevenBitNumber)activateCC, (SevenBitNumber)emitterId));
        }

        private void ThreadLoop()
        {
            while (isRunning)
            {
                workReady.Wait();
                workReady.Reset();

                if (!isRunning) break;

                List<ControlChangeEvent> events;
                lock (pendingLock)
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

            if (frameDurationTicks > 0)
            {
                long frameStartTicks = Stopwatch.GetTimestamp() - frameStopwatch.ElapsedTicks;

                while (true)
                {
                    long now = Stopwatch.GetTimestamp();
                    long remainingMs = (frameStartTicks + frameDurationTicks - now) * 1000 / Stopwatch.Frequency;

                    if (remainingMs <= 0) break;

                    int result = WaitHandle.WaitAny(new[] { cancelSignal.WaitHandle, durationChangedSignal.WaitHandle }, (int)remainingMs);
                    durationChangedSignal.Reset();

                    if (result == 0 || !isRunning) break; // cancelled
                }
            }

            if (!isRunning) return;

            isFramePlaybackActive = false;

            if (fireCallback) OnFramePlaybackCompleted?.Invoke();
        }

        private void BuildFrameEvents(ulong[] emitterGroups)
        {
            frameEvents.Clear();
            ccPoolIndex = 0;

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

                frameEvents.Add(PooledCC(activateCC, emitterId));

                for (byte pos = 0; pos < gridSize; pos++)
                {
                    if ((toRemove & (1UL << pos)) != 0)
                        frameEvents.Add(PooledCC(removeCC, pos));
                    else if ((toAdd & (1UL << pos)) != 0)
                        frameEvents.Add(PooledCC(placeCC, pos));
                }
            }
        }

        private ControlChangeEvent PooledCC(int cc, byte value)
        {
            var e = ccPool[ccPoolIndex++];
            e.ControlNumber = (SevenBitNumber)cc;
            e.ControlValue = (SevenBitNumber)value;
            return e;
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