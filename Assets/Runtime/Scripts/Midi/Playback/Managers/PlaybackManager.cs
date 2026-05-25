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

        const int EventAllocation = 68;

        int activateCC;
        int placeCC;
        int removeCC;
        byte clearEmittersValue;
        int emitterCount;
        int gridSize;

        long intervalTicks;
        long frameDurationTicks;

        // Absolute Stopwatch deadline for the current frame's hold period.
        // Set in PlayFrame on the main thread before signalling the background thread,
        // so the hold ends at a fixed point in time regardless of thread wakeup latency.
        long frameDeadlineTicks;

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

        // Pre-allocated to avoid per-iteration heap allocation in the duration hold loop.
        // Rebuilt in OnEnable after signals are recreated.
        WaitHandle[] waitHandles;

        readonly object pendingLock = new object();

        public volatile bool isPlaybackActive;
        volatile bool firePlaybackCompleteCallback;
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
            frameEvents = new List<ControlChangeEvent>(EventAllocation);

            ccPool = new ControlChangeEvent[EventAllocation];
            for (int i = 0; i < ccPool.Length; i++)
                ccPool[i] = new ControlChangeEvent((SevenBitNumber)0, (SevenBitNumber)0);
        }

        private void OnEnable()
        {
            isRunning = true;
            workReady = new ManualResetEventSlim(false);
            cancelSignal = new ManualResetEventSlim(false);
            durationChangedSignal = new ManualResetEventSlim(false);

            // Rebuild after signal recreation — WaitHandle references must stay current.
            waitHandles = new WaitHandle[] { cancelSignal.WaitHandle, durationChangedSignal.WaitHandle };

            playbackThread = new Thread(ThreadLoop);
            playbackThread.Priority = System.Threading.ThreadPriority.Highest;
            playbackThread.IsBackground = true;
            playbackThread.Start();
        }

        public bool PlayFrame(ulong[] emitterGroups, long duration = 0, bool fireCallback = false)
        {
            if (isPlaybackActive) return false;

            Interlocked.Exchange(ref frameDurationTicks, duration);
            firePlaybackCompleteCallback = fireCallback;

            // Anchor the deadline to now, before signalling the thread.
            // This absorbs thread wakeup latency into the hold period rather than
            // letting it accumulate as drift across frames.
            Interlocked.Exchange(ref frameDeadlineTicks, Stopwatch.GetTimestamp() + duration);

            Array.Copy(emitterGroups, frameSnapshot, emitterCount);

            BuildFrameEvents(frameSnapshot);

            Array.Copy(frameSnapshot, previousGroups, emitterCount);

            lock (pendingLock)
            {
                pendingEvents = frameEvents;
            }

            isPlaybackActive = true;
            cancelSignal.Reset();
            workReady.Set();

            return true;
        }

        public void NotifyDurationChanged(long newDurationTicks)
        {
            // Shift the existing deadline by the delta so an in-flight BPM change
            // extends or contracts the current frame's remaining hold proportionally,
            // rather than snapping to an unanchored absolute value.
            long oldDurationTicks = Interlocked.Exchange(ref frameDurationTicks, newDurationTicks);
            long delta = newDurationTicks - oldDurationTicks;
            Interlocked.Add(ref frameDeadlineTicks, delta);
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
            for (int i = 0; i < events.Count; i++)
            {
                if (!isRunning) break;

                long waitUntil = Stopwatch.GetTimestamp() + intervalTicks;

                while (Stopwatch.GetTimestamp() < waitUntil)
                {
                    if (!isRunning) break;
                }

                try
                {
                    outputDevice?.SendEvent(events[i]);
                }
                catch (ObjectDisposedException)
                {
                    outputDevice = null;
                    break;
                }
                catch (MidiDeviceException e)
                {
                    outputDevice = null;
                    break;
                }
            }

            if (frameDurationTicks > 0)
            {
                // Hold until the pre-anchored deadline rather than computing from now,
                // so thread wakeup jitter doesn't accumulate into frame timing drift.
                //
                // Hybrid sleep/spin: WaitAny handles the bulk of the wait efficiently,
                // then we spin the final 2ms to avoid OS scheduler wakeup latency
                // (~1ms on macOS) causing the callback to fire late each frame.
                long spinThresholdTicks = Stopwatch.Frequency * 2 / 1000;

                while (true)
                {
                    if (cancelSignal.IsSet)
                    {
                        cancelSignal.Reset();
                        break;
                    }
                    if (!isRunning) break;

                    long remaining = Interlocked.Read(ref frameDeadlineTicks) - Stopwatch.GetTimestamp();

                    if (remaining <= 0) break;

                    if (remaining > spinThresholdTicks)
                    {
                        long sleepMs = (remaining - spinThresholdTicks) * 1000 / Stopwatch.Frequency;
                        int result = WaitHandle.WaitAny(waitHandles, (int)sleepMs);
                        durationChangedSignal.Reset();

                        if (result == 0 || !isRunning) break; // cancelled
                        // result == 1: durationChangedSignal — deadline already updated by
                        // NotifyDurationChanged, loop recalculates remaining against new deadline.
                        // result == WaitHandle.WaitTimeout: fell through naturally, continue to spin.
                    }
                    // else: within spin window — tight loop until deadline
                }
            }

            if (!isRunning) return;

            isPlaybackActive = false;

            if (firePlaybackCompleteCallback) OnFramePlaybackCompleted?.Invoke();
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