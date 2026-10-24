using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Midi.Clock;
using TemperaMental.Midi.Core;
using TemperaMental.Midi.Output;
using TemperaMental.Midi.Playbacks;
using TemperaMental.Settings;
using TemperaMental.Transforms;
using Object = UnityEngine.Object;

// Play-mode tests for following external clock. A simulated master clock thread feeds ClockEngine.ReceiveExternal
// with exact timing; a fake output device records what the app sends. No CoreMIDI ports are involved, and the
// master thread always finishes before a call returns.
public static class ReceiveTestHarness
{
    class CapturingDevice : IOutputDevice
    {
        readonly ConcurrentQueue<MidiMessageData> sent;
        public CapturingDevice(ConcurrentQueue<MidiMessageData> sent) { this.sent = sent; }
        public event EventHandler<MidiEventSentEventArgs> EventSent { add { } remove { } }
        public void PrepareForEventsSending() { }
        public void SendEvent(MidiEvent midiEvent) => sent.Enqueue(MidiMessageData.FromEvent(midiEvent, Stopwatch.GetTimestamp()));
        public void Dispose() { }
    }

    static ConcurrentQueue<MidiMessageData> Captured => (ConcurrentQueue<MidiMessageData>)AppDomain.CurrentDomain.GetData("tm_rx_cap");

    static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    public static string Setup()
    {
        var captured = new ConcurrentQueue<MidiMessageData>();
        AppDomain.CurrentDomain.SetData("tm_rx_cap", captured);

        Object.FindAnyObjectByType<MidiOutputScheduler>().SetOutputDevice(new CapturingDevice(captured));

        // clock forwarded to the Out device (the fake one)
        Object.FindAnyObjectByType<ClockEventController>()
            .ActionOnClockRoutingChanged(new ClockRouting(null, ClockDestination.SameAsOut, null, true));

        var frames = Object.FindAnyObjectByType<FrameManager>();
        for (int k = 0; k < 8; k++)
        {
            if (k > 0) frames.InsertFrame();
            frames.AddEmitter(new UnityEngine.Vector2Int(k, k));
        }
        frames.GoToSelectedFrame(1);

        var settings = Object.FindAnyObjectByType<ClockSettingsManager>();
        var engine = Object.FindAnyObjectByType<ClockEngine>();
        for (int i = 0; i < 3 && engine.Mode != ClockMode.Receive; i++) settings.StepClockMode(1);
        settings.SetSendStartStop(true);

        Thread.Sleep(50);
        while (Captured.TryDequeue(out _)) { }

        return "mode=" + engine.Mode;
    }

    // runs a master clock script and reports what the app sent, relative to the master's pulses.
    // tokens: bpm:N  pulses:N  start  stop  continue  spp:N (sixteenths)  gap:MS (no pulses)  play (app Play button)
    public static string Master(string script)
    {
        var engine = Object.FindAnyObjectByType<ClockEngine>();
        var sequencer = Object.FindAnyObjectByType<FrameSequenceManager>();
        var frames = Object.FindAnyObjectByType<FrameManager>();

        while (Captured.TryDequeue(out _)) { }

        var pulseTimes = new List<long>();
        var marks = new List<Tuple<long, string>>();
        string[] tokens = script.Split(' ');

        var master = new Thread(() =>
        {
            double bpm = 120;
            long next = Stopwatch.GetTimestamp();

            foreach (string token in tokens)
            {
                string[] parts = token.Split(':');
                string op = parts[0];
                int value = parts.Length > 1 ? int.Parse(parts[1]) : 0;

                switch (op)
                {
                    case "bpm":
                        bpm = value;
                        break;

                    case "pulses":
                        long interval = (long)(Stopwatch.Frequency * 60.0 / bpm / 24);
                        for (int i = 0; i < value; i++)
                        {
                            while (Stopwatch.GetTimestamp() < next) Thread.SpinWait(10);
                            long now = Stopwatch.GetTimestamp();
                            lock (pulseTimes) pulseTimes.Add(now);
                            engine.ReceiveExternal(new MidiMessageData(now, MidiMessageKind.TimingClock));
                            next += interval;
                        }
                        break;

                    case "gap":
                        Thread.Sleep(value);
                        next = Stopwatch.GetTimestamp();
                        break;

                    case "play":
                        lock (marks) marks.Add(Tuple.Create(Stopwatch.GetTimestamp(), "-- app Play"));
                        sequencer.TogglePlayPause(frames.GetCurrentFrameNumber());
                        break;

                    case "appstop":
                        lock (marks) marks.Add(Tuple.Create(Stopwatch.GetTimestamp(), "-- app Stop x2"));
                        sequencer.Stop();
                        sequencer.Stop();
                        break;

                    default:
                        MidiMessageKind kind = op == "start" ? MidiMessageKind.Start
                            : op == "stop" ? MidiMessageKind.Stop
                            : op == "continue" ? MidiMessageKind.Continue
                            : MidiMessageKind.SongPosition;
                        long t = Stopwatch.GetTimestamp();
                        lock (marks) marks.Add(Tuple.Create(t, $"-- master {token}"));
                        engine.ReceiveExternal(new MidiMessageData(t, kind, data1: value));
                        break;
                }
            }
        });

        // record the sequencer's view at every beat (clock thread)
        var seqType = typeof(FrameSequenceManager);
        var playFrameField = seqType.GetField("playFrame", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var anchorField = seqType.GetField("anchorFrame", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var epochField = seqType.GetField("playEpoch", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Action<ClockBeat> beatProbe = beat =>
        {
            lock (marks) marks.Add(Tuple.Create(beat.Timestamp,
                $"   beat {beat.Beat} epoch {beat.Epoch}: sequencer {sequencer.State} playFrame {playFrameField.GetValue(sequencer)} anchor {anchorField.GetValue(sequencer)} playEpoch {epochField.GetValue(sequencer)}"));
        };
        engine.OnBeat += beatProbe;

        int callerThread = Thread.CurrentThread.ManagedThreadId;

        master.Start();
        master.Join();
        Thread.Sleep(30);

        engine.OnBeat -= beatProbe;
        lock (marks) marks.Add(Tuple.Create(Stopwatch.GetTimestamp(), $"   (harness ran on thread {callerThread})"));

        // pair what the app sent with the master pulse it followed
        var sent = new List<MidiMessageData>();
        while (Captured.TryDequeue(out var e)) sent.Add(e);

        int forwardedPulses = 0;
        var lines = new List<Tuple<long, string>>(marks);
        long lastCc = 0;
        int burstLine = -1;

        foreach (var e in sent)
        {
            if (e.Kind == MidiMessageKind.TimingClock) { forwardedPulses++; continue; }

            if (e.Kind == MidiMessageKind.ControlChange)
            {
                // a burst starts after 5 ms without CCs; transport messages don't break a burst
                if (Ms(e.Timestamp - lastCc) > 5)
                {
                    int pulse = pulseTimes.FindLastIndex(p => p <= e.Timestamp);
                    double lag = pulse >= 0 ? Ms(e.Timestamp - pulseTimes[pulse]) : -1;
                    lines.Add(Tuple.Create(e.Timestamp, $"   frame burst after master pulse {pulse} (beat {pulse / 24.0:F2}), +{lag:F3} ms, places:"));
                    burstLine = lines.Count - 1;
                }

                // CC 11 = place: list the cells so the frame can be identified (frame k places cell k*8+7-k)
                if (e.Data1 == 11 && burstLine >= 0)
                    lines[burstLine] = Tuple.Create(lines[burstLine].Item1, lines[burstLine].Item2 + " " + e.Data2);

                lastCc = e.Timestamp;
                continue;
            }

            lines.Add(Tuple.Create(e.Timestamp, $"   app sent {e.Kind}"));
        }

        lines.Sort((a, b) => a.Item1.CompareTo(b.Item1));

        var sb = new StringBuilder();
        long t0 = pulseTimes.Count > 0 ? pulseTimes[0] : 0;
        foreach (var line in lines) sb.AppendLine($"{Ms(line.Item1 - t0),9:F1} ms {line.Item2}");

        sb.AppendLine($"master pulses={pulseTimes.Count}, forwarded pulses={forwardedPulses}");
        sb.AppendLine($"measured tempo={engine.MeasuredTempo:F2}, clock present={engine.IsExternalClockPresent}");
        sb.AppendLine($"sequencer={sequencer.State}, app frame={frames.GetCurrentFrameNumber()}");
        return sb.ToString();
    }

    public static string LatchShiftRight()
    {
        var transforms = Object.FindAnyObjectByType<TransformManager>();
        var frames = Object.FindAnyObjectByType<FrameManager>();

        frames.GoToSelectedFrame(1);
        transforms.SetTransformType(TransformType.Shift);
        transforms.ToggleWrapping();
        transforms.ToggleLatching();
        transforms.HandleDirectionChange(frames.GetCurrentFrameSnapshot(), 3);
        return "latched";
    }

    public static string ResetAll()
    {
        Object.FindAnyObjectByType<TransformManager>().ResetTransforms();
        var sequencer = Object.FindAnyObjectByType<FrameSequenceManager>();
        sequencer.Stop();
        sequencer.Stop();
        Object.FindAnyObjectByType<FrameManager>().GoToSelectedFrame(1);
        return "reset (" + sequencer.State + ")";
    }

    public static string Status()
    {
        var engine = Object.FindAnyObjectByType<ClockEngine>();
        var tempo = Object.FindAnyObjectByType<MidiTempoManager>();
        return $"mode={engine.Mode} tempo shown={tempo.GetBpm()} measured={engine.MeasuredTempo:F2} present={engine.IsExternalClockPresent}";
    }

    public static string Cleanup()
    {
        var settings = Object.FindAnyObjectByType<ClockSettingsManager>();
        var engine = Object.FindAnyObjectByType<ClockEngine>();
        for (int i = 0; i < 3 && engine.Mode != ClockMode.Internal; i++) settings.StepClockMode(1);

        Object.FindAnyObjectByType<MidiOutputScheduler>().ClearOutputDevice();
        AppDomain.CurrentDomain.SetData("tm_rx_cap", null);
        return "cleaned, mode=" + engine.Mode;
    }
}
