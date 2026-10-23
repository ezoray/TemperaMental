using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using TemperaMental.Applications.Config;
using TemperaMental.Logs;
using TemperaMental.Midi.Output;
using UnityEngine;

namespace TemperaMental.Midi.Clock
{
    // Internal clock running on a dedicated high-priority thread.
    // Keeps a transport position in ticks (TicksPerBeat per beat) and raises OnBeat at every beat boundary.
    // Between beats it pumps the MidiOutputScheduler, so all MIDI output is sent from this one thread.
    // OnBeat handlers run on the clock thread: keep them short and never touch Unity objects from them.
    public class ClockEngine : MonoBehaviour
    {
        // after a long stall (e.g. a debugger pause) catch up at most this many steps, then jump ahead
        const int MaxCatchUpSteps = 4;

        [SerializeField] MidiOutputScheduler outputScheduler;

        int ticksPerBeat;
        int stepTicks;
        long spinThresholdTicks;
        double timestampFrequency;

        // written by the main thread, read by the clock thread
        volatile int requestedBpm;
        long requestedEpoch;
        volatile bool isRunning;

        // owned by the clock thread
        int appliedBpm;
        long currentEpoch = -1;
        long anchorTime;
        double anchorTick;
        double ticksPerTimestamp;
        long lastStepTick;
        long nextStepTime;

        Thread clockThread;
        AutoResetEvent wakeSignal;

        // listener exceptions are logged on the main thread
        readonly ConcurrentQueue<Exception> listenerErrors = new ConcurrentQueue<Exception>();

        public event Action<ClockBeat> OnBeat;

        public int TicksPerBeat => ticksPerBeat;

        private void Awake()
        {
            ticksPerBeat = ConfigRegistry.Midi.TicksPerBeat;

            // only beats are needed for now; sending MIDI clock will step every pulse (TicksPerBeat / 24)
            stepTicks = ticksPerBeat;

            timestampFrequency = Stopwatch.Frequency;
            spinThresholdTicks = (long)(Stopwatch.Frequency * ConfigRegistry.Midi.ClockSpinThresholdMs / 1000.0);

            requestedBpm = ConfigRegistry.Midi.DefaultBpm;
        }

        private void OnEnable()
        {
            wakeSignal = new AutoResetEvent(false);
            outputScheduler.SetWakeHandle(wakeSignal);

            isRunning = true;
            clockThread = new Thread(ThreadLoop)
            {
                Name = "TemperaMental Clock",
                Priority = System.Threading.ThreadPriority.Highest,
                IsBackground = true
            };
            clockThread.Start();
        }

        private void Update()
        {
            while (listenerErrors.TryDequeue(out Exception ex))
            {
                LogMan.LogError($"Clock listener error: {ex}");
            }
        }

        public void SetBpm(int bpm)
        {
            if (bpm <= 0) return;

            requestedBpm = bpm;
            wakeSignal?.Set();
        }

        // restarts the transport at tick 0 and raises beat 0 on the clock thread straight away.
        // returns the new epoch; beats raised before the restart takes effect carry an older epoch
        public long Restart()
        {
            long epoch = Interlocked.Increment(ref requestedEpoch);
            wakeSignal?.Set();

            return epoch;
        }

        private void ThreadLoop()
        {
            while (isRunning)
            {
                long now = Stopwatch.GetTimestamp();

                ApplyRequests(now);

                if (now >= nextStepTime)
                {
                    DispatchDueSteps(now);
                    continue;
                }

                if (outputScheduler.TrySendNext(now))
                    continue;

                WaitForNextWork();
            }
        }

        private void WaitForNextWork()
        {
            long target = nextStepTime;

            if (outputScheduler.HasWork)
                target = Math.Min(target, outputScheduler.NextSendTime);

            long remaining = target - Stopwatch.GetTimestamp();

            if (remaining <= 0) return;

            // sleep for most of the wait, then spin for the last part for accuracy
            if (remaining > spinThresholdTicks)
            {
                int sleepMs = (int)((remaining - spinThresholdTicks) * 1000 / Stopwatch.Frequency);

                if (sleepMs > 0)
                {
                    wakeSignal.WaitOne(sleepMs);
                    return;
                }
            }

            Thread.SpinWait(20);
        }

        private void ApplyRequests(long now)
        {
            long epoch = Interlocked.Read(ref requestedEpoch);

            if (epoch != currentEpoch)
            {
                currentEpoch = epoch;
                SetTempo(requestedBpm);

                anchorTime = now;
                anchorTick = 0;
                lastStepTick = 0;

                RaiseStep(0, now);

                nextStepTime = TimeOfTick(lastStepTick + stepTicks);
                return;
            }

            int bpm = requestedBpm;

            if (bpm != appliedBpm)
            {
                // re-anchor at the current position so the change applies from now on
                anchorTick = TickAt(now);
                anchorTime = now;
                SetTempo(bpm);

                nextStepTime = TimeOfTick(lastStepTick + stepTicks);
            }
        }

        private void DispatchDueSteps(long now)
        {
            long stepTime = TimeOfTick(lastStepTick + stepTicks);
            long stepDuration = Math.Max(1, (long)(stepTicks / ticksPerTimestamp));

            long lateness = now - stepTime;

            if (lateness > stepDuration * MaxCatchUpSteps)
            {
                lastStepTick += lateness / stepDuration * stepTicks;
                stepTime = TimeOfTick(lastStepTick + stepTicks);
            }

            while (stepTime <= now)
            {
                lastStepTick += stepTicks;
                RaiseStep(lastStepTick, stepTime);
                stepTime = TimeOfTick(lastStepTick + stepTicks);
            }

            nextStepTime = stepTime;
        }

        private void RaiseStep(long tick, long timestamp)
        {
            if (tick % ticksPerBeat != 0) return;

            Action<ClockBeat> handlers = OnBeat;

            if (handlers == null) return;

            ClockBeat beat = new ClockBeat(currentEpoch, tick / ticksPerBeat, timestamp);

            // call each listener separately so one failing doesn't stop the others
            foreach (Action<ClockBeat> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(beat);
                }
                catch (Exception ex)
                {
                    listenerErrors.Enqueue(ex);
                }
            }
        }

        private void SetTempo(int bpm)
        {
            appliedBpm = bpm;
            ticksPerTimestamp = bpm / 60.0 * ticksPerBeat / timestampFrequency;
        }

        private double TickAt(long timestamp)
        {
            return anchorTick + (timestamp - anchorTime) * ticksPerTimestamp;
        }

        private long TimeOfTick(double tick)
        {
            return anchorTime + (long)Math.Round((tick - anchorTick) / ticksPerTimestamp);
        }

        private void OnDisable()
        {
            isRunning = false;
            wakeSignal?.Set();

            clockThread?.Join();
            clockThread = null;

            outputScheduler.SetWakeHandle(null);
            wakeSignal?.Dispose();
            wakeSignal = null;
        }
    }
}
