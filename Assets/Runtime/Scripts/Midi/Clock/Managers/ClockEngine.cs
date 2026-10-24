using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using TemperaMental.Midi.Core;
using TemperaMental.Midi.Input;
using TemperaMental.Midi.Output;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Clock
{
    // The app's clock, running on a dedicated high-priority thread. Keeps a transport position in ticks
    // (TicksPerBeat per beat) and raises OnBeat at every beat boundary, and OnTick at steps in between (clock pulses,
    // and any tick a listener asks for with ScheduleTick). Between steps it pumps the MidiOutputScheduler, so all
    // MIDI output is sent from this one thread.
    //
    // Internal: times itself and wakes once per beat.
    // Send:     times itself, steps every MIDI clock pulse (TicksPerBeat / PulsesPerBeat ticks) and sends F8, plus
    //           Start/Continue/Stop when enabled.
    // Receive:  follows F8 from the clock source (the In device, or a separate Clock from device); each pulse is one
    //           step. Start/Continue/Stop/Song Position from the source drive the sequencer through OnExternalTransport,
    //           and are forwarded with the clock to the Clock to device unless that would send it back to its source.
    //           The tempo is measured from the pulses. A scheduled tick between two pulses is timed from the last pulse
    //           and the measured pulse length; until a tempo is measured it waits for the next pulse.
    //
    // OnBeat, OnTick and OnExternalTransport handlers run on the clock thread: keep them short and never touch Unity
    // objects.
    public class ClockEngine : MonoBehaviour
    {
        // after a long stall (e.g. a debugger pause) catch up at most this many steps, then jump ahead
        const int MaxCatchUpSteps = 4;

        // longest single sleep, so Receive mode notices a lost clock promptly
        const int MaxSleepMs = 100;

        [SerializeField] MidiOutputScheduler outputScheduler;

        // the In device's router, and the router for a separately chosen Clock from device
        [SerializeField] MidiInputRouter inputRouter;
        [SerializeField] MidiInputRouter clockInputRouter;

        // tempo measured in Receive mode, rounded, raised on the main thread
        [SerializeField] UnityEvent<int> onExternalTempoChanged = new UnityEvent<int>();

        // whether clock is arriving in Receive mode (false in the other modes), raised on the main thread when it changes
        [SerializeField] UnityEvent<bool> onExternalClockPresenceChanged = new UnityEvent<bool>();

        int ticksPerBeat;
        int ticksPerPulse;
        int pulsesPerBeat;
        long spinThresholdTicks;
        long lateThresholdTicks;
        long clockTimeoutTicks;
        double timestampFrequency;
        float tempoHysteresis;

        // written by the main thread, read by the clock thread
        volatile int requestedBpm;
        volatile int clockMode;
        volatile int clockDestination;
        volatile bool isTransportSent = true;
        volatile bool isForwardingAllowed;
        volatile bool isClockInputSeparate;
        long requestedEpoch;
        int pendingRestartTransport;
        long pendingRestartTime;    // 0: restart now
        volatile bool isRunning;

        readonly ConcurrentQueue<ClockTransport> transportQueue = new ConcurrentQueue<ClockTransport>();
        readonly ConcurrentQueue<MidiMessageData> externalQueue = new ConcurrentQueue<MidiMessageData>();

        // owned by the clock thread
        int appliedMode;
        int appliedBpm;
        int appliedStepTicks;
        long currentEpoch = -1;
        long lastSeenRequestedEpoch = -1;
        long anchorTime;
        double anchorTick;
        double ticksPerTimestamp;
        long lastStepTick;
        long nextStepTime;

        // the next tick a listener asked for, valid only for the epoch it was asked in
        long scheduledTick = long.MaxValue;
        long scheduledEpoch = -1;

        // Receive mode, owned by the clock thread
        long pulseCount;            // every pulse received
        long songPosition;          // the source's position (in pulses) of its next pulse
        bool hasSongPosition;       // Start or Song Position seen, so the source's beats are known
        bool isSourceRunning;       // between the source's Start/Continue and Stop
        bool isEpochFromSong;       // beats follow the source's song position, otherwise pulses counted from epochOrigin
        bool isEpochStarted;
        long epochOrigin;
        bool isRestartPending;      // the app asked to play; starts on the next pulse, or the source's next beat
        long pendingEpoch;
        long lastPulseTime;
        double[] pulseIntervals;
        int intervalCount;
        int intervalIndex;
        double intervalSum;
        long lastPulseStepTick;     // transport tick and time of the last pulse, for timing scheduled ticks after it
        long lastPulseStepTime;
        long interpolatedStepTime = long.MaxValue;
        bool isRestartFree;         // the pending restart counts pulses from the next one, ignoring the source's beats

        // where the transport is in time, for TryGetTickAt on any thread: tick referenceTick of referenceEpoch was at
        // referenceTime, moving referenceRate ticks per timestamp unit (0 when unknown). In Receive mode ticks never
        // reach referenceLimit, the next pulse's tick, until that pulse arrives
        readonly object referenceLock = new object();
        long referenceEpoch = -1;
        double referenceTick;
        long referenceTime;
        double referenceRate;
        long referenceLimit = long.MaxValue;

        // written by the clock thread, read by the main thread
        volatile bool isExternalClockPresent;
        long measuredTempoBits;

        // main thread
        bool wasExternalClockPresent;
        float displayedTempo;

        Thread clockThread;
        AutoResetEvent wakeSignal;

        // punctuality, written by the clock thread and taken by anyone
        long statSteps;
        long statLateSteps;
        long statMaxLateTicks;

        // listener exceptions are logged on the main thread
        readonly ConcurrentQueue<Exception> listenerErrors = new ConcurrentQueue<Exception>();

        // handlers are kept in arrays replaced when listeners are added or removed, so raising them on the clock thread
        // allocates nothing (OnTick can be raised every pulse)
        readonly object handlerLock = new object();
        volatile Action<ClockBeat>[] beatHandlers = new Action<ClockBeat>[0];
        volatile Action<ClockTick>[] tickHandlers = new Action<ClockTick>[0];

        public event Action<ClockBeat> OnBeat
        {
            add { lock (handlerLock) beatHandlers = AddHandler(beatHandlers, value); }
            remove { lock (handlerLock) beatHandlers = RemoveHandler(beatHandlers, value); }
        }

        // every step that isn't a beat boundary
        public event Action<ClockTick> OnTick
        {
            add { lock (handlerLock) tickHandlers = AddHandler(tickHandlers, value); }
            remove { lock (handlerLock) tickHandlers = RemoveHandler(tickHandlers, value); }
        }

        // transport received from the clock source in Receive mode: transport, epoch of the beats that follow,
        // and for SongPosition the beat number (from the song start) the next frame begins on
        public event Action<ClockTransport, long, int> OnExternalTransport;

        public int TicksPerBeat => ticksPerBeat;

        public ClockMode Mode => (ClockMode)clockMode;

        public bool IsExternalClockPresent => isExternalClockPresent;

        public double MeasuredTempo => BitConverter.Int64BitsToDouble(Interlocked.Read(ref measuredTempoBits));

        private bool IsReceiving => appliedMode == (int)ClockMode.Receive;

        private bool IsSendingClock => clockMode == (int)ClockMode.Send && clockDestination != (int)ClockDestination.None;

        private bool IsForwarding => clockDestination != (int)ClockDestination.None && isForwardingAllowed;

        // pulses are only needed while sending clock
        private int StepTicks => IsSendingClock ? ticksPerPulse : ticksPerBeat;

        private void Awake()
        {
            ticksPerBeat = ConfigRegistry.Midi.TicksPerBeat;
            pulsesPerBeat = ConfigRegistry.Midi.PulsesPerBeat;
            ticksPerPulse = Math.Max(1, ticksPerBeat / pulsesPerBeat);

            timestampFrequency = Stopwatch.Frequency;
            spinThresholdTicks = (long)(Stopwatch.Frequency * ConfigRegistry.Midi.ClockSpinThresholdMs / 1000.0);
            lateThresholdTicks = Stopwatch.Frequency / 1000;
            clockTimeoutTicks = (long)(Stopwatch.Frequency * ConfigRegistry.Midi.ExternalClockTimeout);
            tempoHysteresis = ConfigRegistry.Midi.TempoDisplayHysteresis;

            pulseIntervals = new double[Math.Max(1, ConfigRegistry.Midi.TempoMeasurePulses)];

            requestedBpm = ConfigRegistry.Midi.DefaultBpm;
        }

        private void OnEnable()
        {
            PlatformTimingActivity.Begin("TemperaMental MIDI clock");

            wakeSignal = new AutoResetEvent(false);
            outputScheduler.SetWakeHandle(wakeSignal);

            inputRouter.MessageReceived += OnInputMessage;
            clockInputRouter.MessageReceived += OnClockInputMessage;

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

            UpdateExternalClockStatus();
        }

        // --- main thread API ---

        public void SetBpm(int bpm)
        {
            if (bpm <= 0) return;

            requestedBpm = bpm;
            wakeSignal?.Set();
        }

        public void SetClockMode(ClockMode mode)
        {
            clockMode = (int)mode;
            wakeSignal?.Set();
        }

        // whether Start/Continue/Stop are sent (or forwarded) along with the clock
        public void SetTransportSent(bool isSent)
        {
            isTransportSent = isSent;
        }

        public void SetClockRouting(bool isInputSeparate, ClockDestination destination, bool isForwardAllowed)
        {
            isClockInputSeparate = isInputSeparate;
            clockDestination = (int)destination;
            isForwardingAllowed = isForwardAllowed;
            wakeSignal?.Set();
        }

        // restarts the transport at tick 0 and raises beat 0 on the clock thread straight away.
        // when sending clock, the transport message (Start or Continue) goes out just before the first pulse.
        // in Receive mode the restart waits for the next pulse, or for the source's next beat when its position is known,
        // and no transport message is sent.
        // atTimestamp (a Stopwatch timestamp a little in the past, e.g. a touch on the Tempera) puts tick 0 at that
        // moment: beat 0 is raised straight away and later steps are timed from it. In Receive mode tick 0 is then the
        // pulse nearest that moment, counting pulses from there.
        // returns the new epoch; beats raised before the restart takes effect carry an older epoch
        public long Restart(ClockTransport transport = ClockTransport.None, long atTimestamp = 0)
        {
            Interlocked.Exchange(ref pendingRestartTransport, (int)transport);
            Interlocked.Exchange(ref pendingRestartTime, atTimestamp);
            long epoch = Interlocked.Increment(ref requestedEpoch);
            wakeSignal?.Set();

            return epoch;
        }

        // sends a transport message (e.g. Stop) as soon as possible without restarting the clock.
        // ignored in Receive mode, where transport follows the clock source
        public void SendTransport(ClockTransport transport)
        {
            if (transport == ClockTransport.None) return;

            transportQueue.Enqueue(transport);
            wakeSignal?.Set();
        }

        // clock and transport messages from the clock source; any thread. Only used in Receive mode
        public void ReceiveExternal(MidiMessageData message)
        {
            if (clockMode != (int)ClockMode.Receive) return;

            switch (message.Kind)
            {
                case MidiMessageKind.TimingClock:
                case MidiMessageKind.Start:
                case MidiMessageKind.Continue:
                case MidiMessageKind.Stop:
                case MidiMessageKind.SongPosition:
                    externalQueue.Enqueue(message);
                    wakeSignal?.Set();
                    break;
            }
        }

        // returns punctuality since the last call and starts counting again
        public ClockTimingStats TakeTimingStats()
        {
            long steps = Interlocked.Exchange(ref statSteps, 0);
            long late = Interlocked.Exchange(ref statLateSteps, 0);
            long maxLate = Interlocked.Exchange(ref statMaxLateTicks, 0);

            return new ClockTimingStats(steps, late, maxLate * 1000.0 / Stopwatch.Frequency);
        }

        // any thread: the epoch and transport tick at a Stopwatch timestamp, worked out from the latest step. Ticks
        // before the epoch's tick 0 are negative. False until the clock has a position
        public bool TryGetTickAt(long timestamp, out long epoch, out long tick)
        {
            lock (referenceLock)
            {
                epoch = referenceEpoch;
                tick = 0;

                if (referenceEpoch < 0) return false;

                double position = referenceTick + (timestamp - referenceTime) * referenceRate;
                tick = (long)Math.Floor(position);

                if (tick >= referenceLimit) tick = referenceLimit - 1;

                return true;
            }
        }

        private void SetReference(double tick, long time, double rate, long limit)
        {
            lock (referenceLock)
            {
                referenceEpoch = currentEpoch;
                referenceTick = tick;
                referenceTime = time;
                referenceRate = rate;
                referenceLimit = limit;
            }
        }

        private void ClearReference()
        {
            lock (referenceLock)
            {
                referenceEpoch = -1;
            }
        }

        // clock thread only (call it from an OnBeat or OnTick handler): asks for a step at this tick of the current
        // epoch, which is raised through OnTick. Only the earliest pending request is kept, so ask again for the next
        // one from its handler. Ignored for ticks already passed
        public void ScheduleTick(long tick)
        {
            if (tick <= lastStepTick) return;

            if (!HasScheduledTick || tick < scheduledTick)
            {
                scheduledTick = tick;
                scheduledEpoch = currentEpoch;
            }
        }

        // driver threads
        private void OnInputMessage(MidiMessageData message)
        {
            if (!isClockInputSeparate) ReceiveExternal(message);
        }

        private void OnClockInputMessage(MidiMessageData message)
        {
            if (isClockInputSeparate) ReceiveExternal(message);
        }

        private void UpdateExternalClockStatus()
        {
            if (clockMode != (int)ClockMode.Receive)
            {
                if (wasExternalClockPresent) onExternalClockPresenceChanged?.Invoke(false);

                wasExternalClockPresent = false;
                return;
            }

            bool isPresent = isExternalClockPresent;

            if (isPresent != wasExternalClockPresent)
            {
                wasExternalClockPresent = isPresent;
                onExternalClockPresenceChanged?.Invoke(isPresent);

                if (isPresent)
                    LogMan.Log("MIDI clock found");
                else
                    LogMan.LogWarning("No MIDI clock");
            }

            double tempo = MeasuredTempo;

            if (isPresent && tempo > 0 && Math.Abs(tempo - displayedTempo) >= tempoHysteresis)
            {
                displayedTempo = (float)tempo;
                onExternalTempoChanged?.Invoke((int)Math.Round(tempo));
            }
        }

        // --- clock thread ---

        private void ThreadLoop()
        {
            appliedMode = clockMode;

            if (IsReceiving) EnterReceiveMode();

            while (isRunning)
            {
                long now = Stopwatch.GetTimestamp();

                ApplyRequests(now);

                if (IsReceiving)
                {
                    ProcessExternal();
                    CheckClockTimeout(now);

                    if (now >= interpolatedStepTime)
                    {
                        DispatchInterpolatedStep();
                        continue;
                    }
                }
                else if (now >= nextStepTime)
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
            long target = IsReceiving ? interpolatedStepTime : nextStepTime;

            if (outputScheduler.HasWork)
                target = Math.Min(target, outputScheduler.NextSendTime);

            if (!externalQueue.IsEmpty) return;

            long remaining = target == long.MaxValue ? long.MaxValue : target - Stopwatch.GetTimestamp();

            if (remaining <= 0) return;

            // sleep for most of the wait, then spin for the last part for accuracy
            if (remaining > spinThresholdTicks)
            {
                double sleepMs = remaining == long.MaxValue
                    ? MaxSleepMs
                    : (remaining - spinThresholdTicks) * 1000.0 / Stopwatch.Frequency;

                if (sleepMs >= 1)
                {
                    wakeSignal.WaitOne((int)Math.Min(sleepMs, MaxSleepMs));
                    return;
                }
            }

            Thread.SpinWait(20);
        }

        private void ApplyRequests(long now)
        {
            int mode = clockMode;

            if (mode != appliedMode)
            {
                int previousMode = appliedMode;
                appliedMode = mode;
                OnModeChanged(previousMode, now);
            }

            long epoch = Interlocked.Read(ref requestedEpoch);

            if (epoch != lastSeenRequestedEpoch)
            {
                lastSeenRequestedEpoch = epoch;
                var transport = (ClockTransport)Interlocked.Exchange(ref pendingRestartTransport, 0);
                long restartTime = Interlocked.Exchange(ref pendingRestartTime, 0);

                if (IsReceiving)
                {
                    if (restartTime != 0)
                    {
                        RestartAtNearestPulse(epoch, restartTime);
                    }
                    else
                    {
                        // the restart happens on the next suitable pulse
                        pendingEpoch = epoch;
                        isRestartPending = true;
                        isRestartFree = false;
                    }
                }
                else
                {
                    currentEpoch = epoch;
                    SendTransportMessage(transport);
                    RestartInternal(restartTime != 0 ? Math.Min(restartTime, now) : now);
                }
            }

            while (transportQueue.TryDequeue(out ClockTransport queued))
            {
                if (!IsReceiving) SendTransportMessage(queued);
            }

            if (IsReceiving) return;

            int bpm = requestedBpm;

            if (bpm != appliedBpm)
            {
                // re-anchor at the current position so the change applies from now on
                anchorTick = TickAt(now);
                anchorTime = now;
                SetTempo(bpm);
                SetReference(anchorTick, anchorTime, ticksPerTimestamp, long.MaxValue);

                nextStepTime = TimeOfTick(NextDueTick(lastStepTick));
            }

            int stepTicks = StepTicks;

            if (stepTicks != appliedStepTicks)
            {
                appliedStepTicks = stepTicks;
                nextStepTime = TimeOfTick(NextDueTick(lastStepTick));
            }
        }

        // the epoch is kept so playback carries on in the new mode; only the timing source changes
        private void OnModeChanged(int previousMode, long now)
        {
            if (IsReceiving)
            {
                EnterReceiveMode();
            }
            else if (previousMode == (int)ClockMode.Receive)
            {
                while (externalQueue.TryDequeue(out _)) { }

                isExternalClockPresent = false;
                RestartInternal(now);
            }
        }

        // tick 0 at 'start', normally now; a past start (a recording's trigger) makes later steps catch up straight away
        private void RestartInternal(long start)
        {
            SetTempo(requestedBpm);

            anchorTime = start;
            anchorTick = 0;
            lastStepTick = 0;
            appliedStepTicks = StepTicks;
            scheduledTick = long.MaxValue;

            SetReference(0, anchorTime, ticksPerTimestamp, long.MaxValue);

            RaiseStep(0, start);

            nextStepTime = TimeOfTick(NextDueTick(lastStepTick));
        }

        private void DispatchDueSteps(long now)
        {
            long nextTick = NextDueTick(lastStepTick);
            long stepTime = TimeOfTick(nextTick);
            long stepDuration = Math.Max(1, (long)(appliedStepTicks / ticksPerTimestamp));

            long lateness = now - stepTime;

            // a scheduled tick jumped over here is caught up by its listener at the next step
            if (lateness > stepDuration * MaxCatchUpSteps)
            {
                lastStepTick += lateness / stepDuration * appliedStepTicks;
                nextTick = NextDueTick(lastStepTick);
                stepTime = TimeOfTick(nextTick);
            }

            while (stepTime <= now)
            {
                lastStepTick = nextTick;
                RecordPunctuality(stepTime);
                RaiseStep(lastStepTick, stepTime);

                nextTick = NextDueTick(lastStepTick);
                stepTime = TimeOfTick(nextTick);
            }

            nextStepTime = stepTime;
        }

        // --- Receive mode ---

        private void EnterReceiveMode()
        {
            nextStepTime = long.MaxValue;
            ClearReference();

            hasSongPosition = false;
            isSourceRunning = false;
            isEpochFromSong = false;
            isEpochStarted = false;     // pulse-counted beats start on the next pulse
            isRestartPending = false;
            interpolatedStepTime = long.MaxValue;

            ResetTempoMeasurement();
        }

        private void ProcessExternal()
        {
            while (externalQueue.TryDequeue(out MidiMessageData message))
            {
                switch (message.Kind)
                {
                    case MidiMessageKind.TimingClock:
                        OnExternalPulse(message.Timestamp);
                        break;

                    case MidiMessageKind.Start:
                        OnExternalStart();
                        break;

                    case MidiMessageKind.Continue:
                        OnExternalContinue();
                        break;

                    case MidiMessageKind.Stop:
                        OnExternalStop();
                        break;

                    case MidiMessageKind.SongPosition:
                        OnExternalSongPosition(message.Data1);
                        break;
                }
            }
        }

        private void OnExternalPulse(long timestamp)
        {
            pulseCount++;
            MeasureTempo(timestamp);
            isExternalClockPresent = true;

            if (IsForwarding)
                outputScheduler.SendRealtime(MidiMessageKind.TimingClock);

            long songPulse = -1;
            bool isFollowingSong = hasSongPosition && isSourceRunning;

            if (isFollowingSong)
                songPulse = songPosition++;

            // the app pressed Play: start now, or on the source's next beat if its beats are known
            if (isRestartPending && (isRestartFree || !isFollowingSong || songPulse % pulsesPerBeat == 0))
            {
                isRestartPending = false;
                currentEpoch = pendingEpoch;
                isEpochFromSong = isFollowingSong && !isRestartFree;
                epochOrigin = isEpochFromSong ? songPulse : pulseCount;
                isEpochStarted = true;
                isRestartFree = false;
            }

            if (!isEpochStarted && !isEpochFromSong)
            {
                isEpochStarted = true;
                epochOrigin = pulseCount;
            }

            long position;

            if (isEpochFromSong)
            {
                if (!isEpochStarted || songPulse < epochOrigin)
                {
                    interpolatedStepTime = long.MaxValue;
                    return;
                }

                position = songPulse - epochOrigin;
            }
            else
            {
                position = pulseCount - epochOrigin;
            }

            lastStepTick = position * ticksPerPulse;
            lastPulseStepTick = lastStepTick;
            lastPulseStepTime = timestamp;
            SetPulseReference();

            RaiseStep(lastStepTick, timestamp);

            ScheduleInterpolatedStep();
        }

        // ticks after the last pulse move at the measured pulse rate, and stop short of the next pulse
        private void SetPulseReference()
        {
            double rate = intervalCount == 0 ? 0 : ticksPerPulse / (intervalSum / intervalCount * timestampFrequency / 1000.0);

            SetReference(lastPulseStepTick, lastPulseStepTime, rate, lastPulseStepTick + ticksPerPulse);
        }

        // a restart at a moment in the past (a recording's trigger): tick 0 is the pulse nearest that moment, and the
        // epoch counts pulses from there, ignoring the source's beats. When the last pulse is nearer, the epoch starts
        // on it straight away (late); otherwise on the next pulse
        private void RestartAtNearestPulse(long epoch, long restartTime)
        {
            double pulseTimestamps = intervalCount == 0 ? 0 : intervalSum / intervalCount * timestampFrequency / 1000.0;
            bool isLastPulseNearer = isExternalClockPresent && pulseTimestamps > 0 &&
                                     restartTime - lastPulseTime < pulseTimestamps / 2;

            if (!isLastPulseNearer)
            {
                pendingEpoch = epoch;
                isRestartPending = true;
                isRestartFree = true;
                return;
            }

            isRestartPending = false;
            isRestartFree = false;
            currentEpoch = epoch;
            isEpochFromSong = false;
            isEpochStarted = true;
            epochOrigin = pulseCount;

            lastStepTick = 0;
            lastPulseStepTick = 0;
            lastPulseStepTime = lastPulseTime;
            SetPulseReference();

            RaiseStep(0, lastPulseTime);

            ScheduleInterpolatedStep();
        }

        // times a scheduled tick that falls before the next pulse from the last pulse and the measured pulse length.
        // later ticks wait for their pulse; with no tempo measured yet they're raised with the next pulse instead
        private void ScheduleInterpolatedStep()
        {
            interpolatedStepTime = long.MaxValue;

            if (!HasScheduledTick || intervalCount == 0) return;
            if (scheduledTick >= lastPulseStepTick + ticksPerPulse) return;

            double pulseTimestamps = intervalSum / intervalCount * timestampFrequency / 1000.0;
            double offset = (scheduledTick - lastPulseStepTick) * pulseTimestamps / ticksPerPulse;

            interpolatedStepTime = lastPulseStepTime + (long)Math.Round(offset);
        }

        private void DispatchInterpolatedStep()
        {
            long stepTime = interpolatedStepTime;
            interpolatedStepTime = long.MaxValue;

            if (!HasScheduledTick || scheduledTick >= lastPulseStepTick + ticksPerPulse) return;

            lastStepTick = scheduledTick;
            RaiseStep(lastStepTick, stepTime);

            ScheduleInterpolatedStep();
        }

        private void OnExternalStart()
        {
            hasSongPosition = true;
            songPosition = 0;
            isSourceRunning = true;

            // the first pulse after Start is the first beat
            StartSongEpoch(0);

            ForwardTransport(ClockTransport.Start);
            RaiseTransport(ClockTransport.Start, currentEpoch, 0);
        }

        private void OnExternalContinue()
        {
            if (!hasSongPosition)
            {
                // a Continue with no known position counts as a Start
                OnExternalStart();
                return;
            }

            isSourceRunning = true;

            if (!isEpochFromSong)
                StartSongEpoch(NextBeatBoundary(songPosition));

            ForwardTransport(ClockTransport.Continue);
            RaiseTransport(ClockTransport.Continue, currentEpoch, -1);
        }

        private void OnExternalStop()
        {
            isSourceRunning = false;

            ForwardTransport(ClockTransport.Stop);
            RaiseTransport(ClockTransport.Stop, currentEpoch, -1);
        }

        // Song Position is in sixteenths (6 pulses); the next frame starts on the first whole beat from there
        private void OnExternalSongPosition(int sixteenths)
        {
            songPosition = (long)sixteenths * pulsesPerBeat / 4;
            hasSongPosition = true;

            long origin = NextBeatBoundary(songPosition);
            StartSongEpoch(origin);

            RaiseTransport(ClockTransport.SongPosition, currentEpoch, (int)(origin / pulsesPerBeat));
        }

        private void StartSongEpoch(long origin)
        {
            long epoch = Interlocked.Increment(ref requestedEpoch);
            lastSeenRequestedEpoch = epoch;
            currentEpoch = epoch;

            isRestartPending = false;
            isRestartFree = false;
            isEpochFromSong = true;
            isEpochStarted = true;
            epochOrigin = origin;
        }

        private long NextBeatBoundary(long position)
        {
            return (position + pulsesPerBeat - 1) / pulsesPerBeat * pulsesPerBeat;
        }

        private void ForwardTransport(ClockTransport transport)
        {
            if (!IsForwarding || !isTransportSent) return;

            outputScheduler.SendRealtime(ToMessageKind(transport));
        }

        private void MeasureTempo(long timestamp)
        {
            long gap = timestamp - lastPulseTime;
            lastPulseTime = timestamp;

            if (gap <= 0 || gap > clockTimeoutTicks)
            {
                ResetTempoMeasurement();
                return;
            }

            double intervalMs = gap * 1000.0 / timestampFrequency;

            if (intervalCount == pulseIntervals.Length)
                intervalSum -= pulseIntervals[intervalIndex];
            else
                intervalCount++;

            pulseIntervals[intervalIndex] = intervalMs;
            intervalSum += intervalMs;
            intervalIndex = (intervalIndex + 1) % pulseIntervals.Length;

            double tempo = 60000.0 / (intervalSum / intervalCount * pulsesPerBeat);
            Interlocked.Exchange(ref measuredTempoBits, BitConverter.DoubleToInt64Bits(tempo));
        }

        private void ResetTempoMeasurement()
        {
            intervalCount = 0;
            intervalIndex = 0;
            intervalSum = 0;
        }

        private void CheckClockTimeout(long now)
        {
            if (isExternalClockPresent && now - lastPulseTime > clockTimeoutTicks)
            {
                isExternalClockPresent = false;
                ResetTempoMeasurement();
            }
        }

        // --- shared ---

        private void RecordPunctuality(long stepTime)
        {
            long lateness = Stopwatch.GetTimestamp() - stepTime;

            Interlocked.Increment(ref statSteps);

            if (lateness > lateThresholdTicks)
                Interlocked.Increment(ref statLateSteps);

            if (lateness > Interlocked.Read(ref statMaxLateTicks))
                Interlocked.Exchange(ref statMaxLateTicks, lateness);
        }

        // the next multiple of the current step size, so changing step size keeps pulses and beats aligned
        private long NextStepTick(long tick)
        {
            return (tick / appliedStepTicks + 1) * appliedStepTicks;
        }

        // the next regular step, or a scheduled tick before it
        private long NextDueTick(long tick)
        {
            long next = NextStepTick(tick);

            return HasScheduledTick && scheduledTick < next ? scheduledTick : next;
        }

        private bool HasScheduledTick => scheduledEpoch == currentEpoch && scheduledTick != long.MaxValue && scheduledTick > lastStepTick;

        private void RaiseStep(long tick, long timestamp)
        {
            // reached (or passed): listeners ask for their next one from their handlers
            if (scheduledTick <= tick) scheduledTick = long.MaxValue;

            // the pulse goes out first so the beat's frame follows it immediately
            if (IsSendingClock && !IsReceiving && tick % ticksPerPulse == 0)
            {
                outputScheduler.SendRealtime(MidiMessageKind.TimingClock);
            }

            if (tick % ticksPerBeat != 0)
            {
                RaiseTick(tick, timestamp);
                return;
            }

            Action<ClockBeat>[] handlers = beatHandlers;

            if (handlers.Length == 0) return;

            ClockBeat beat = new ClockBeat(currentEpoch, tick / ticksPerBeat, timestamp);

            // call each listener separately so one failing doesn't stop the others
            foreach (Action<ClockBeat> handler in handlers)
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

        private void RaiseTick(long tick, long timestamp)
        {
            Action<ClockTick>[] handlers = tickHandlers;

            if (handlers.Length == 0) return;

            ClockTick clockTick = new ClockTick(currentEpoch, tick, timestamp);

            foreach (Action<ClockTick> handler in handlers)
            {
                try
                {
                    handler(clockTick);
                }
                catch (Exception ex)
                {
                    listenerErrors.Enqueue(ex);
                }
            }
        }

        private void RaiseTransport(ClockTransport transport, long epoch, int songBeat)
        {
            Action<ClockTransport, long, int> handlers = OnExternalTransport;

            if (handlers == null) return;

            foreach (Action<ClockTransport, long, int> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(transport, epoch, songBeat);
                }
                catch (Exception ex)
                {
                    listenerErrors.Enqueue(ex);
                }
            }
        }

        private static T[] AddHandler<T>(T[] handlers, T handler) where T : Delegate
        {
            if (handler == null) return handlers;

            var result = new T[handlers.Length + 1];
            Array.Copy(handlers, result, handlers.Length);
            result[handlers.Length] = handler;

            return result;
        }

        private static T[] RemoveHandler<T>(T[] handlers, T handler) where T : Delegate
        {
            int index = Array.LastIndexOf(handlers, handler);

            if (index < 0) return handlers;

            var result = new T[handlers.Length - 1];
            Array.Copy(handlers, 0, result, 0, index);
            Array.Copy(handlers, index + 1, result, index, handlers.Length - index - 1);

            return result;
        }

        private void SendTransportMessage(ClockTransport transport)
        {
            if (transport == ClockTransport.None || !IsSendingClock || !isTransportSent) return;

            outputScheduler.SendRealtime(ToMessageKind(transport));
        }

        private static MidiMessageKind ToMessageKind(ClockTransport transport)
        {
            return transport switch
            {
                ClockTransport.Start => MidiMessageKind.Start,
                ClockTransport.Continue => MidiMessageKind.Continue,
                _ => MidiMessageKind.Stop
            };
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
            inputRouter.MessageReceived -= OnInputMessage;
            clockInputRouter.MessageReceived -= OnClockInputMessage;

            isRunning = false;
            wakeSignal?.Set();

            clockThread?.Join();
            clockThread = null;

            // the clock thread has stopped, so the scheduler can send from here: release any notes still on
            outputScheduler.ReleaseNotesNow();

            outputScheduler.SetWakeHandle(null);
            wakeSignal?.Dispose();
            wakeSignal = null;

            PlatformTimingActivity.End();
        }
    }
}
