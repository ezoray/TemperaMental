using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using TemperaMental.Midi.Core;
using TemperaMental.Midi.Output;
using UnityEngine;

namespace TemperaMental.Midi.Clock
{
    // Internal clock running on a dedicated high-priority thread.
    // Keeps a transport position in ticks (TicksPerBeat per beat) and raises OnBeat at every beat boundary.
    // In Send mode it also steps every MIDI clock pulse (TicksPerBeat / PulsesPerBeat ticks) and sends F8, plus
    // Start/Continue/Stop when enabled. Otherwise it only wakes for beats, which costs less CPU.
    // Between steps it pumps the MidiOutputScheduler, so all MIDI output is sent from this one thread.
    // OnBeat handlers run on the clock thread: keep them short and never touch Unity objects from them.
    public class ClockEngine : MonoBehaviour
    {
        // after a long stall (e.g. a debugger pause) catch up at most this many steps, then jump ahead
        const int MaxCatchUpSteps = 4;

        [SerializeField] MidiOutputScheduler outputScheduler;

        int ticksPerBeat;
        int ticksPerPulse;
        long spinThresholdTicks;
        double timestampFrequency;

        // written by the main thread, read by the clock thread
        volatile int requestedBpm;
        volatile int clockMode;
        volatile bool isTransportSent = true;
        long requestedEpoch;
        int pendingRestartTransport;
        volatile bool isRunning;

        readonly ConcurrentQueue<ClockTransport> transportQueue = new ConcurrentQueue<ClockTransport>();

        // owned by the clock thread
        int appliedBpm;
        int appliedStepTicks;
        long currentEpoch = -1;
        long anchorTime;
        double anchorTick;
        double ticksPerTimestamp;
        long lastStepTick;
        long nextStepTime;

        Thread clockThread;
        AutoResetEvent wakeSignal;

        // punctuality, written by the clock thread and taken by anyone
        long statSteps;
        long statLateSteps;
        long statMaxLateTicks;
        long lateThresholdTicks;

        // listener exceptions are logged on the main thread
        readonly ConcurrentQueue<Exception> listenerErrors = new ConcurrentQueue<Exception>();

        public event Action<ClockBeat> OnBeat;

        public int TicksPerBeat => ticksPerBeat;

        public ClockMode Mode => (ClockMode)clockMode;

        private bool IsSendingClock => clockMode == (int)ClockMode.Send;

        // pulses are only needed while sending clock
        private int StepTicks => IsSendingClock ? ticksPerPulse : ticksPerBeat;

        private void Awake()
        {
            ticksPerBeat = ConfigRegistry.Midi.TicksPerBeat;
            ticksPerPulse = Math.Max(1, ticksPerBeat / ConfigRegistry.Midi.PulsesPerBeat);

            timestampFrequency = Stopwatch.Frequency;
            spinThresholdTicks = (long)(Stopwatch.Frequency * ConfigRegistry.Midi.ClockSpinThresholdMs / 1000.0);
            lateThresholdTicks = Stopwatch.Frequency / 1000;

            requestedBpm = ConfigRegistry.Midi.DefaultBpm;
        }

        private void OnEnable()
        {
            PlatformTimingActivity.Begin("TemperaMental MIDI clock");

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

        public void SetClockMode(ClockMode mode)
        {
            // following an external clock arrives in a later phase; until then it behaves as Internal
            clockMode = (int)mode;
            wakeSignal?.Set();
        }

        // whether Start/Continue/Stop are sent along with the clock
        public void SetTransportSent(bool isSent)
        {
            isTransportSent = isSent;
        }

        // restarts the transport at tick 0 and raises beat 0 on the clock thread straight away.
        // when sending clock, the transport message (Start or Continue) goes out just before the first pulse.
        // returns the new epoch; beats raised before the restart takes effect carry an older epoch
        public long Restart(ClockTransport transport = ClockTransport.None)
        {
            Interlocked.Exchange(ref pendingRestartTransport, (int)transport);
            long epoch = Interlocked.Increment(ref requestedEpoch);
            wakeSignal?.Set();

            return epoch;
        }

        // sends a transport message (e.g. Stop) as soon as possible without restarting the clock
        public void SendTransport(ClockTransport transport)
        {
            if (transport == ClockTransport.None) return;

            transportQueue.Enqueue(transport);
            wakeSignal?.Set();
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
                appliedStepTicks = StepTicks;

                var transport = (ClockTransport)Interlocked.Exchange(ref pendingRestartTransport, 0);
                SendTransportMessage(transport);

                RaiseStep(0, now);

                nextStepTime = TimeOfTick(NextStepTick(lastStepTick));
            }

            while (transportQueue.TryDequeue(out ClockTransport transport))
            {
                SendTransportMessage(transport);
            }

            int bpm = requestedBpm;

            if (bpm != appliedBpm)
            {
                // re-anchor at the current position so the change applies from now on
                anchorTick = TickAt(now);
                anchorTime = now;
                SetTempo(bpm);

                nextStepTime = TimeOfTick(NextStepTick(lastStepTick));
            }

            int stepTicks = StepTicks;

            if (stepTicks != appliedStepTicks)
            {
                appliedStepTicks = stepTicks;
                nextStepTime = TimeOfTick(NextStepTick(lastStepTick));
            }
        }

        private void DispatchDueSteps(long now)
        {
            long nextTick = NextStepTick(lastStepTick);
            long stepTime = TimeOfTick(nextTick);
            long stepDuration = Math.Max(1, (long)(appliedStepTicks / ticksPerTimestamp));

            long lateness = now - stepTime;

            if (lateness > stepDuration * MaxCatchUpSteps)
            {
                lastStepTick += lateness / stepDuration * appliedStepTicks;
                nextTick = NextStepTick(lastStepTick);
                stepTime = TimeOfTick(nextTick);
            }

            while (stepTime <= now)
            {
                lastStepTick = nextTick;
                RecordPunctuality(stepTime);
                RaiseStep(lastStepTick, stepTime);

                nextTick = NextStepTick(lastStepTick);
                stepTime = TimeOfTick(nextTick);
            }

            nextStepTime = stepTime;
        }

        private void RecordPunctuality(long stepTime)
        {
            long lateness = Stopwatch.GetTimestamp() - stepTime;

            Interlocked.Increment(ref statSteps);

            if (lateness > lateThresholdTicks)
                Interlocked.Increment(ref statLateSteps);

            if (lateness > Interlocked.Read(ref statMaxLateTicks))
                Interlocked.Exchange(ref statMaxLateTicks, lateness);
        }

        // returns punctuality since the last call and starts counting again
        public ClockTimingStats TakeTimingStats()
        {
            long steps = Interlocked.Exchange(ref statSteps, 0);
            long late = Interlocked.Exchange(ref statLateSteps, 0);
            long maxLate = Interlocked.Exchange(ref statMaxLateTicks, 0);

            return new ClockTimingStats(steps, late, maxLate * 1000.0 / Stopwatch.Frequency);
        }

        // the next multiple of the current step size, so changing step size keeps pulses and beats aligned
        private long NextStepTick(long tick)
        {
            return (tick / appliedStepTicks + 1) * appliedStepTicks;
        }

        private void RaiseStep(long tick, long timestamp)
        {
            // the pulse goes out first so the beat's frame follows it immediately
            if (IsSendingClock && tick % ticksPerPulse == 0)
            {
                outputScheduler.SendRealtime(MidiMessageKind.TimingClock);
            }

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

        private void SendTransportMessage(ClockTransport transport)
        {
            if (transport == ClockTransport.None || !IsSendingClock || !isTransportSent) return;

            MidiMessageKind kind = transport switch
            {
                ClockTransport.Start => MidiMessageKind.Start,
                ClockTransport.Continue => MidiMessageKind.Continue,
                _ => MidiMessageKind.Stop
            };

            outputScheduler.SendRealtime(kind);
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

            PlatformTimingActivity.End();
        }
    }
}
