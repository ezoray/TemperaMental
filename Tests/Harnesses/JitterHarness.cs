using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TemperaMental.Midi.Clock;
using TemperaMental.Midi.Core;
using Object = UnityEngine.Object;

// Pulse spacing of the clock, from the fake output that ClockTestHarness.Setup installs (run that first; it also
// switches to Send mode so pulses are sent). Begin/End measure while Unity's main thread runs freely between the
// calls; Run measures while the call holds the main thread. Garbage collections during the run are counted.
public static class JitterHarness
{
    static double Ms(long t) => t * 1000.0 / Stopwatch.Frequency;

    public static string Begin()
    {
        var q = (ConcurrentQueue<MidiMessageData>)AppDomain.CurrentDomain.GetData("tm_cap");
        while (q.TryDequeue(out _)) { }
        AppDomain.CurrentDomain.SetData("tm_jit_start", Stopwatch.GetTimestamp());
        AppDomain.CurrentDomain.SetData("tm_jit_gc", GC.CollectionCount(0));
        return "begun";
    }

    // what the clock did while the main thread ran freely since Begin
    public static string End()
    {
        var q = (ConcurrentQueue<MidiMessageData>)AppDomain.CurrentDomain.GetData("tm_cap");
        long start = (long)AppDomain.CurrentDomain.GetData("tm_jit_start");
        int gcs0 = GC.CollectionCount(0) - (int)AppDomain.CurrentDomain.GetData("tm_jit_gc");
        return Report(q, start, gcs0, 0);
    }

    static string Report(ConcurrentQueue<MidiMessageData> q, long start, int gcs0, int gcs1)
    {
        var pulses = new List<long>();
        while (q.TryDequeue(out var m)) if (m.Kind == MidiMessageKind.TimingClock) pulses.Add(m.Timestamp);
        var intervals = new List<double>();
        for (int i = 1; i < pulses.Count; i++) intervals.Add(Ms(pulses[i] - pulses[i - 1]));
        double mean = intervals.Average();
        double sd = Math.Sqrt(intervals.Select(x => (x - mean) * (x - mean)).Average());
        var worst = intervals.Select((x, i) => (x, i)).OrderByDescending(p => Math.Abs(p.x - mean)).Take(6)
            .Select(p => $"{p.x:F3}@{Ms(pulses[p.i + 1] - start):F0}ms");
        int off = intervals.Count(x => Math.Abs(x - mean) > 0.5);
        return $"pulses {pulses.Count}, mean {mean:F4}, sd {sd:F4}, over 0.5 ms off: {off}, gc0 {gcs0}, gc1 {gcs1}\n  worst: {string.Join(", ", worst)}";
    }

    // detaches (0) or reattaches (1) the sequencer's tick listener, to compare
    public static string SequencerTicks(string on)
    {
        var engine = Object.FindAnyObjectByType<ClockEngine>();
        var seq = Object.FindAnyObjectByType<TemperaMental.Midi.Playbacks.FrameSequenceManager>();
        var method = typeof(TemperaMental.Midi.Playbacks.FrameSequenceManager).GetMethod("HandleTick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var handler = (Action<ClockTick>)Delegate.CreateDelegate(typeof(Action<ClockTick>), seq, method);
        engine.OnTick -= handler;
        if (on == "1") engine.OnTick += handler;
        return "sequencer ticks " + (on == "1" ? "on" : "off");
    }

    public static string Run(string seconds)
    {
        var q = (ConcurrentQueue<MidiMessageData>)AppDomain.CurrentDomain.GetData("tm_cap");
        while (q.TryDequeue(out _)) { }
        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1);
        long start = Stopwatch.GetTimestamp();
        Thread.Sleep((int)(double.Parse(seconds) * 1000));
        int gcs0 = GC.CollectionCount(0) - gc0, gcs1 = GC.CollectionCount(1) - gc1;

        var pulses = new List<long>();
        while (q.TryDequeue(out var m)) if (m.Kind == MidiMessageKind.TimingClock) pulses.Add(m.Timestamp);
        var intervals = new List<double>();
        for (int i = 1; i < pulses.Count; i++) intervals.Add(Ms(pulses[i] - pulses[i - 1]));
        double mean = intervals.Average();
        double sd = Math.Sqrt(intervals.Select(x => (x - mean) * (x - mean)).Average());
        var worst = intervals.Select((x, i) => (x, i)).OrderByDescending(p => Math.Abs(p.x - mean)).Take(6)
            .Select(p => $"{p.x:F3}@{Ms(pulses[p.i + 1] - start):F0}ms");
        int off = intervals.Count(x => Math.Abs(x - mean) > 0.5);
        return $"pulses {pulses.Count}, mean {mean:F4}, sd {sd:F4}, over 0.5 ms off: {off}, gc0 {gcs0}, gc1 {gcs1}\n  worst: {string.Join(", ", worst)}";
    }
}
