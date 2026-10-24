using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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

// Play-mode test harness for the clock and output scheduler. A fake output device records every message the
// scheduler sends, timestamped on the clock thread, so no CoreMIDI ports are involved.
// State is kept in AppDomain data between calls because each run compiles a new assembly.
public static class ClockTestHarness
{
    class CapturingDevice : IOutputDevice
    {
        readonly ConcurrentQueue<MidiMessageData> sent;

        public CapturingDevice(ConcurrentQueue<MidiMessageData> sent) { this.sent = sent; }

        public event EventHandler<MidiEventSentEventArgs> EventSent { add { } remove { } }

        public void PrepareForEventsSending() { }

        public void SendEvent(MidiEvent midiEvent)
        {
            sent.Enqueue(MidiMessageData.FromEvent(midiEvent, Stopwatch.GetTimestamp()));
        }

        public void Dispose() { }
    }

    static ConcurrentQueue<MidiMessageData> Queue => (ConcurrentQueue<MidiMessageData>)AppDomain.CurrentDomain.GetData("tm_cap");

    static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    public static string Setup()
    {
        var queue = new ConcurrentQueue<MidiMessageData>();
        AppDomain.CurrentDomain.SetData("tm_cap", queue);

        Object.FindAnyObjectByType<MidiOutputScheduler>().SetOutputDevice(new CapturingDevice(queue));

        var frames = Object.FindAnyObjectByType<FrameManager>();
        for (int k = 0; k < 8; k++)
        {
            if (k > 0) frames.InsertFrame();
            frames.AddEmitter(new UnityEngine.Vector2Int(k, k));
        }
        frames.GoToSelectedFrame(1);

        Object.FindAnyObjectByType<MidiTempoManager>().SetBpm(120);

        var clockSettings = Object.FindAnyObjectByType<ClockSettingsManager>();
        if (Object.FindAnyObjectByType<ClockEngine>().Mode != ClockMode.Send) clockSettings.StepClockMode(1);
        clockSettings.SetSendStartStop(true);

        Object.FindAnyObjectByType<ClockEngine>().TakeTimingStats();

        return "mode=" + Object.FindAnyObjectByType<ClockEngine>().Mode + ", timing activity=" + PlatformTimingActivity.IsActive;
    }

    public static string Drain()
    {
        while (Queue.TryDequeue(out _)) { }
        Object.FindAnyObjectByType<ClockEngine>().TakeTimingStats();
        return "drained";
    }

    public static string ToggleLoop()
    {
        Object.FindAnyObjectByType<FrameSequenceManager>().ToggleLooping();
        return "loop toggled";
    }

    public static string Play()
    {
        var frames = Object.FindAnyObjectByType<FrameManager>();
        var sequencer = Object.FindAnyObjectByType<FrameSequenceManager>();
        frames.GoToSelectedFrame(1);
        sequencer.TogglePlayPause(frames.GetCurrentFrameNumber());
        return Drain();
    }

    public static string StopAll()
    {
        var sequencer = Object.FindAnyObjectByType<FrameSequenceManager>();
        sequencer.Stop();
        sequencer.Stop();
        return "stopped (" + sequencer.State + ")";
    }

    // pulse spacing, clock thread punctuality, and how soon each emitter burst follows the latest pulse
    public static string Measure()
    {
        var events = new List<MidiMessageData>();
        while (Queue.TryDequeue(out var e)) events.Add(e);

        var intervals = new List<double>();
        var burstLags = new List<double>();
        var burstGaps = new List<double>();
        long lastClock = -1, lastEvent = 0, lastBurst = -1;
        int pulses = 0, ccs = 0, transport = 0;

        foreach (var e in events)
        {
            switch (e.Kind)
            {
                case MidiMessageKind.TimingClock:
                    if (lastClock >= 0) intervals.Add(Ms(e.Timestamp - lastClock));
                    lastClock = e.Timestamp;
                    pulses++;
                    break;

                case MidiMessageKind.ControlChange:
                    ccs++;
                    // a burst starts after a quiet gap of more than 5 ms
                    if (Ms(e.Timestamp - lastEvent) > 5 || lastBurst < 0 && ccs == 1)
                    {
                        if (lastClock >= 0) burstLags.Add(Ms(e.Timestamp - lastClock));
                        if (lastBurst >= 0) burstGaps.Add(Ms(e.Timestamp - lastBurst));
                        lastBurst = e.Timestamp;
                    }
                    break;

                case MidiMessageKind.Start:
                case MidiMessageKind.Continue:
                case MidiMessageKind.Stop:
                    transport++;
                    break;
            }

            if (e.Kind != MidiMessageKind.TimingClock) lastEvent = e.Timestamp;
        }

        var timing = Object.FindAnyObjectByType<ClockEngine>().TakeTimingStats();

        return $"events={events.Count} pulses={pulses} ccs={ccs} transport={transport}\n" +
               $"pulse interval ms: {Stats(intervals)}\n" +
               $"burst start after latest pulse ms: {Stats(burstLags)}\n" +
               $"gap between bursts ms: {Stats(burstGaps)}\n" +
               $"clock thread: {timing.LateSteps} of {timing.Steps} steps over 1 ms late, worst {timing.MaxLatenessMs:F3} ms";
    }

    // pause, resume, seek, stop and stop again; lists everything sent except pulses, which are counted
    public static string Transport()
    {
        var sequencer = Object.FindAnyObjectByType<FrameSequenceManager>();
        var frames = Object.FindAnyObjectByType<FrameManager>();

        frames.GoToSelectedFrame(1);
        Drain();

        var marks = new List<Tuple<long, string>>();
        Action<string> mark = text => marks.Add(Tuple.Create(Stopwatch.GetTimestamp(), "-- " + text + " (" + sequencer.State + ")"));

        mark("play"); sequencer.TogglePlayPause(frames.GetCurrentFrameNumber()); System.Threading.Thread.Sleep(1100);
        mark("pause"); sequencer.TogglePlayPause(frames.GetCurrentFrameNumber()); System.Threading.Thread.Sleep(600);
        mark("resume"); sequencer.TogglePlayPause(frames.GetCurrentFrameNumber()); System.Threading.Thread.Sleep(600);
        mark("seek 6"); sequencer.SeekToFrame(6); System.Threading.Thread.Sleep(300);
        mark("stop"); sequencer.Stop(); System.Threading.Thread.Sleep(300);
        mark("stop again"); sequencer.Stop(); System.Threading.Thread.Sleep(100);
        mark("end");

        var lines = new List<Tuple<long, string>>(marks);
        int pulseRun = 0;
        long runStart = 0;

        while (Queue.TryDequeue(out var e))
        {
            if (e.Kind == MidiMessageKind.TimingClock)
            {
                if (pulseRun == 0) runStart = e.Timestamp;
                pulseRun++;
                continue;
            }

            if (pulseRun > 0)
            {
                lines.Add(Tuple.Create(runStart, $"   {pulseRun} x F8"));
                pulseRun = 0;
            }

            string text = e.Kind == MidiMessageKind.ControlChange ? $"   CC{e.Data1}={e.Data2}" : $"   {e.Kind}";
            lines.Add(Tuple.Create(e.Timestamp, text));
        }

        if (pulseRun > 0) lines.Add(Tuple.Create(runStart, $"   {pulseRun} x F8"));

        lines.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        long t0 = marks[0].Item1;

        var sb = new StringBuilder();
        foreach (var line in lines) sb.AppendLine($"{Ms(line.Item1 - t0),9:F2} ms {line.Item2}");
        return sb.ToString();
    }

    // checks that a Start is immediately followed by a pulse and then the frame
    public static string StartOrder()
    {
        var sequencer = Object.FindAnyObjectByType<FrameSequenceManager>();
        var frames = Object.FindAnyObjectByType<FrameManager>();

        frames.GoToSelectedFrame(1);
        Drain();
        sequencer.TogglePlayPause(frames.GetCurrentFrameNumber());
        System.Threading.Thread.Sleep(60);
        sequencer.Stop();
        sequencer.Stop();
        System.Threading.Thread.Sleep(50);

        var sb = new StringBuilder();
        long t0 = -1;
        int shown = 0;

        while (Queue.TryDequeue(out var e))
        {
            if (t0 < 0 && e.Kind != MidiMessageKind.Start) continue;
            if (t0 < 0) t0 = e.Timestamp;
            if (shown++ > 8) break;

            string text = e.Kind == MidiMessageKind.ControlChange ? $"CC{e.Data1}={e.Data2}" : e.Kind.ToString();
            sb.AppendLine($"{Ms(e.Timestamp - t0),8:F3} ms  {text}");
        }

        return sb.ToString();
    }

    public static string LatchShiftRight()
    {
        var transforms = Object.FindAnyObjectByType<TransformManager>();
        var frames = Object.FindAnyObjectByType<FrameManager>();

        frames.GoToSelectedFrame(1);
        transforms.SetTransformType(TransformType.Shift);
        transforms.ToggleWrapping();   // wrap so the emitter keeps moving
        transforms.ToggleLatching();
        transforms.HandleDirectionChange(frames.GetCurrentFrameSnapshot(), 3);

        return Drain();
    }

    public static string Unlatch()
    {
        Object.FindAnyObjectByType<TransformManager>().ResetTransforms();
        return "transforms reset";
    }

    public static string ModeInternal()
    {
        var clockSettings = Object.FindAnyObjectByType<ClockSettingsManager>();
        var engine = Object.FindAnyObjectByType<ClockEngine>();
        // Internal, Send and Receive cycle, so step until Internal comes round
        for (int i = 0; i < 3 && engine.Mode != ClockMode.Internal; i++) clockSettings.StepClockMode(1);
        return "mode=" + engine.Mode;
    }

    public static string Cleanup()
    {
        Object.FindAnyObjectByType<MidiOutputScheduler>().ClearOutputDevice();
        AppDomain.CurrentDomain.SetData("tm_cap", null);
        return "cleaned";
    }

    static string Stats(List<double> values)
    {
        if (values.Count == 0) return "n=0";

        double mean = 0;
        foreach (var v in values) mean += v;
        mean /= values.Count;

        double sd = 0, min = double.MaxValue, max = double.MinValue;
        foreach (var v in values)
        {
            sd += (v - mean) * (v - mean);
            min = Math.Min(min, v);
            max = Math.Max(max, v);
        }

        return $"n={values.Count} mean={mean:F3} sd={Math.Sqrt(sd / values.Count):F3} min={min:F3} max={max:F3}";
    }
}
