using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Midi.Clock;
using TemperaMental.Midi.Core;
using TemperaMental.Midi.Output;
using TemperaMental.Midi.Playbacks;
using TemperaMental.Midi.Transforms;
using TemperaMental.Settings;
using TemperaMental.Transforms;
using TemperaMental.Utils;
using Object = UnityEngine.Object;

// Play-mode tests for frame events (phase 5). A fake output device records what the app sends, timestamped on the
// clock thread; external clock is simulated by a thread feeding ClockEngine.ReceiveExternal. No CoreMIDI ports.
public static class EventsTestHarness
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

    static readonly string[] Colours = { "Blue", "Red", "Yellow", "Green" };

    static ConcurrentQueue<MidiMessageData> Captured => (ConcurrentQueue<MidiMessageData>)AppDomain.CurrentDomain.GetData("tm_ev_cap");

    static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>();

    public static string Setup()
    {
        var captured = new ConcurrentQueue<MidiMessageData>();
        AppDomain.CurrentDomain.SetData("tm_ev_cap", captured);

        Find<MidiOutputScheduler>().SetOutputDevice(new CapturingDevice(captured));
        Find<ClockEventController>().ActionOnClockRoutingChanged(new ClockRouting(null, ClockDestination.SameAsOut, null, true));
        Find<MidiTempoManager>().SetBpm(120);
        Find<ClockEngine>().SetClockMode(ClockMode.Internal);

        Thread.Sleep(60);
        Drain();

        return "ready";
    }

    static void Drain()
    {
        while (Captured.TryDequeue(out _)) { }
    }

    static ulong[] Groups(params (int emitter, int cell)[] cells)
    {
        var g = new ulong[4];
        foreach (var (e, c) in cells) g[e] |= 1UL << c;
        return g;
    }

    static string Describe(ulong[] groups)
    {
        var cells = new List<string>();
        for (int c = 0; c < 4; c++)
            for (int i = 0; i < 64; i++)
                if ((groups[c] & (1UL << i)) != 0) cells.Add($"{Colours[c]}@{i}");
        return "[" + string.Join(" ", cells) + "]";
    }

    static string Describe(FrameSnapshot s) => Describe(s.StartGroups) + " {" + string.Join(", ", s.Events.Select(e => e.ToString())) + "}";

    // replaces the frames with these, current frame 1
    static void SetFrames(params FrameSnapshot[] content)
    {
        var frames = Find<FrameManager>();
        var list = content.Select(c => new Frame(c)).ToList();
        frames.SetFrames(list);
    }

    // two frames used by the playback tests
    static FrameSnapshot[] PlaybackFrames() => new[]
    {
        new FrameSnapshot(Groups((1, 18)), new[]
        {
            FrameEvent.Place(24, 0, 10),
            FrameEvent.Remove(48, 18),
            FrameEvent.Select(72, 3)
        }),
        new FrameSnapshot(Groups(), new[]
        {
            FrameEvent.Place(0, 2, 40),
            FrameEvent.ClearAll(95)
        })
    };

    static string Label(MidiMessageData m) => m.Kind == MidiMessageKind.ControlChange ? $"CC{m.Data1}={m.Data2}" : m.Kind.ToString();

    // groups captured CCs into bursts (gaps under 1 ms) and reports each one from the reference time
    static string Bursts(List<MidiMessageData> sent, long reference, bool includeClock)
    {
        var sb = new StringBuilder();
        long burstStart = 0, last = 0;
        var labels = new List<string>();

        void Flush()
        {
            if (labels.Count == 0) return;
            sb.AppendLine($"  {Ms(burstStart - reference),9:F3} ms  {string.Join(" ", labels)}");
            labels.Clear();
        }

        foreach (var m in sent)
        {
            bool isClock = m.Kind == MidiMessageKind.TimingClock;
            if (isClock && !includeClock) continue;

            if (labels.Count == 0 || Ms(m.Timestamp - last) > 1.0)
            {
                Flush();
                burstStart = m.Timestamp;
            }

            labels.Add(Label(m));
            last = m.Timestamp;
        }

        Flush();
        return sb.ToString();
    }

    // compares the first message of each expected event against its ideal time
    static string Check(List<MidiMessageData> sent, long reference, double beatMs, (double beats, string first)[] expected)
    {
        var sb = new StringBuilder();
        var ccs = sent.Where(m => m.Kind == MidiMessageKind.ControlChange).ToList();
        int from = 0;
        double worst = 0;

        foreach (var (beats, first) in expected)
        {
            double ideal = beats * beatMs;
            int index = -1;

            for (int i = from; i < ccs.Count; i++)
            {
                if (Label(ccs[i]) == first && Ms(ccs[i].Timestamp - reference) > ideal - 3) { index = i; break; }
            }

            if (index < 0) { sb.AppendLine($"  MISSING {first} at {ideal:F3}"); continue; }

            double actual = Ms(ccs[index].Timestamp - reference);
            double error = actual - ideal;
            worst = Math.Max(worst, Math.Abs(error));
            sb.AppendLine($"  ideal {ideal,8:F3}  actual {actual,8:F3}  error {error,7:F3} ms  {first}");
            from = index + 1;
        }

        sb.AppendLine($"  worst |error| {worst:F3} ms");
        return sb.ToString();
    }

    static readonly (double, string)[] PlaybackExpected =
    {
        (0, "CC10=1"),                 // frame 1 start: Red@18
        (24 / 96.0, "CC10=0"),         // place Blue@10
        (48 / 96.0, "CC12=18"),        // remove 18
        (72 / 96.0, "CC10=3"),         // select Green
        (1, "CC10=0"),                 // frame 2 start diff: remove Blue@10
        (1, "CC10=2"),                 // tick 0: place Yellow@40
        (1 + 95 / 96.0, "CC12=64"),    // clear all
    };

    // plays the two test frames once with the internal clock (mode "internal") or while sending clock ("send")
    public static string Play(string mode)
    {
        var engine = Find<ClockEngine>();
        var sequencer = Find<FrameSequenceManager>();
        var frames = Find<FrameManager>();

        engine.SetClockMode(mode == "send" ? ClockMode.Send : ClockMode.Internal);
        SetFrames(PlaybackFrames());

        // the device shows frame 2's start before play, so frame 1's start is a visible change on beat 0
        frames.GoToSelectedFrame(2);
        Thread.Sleep(80);
        Drain();

        long pressed = Stopwatch.GetTimestamp();
        sequencer.TogglePlayPause(1);
        Thread.Sleep(1300);

        var sent = new List<MidiMessageData>();
        while (Captured.TryDequeue(out var m)) sent.Add(m);

        // beat 0 is the first F8 when sending clock, otherwise frame 1's start
        long reference = mode == "send"
            ? sent.First(m => m.Kind == MidiMessageKind.TimingClock).Timestamp
            : sent.First(m => m.Kind == MidiMessageKind.ControlChange).Timestamp;

        int pulses = sent.Count(m => m.Kind == MidiMessageKind.TimingClock);
        engine.SetClockMode(ClockMode.Internal);

        return $"mode {mode}: beat 0 at {Ms(reference - pressed):F3} ms after Play, {pulses} pulses, state {sequencer.State}\n"
               + Bursts(sent, reference, false) + Check(sent, reference, 500, PlaybackExpected);
    }

    // reverse: frames backwards, each frame's events forwards
    public static string Reverse()
    {
        var sequencer = Find<FrameSequenceManager>();
        var frames = Find<FrameManager>();

        SetFrames(PlaybackFrames());
        frames.GoToSelectedFrame(1);
        sequencer.ToggleReverse();
        Thread.Sleep(80);
        Drain();

        sequencer.TogglePlayPause(2);
        Thread.Sleep(1300);
        sequencer.ToggleReverse();

        var sent = new List<MidiMessageData>();
        while (Captured.TryDequeue(out var m)) sent.Add(m);
        long reference = sent.First(m => m.Kind == MidiMessageKind.ControlChange).Timestamp;

        return "reverse from frame 2:\n" + Bursts(sent, reference, false);
    }

    // follows a simulated external clock at 120 BPM. "warm": 48 pulses before Start so the tempo is measured;
    // "cold": Start straight away, so the first events wait for a pulse
    public static string Receive(string kind)
    {
        var engine = Find<ClockEngine>();
        var sequencer = Find<FrameSequenceManager>();
        var frames = Find<FrameManager>();

        SetFrames(
            new FrameSnapshot(Groups((1, 18)), new[] { FrameEvent.Place(2, 0, 10), FrameEvent.Remove(50, 18), FrameEvent.Place(95, 3, 63) }),
            new FrameSnapshot(Groups(), new[] { FrameEvent.Place(30, 2, 40) }));
        frames.GoToSelectedFrame(1);

        engine.SetClockMode(ClockMode.Receive);
        Thread.Sleep(80);
        Drain();

        double pulseMs = 500.0 / 24;
        long pulseTicks = (long)(Stopwatch.Frequency * pulseMs / 1000);
        int prePulses = kind == "warm" ? 48 : 0;
        long firstAfterStart = 0;

        var master = new Thread(() =>
        {
            long next = Stopwatch.GetTimestamp() + pulseTicks;

            for (int i = 0; i < prePulses + 1 + 56; i++)
            {
                if (i == prePulses)
                {
                    engine.ReceiveExternal(new MidiMessageData(Stopwatch.GetTimestamp(), MidiMessageKind.Start));
                }

                while (Stopwatch.GetTimestamp() < next) Thread.SpinWait(10);
                long now = Stopwatch.GetTimestamp();
                if (i == prePulses) firstAfterStart = now;
                engine.ReceiveExternal(new MidiMessageData(now, MidiMessageKind.TimingClock));
                next += pulseTicks;
            }

            engine.ReceiveExternal(new MidiMessageData(Stopwatch.GetTimestamp(), MidiMessageKind.Stop));
        }) { Priority = ThreadPriority.Highest };

        master.Start();
        master.Join();
        Thread.Sleep(50);

        var sent = new List<MidiMessageData>();
        while (Captured.TryDequeue(out var m)) sent.Add(m);
        var afterStart = sent.Where(m => m.Timestamp >= firstAfterStart - Stopwatch.Frequency / 1000).ToList();

        engine.SetClockMode(ClockMode.Internal);

        var expected = new (double, string)[]
        {
            (0, "CC10=1"),
            (2 / 96.0, "CC10=0"),
            (50 / 96.0, "CC12=18"),
            (95 / 96.0, "CC10=3"),
            (1 + 30 / 96.0, "CC10=2"),
        };

        return $"receive {kind}: measured tempo {engine.MeasuredTempo:F2}, state {sequencer.State}\n"
               + Bursts(afterStart, firstAfterStart, false) + Check(afterStart, firstAfterStart, 500, expected);
    }

    // starts playing the test frames and stops part way through frame 1 (at about tick 60), so a later call can see
    // what was sent once the main thread caught up
    public static string PauseStart()
    {
        var sequencer = Find<FrameSequenceManager>();
        var frames = Find<FrameManager>();

        SetFrames(PlaybackFrames());
        frames.GoToSelectedFrame(1);
        Thread.Sleep(80);
        Drain();

        sequencer.TogglePlayPause(1);
        Thread.Sleep(310);
        sequencer.TogglePlayPause(1);
        Thread.Sleep(20);

        var sent = new List<MidiMessageData>();
        while (Captured.TryDequeue(out var m)) sent.Add(m);

        return "before pause:" + string.Join(" ", sent.Select(Label)) + $"  state {sequencer.State}";
    }

    public static string PauseCheck()
    {
        var frames = Find<FrameManager>();
        Thread.Sleep(30);

        var sent = new List<MidiMessageData>();
        while (Captured.TryDequeue(out var m)) sent.Add(m);

        return "after pause, sent: " + string.Join(" ", sent.Select(Label)) + "\nframe now " + Describe(frames.GetCurrentFrameSnapshot());
    }

    // records what the grid is told during playback, for DisplayCheck to report
    public static string DisplayStart()
    {
        var sequencer = Find<FrameSequenceManager>();
        var frames = Find<FrameManager>();

        var log = new List<string>();
        AppDomain.CurrentDomain.SetData("tm_ev_display", log);

        var field = typeof(FrameSequenceManager).GetField("onPlaybackEmittersChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        var ev = (UnityEngine.Events.UnityEvent<FrameDetail>)field.GetValue(sequencer);
        long start = Stopwatch.GetTimestamp();
        AppDomain.CurrentDomain.SetData("tm_ev_display_start", start);

        if (AppDomain.CurrentDomain.GetData("tm_ev_display_hooked") == null)
        {
            ev.AddListener(d =>
            {
                var l = (List<string>)AppDomain.CurrentDomain.GetData("tm_ev_display");
                long s = (long)AppDomain.CurrentDomain.GetData("tm_ev_display_start");
                l?.Add($"{Ms(Stopwatch.GetTimestamp() - s),8:F1} ms  frame {d.FrameNumber}/{d.FrameTotal} {Describe(d.EmitterGroups)} count {d.EmitterCount}");
            });
            AppDomain.CurrentDomain.SetData("tm_ev_display_hooked", true);
        }

        SetFrames(PlaybackFrames());
        frames.GoToSelectedFrame(1);
        sequencer.TogglePlayPause(1);

        return "playing";
    }

    public static string DisplayCheck()
    {
        var log = (List<string>)AppDomain.CurrentDomain.GetData("tm_ev_display");
        Drain();
        return "grid updates while events played (main thread, so to the nearest Unity frame):\n  " + string.Join("\n  ", log);
    }

    // transforms carrying events, using immediate (unlatched) presses
    public static string Transforms()
    {
        var frames = Find<FrameManager>();
        var transforms = Find<TransformManager>();
        var emitterSettings = Find<EmitterSettingsManager>();
        var sb = new StringBuilder();

        var start = new FrameSnapshot(Groups((1, 18)), new[]
        {
            FrameEvent.Place(24, 0, 10),
            FrameEvent.Remove(48, 18),
            FrameEvent.Select(72, 3),
            FrameEvent.Place(80, 2, 60),
        });

        void Try(string name, TransformType type, int direction, FrameSnapshot input = null)
        {
            SetFrames(input ?? start);
            transforms.SetTransformType(type);
            transforms.HandleDirectionChange(frames.GetCurrentFrameSnapshot(), direction);
            sb.AppendLine($"{name,-22} {Describe(frames.GetCurrentFrameSnapshot())}");
        }

        sb.AppendLine($"{"start",-22} {Describe(start)}");
        Try("shift right", TransformType.Shift, 3);
        Try("shift up", TransformType.Shift, 0);
        Try("flip left/right", TransformType.Flip, 2);
        Try("flip up/down", TransformType.Flip, 0);
        Try("rotate clockwise", TransformType.Rotate, 3);
        Try("swap right", TransformType.Swap, 3);
        Try("swap up (Blue<->Green)", TransformType.Swap, 0);
        Try("random up (count only)", TransformType.Random, 0);
        Try("random right", TransformType.Random, 3);

        // 2-Lane: Blue owns columns 0-1, so a Red event shifted from column 2 into column 1 becomes Blue
        emitterSettings.ToggleTwoLane(0);
        Try("2-Lane Blue, shift left", TransformType.Shift, 2);
        emitterSettings.ToggleTwoLane(0);

        // an event with nothing to remove is dropped when moved
        Try("remove on empty cell", TransformType.Shift, 3, new FrameSnapshot(Groups(), new[] { FrameEvent.Remove(10, 5) }));

        transforms.SetTransformType(TransformType.Shift);
        Drain();
        return sb.ToString();
    }

    // latched Shift Right at Per Beat: two beats pass while the main thread is held here, then LatchCheck reports
    public static string LatchStart()
    {
        var frames = Find<FrameManager>();
        var transforms = Find<TransformManager>();

        SetFrames(new FrameSnapshot(Groups((1, 18)), new[] { FrameEvent.Place(24, 0, 10), FrameEvent.Remove(48, 18) }));
        transforms.SetTransformType(TransformType.Shift);
        transforms.SetTransformRate(1);
        transforms.ToggleLatching();
        transforms.HandleDirectionChange(frames.GetCurrentFrameSnapshot(), 3);
        Thread.Sleep(1050);
        transforms.UnlatchTransforms();

        return "latched for about two beats";
    }

    public static string LatchCheck()
    {
        var frames = Find<FrameManager>();
        var sb = new StringBuilder();
        var all = frames.GetFrames();
        for (int i = 0; i < all.Count; i++) sb.AppendLine($"frame {i + 1}: {Describe(all[i].GetSnapshot())}");
        Drain();
        return sb.ToString();
    }

    // copy, paste, duplicate, clear, recording a transform, and the end state
    public static string Edits()
    {
        var frames = Find<FrameManager>();
        var transforms = Find<TransformManager>();
        var sb = new StringBuilder();

        var content = new FrameSnapshot(Groups((1, 18)), new[] { FrameEvent.Place(24, 0, 10), FrameEvent.Remove(48, 18) });
        SetFrames(content);

        sb.AppendLine("end state " + Describe(frames.GetCurrentFrameSnapshot().GetEndGroups()));

        frames.DuplicateFrame();
        sb.AppendLine("duplicate -> frame 2 " + Describe(frames.GetCurrentFrameSnapshot()));

        frames.CopyFrame();
        frames.InsertFrame();
        frames.PasteOntoFrame();
        sb.AppendLine("paste -> frame 3 " + Describe(frames.GetCurrentFrameSnapshot()));

        frames.ClearFrame();
        sb.AppendLine("clear -> frame 3 " + Describe(frames.GetCurrentFrameSnapshot()));

        // unsorted events are put in tick order, keeping same-tick order; ticks outside the beat are clamped
        frames.SetCurrentFrameEvents(new[] { FrameEvent.Place(50, 1, 3), FrameEvent.Select(10, 2), FrameEvent.Place(10, 2, 4), FrameEvent.Remove(200, 3), FrameEvent.Place(-5, 9, 1) });
        sb.AppendLine("sorted -> " + Describe(frames.GetCurrentFrameSnapshot()));

        // recording: a transform result becomes a new frame after the current one, events included
        SetFrames(content);
        frames.ToggleRecording();
        transforms.SetTransformType(TransformType.Shift);
        transforms.HandleDirectionChange(frames.GetCurrentFrameSnapshot(), 3);
        frames.ToggleRecording();
        var all = frames.GetFrames();
        sb.AppendLine($"recording shift right -> {all.Count} frames: 1 {Describe(all[0].GetSnapshot())} | 2 {Describe(all[1].GetSnapshot())}");

        Drain();
        return sb.ToString();
    }

    // saves frames with events and loads them back; also loads a file in the old format
    public static string Files()
    {
        var service = Find<MidiTransformService>();
        var sb = new StringBuilder();

        var original = new List<Frame>
        {
            new Frame(new FrameSnapshot(Groups((1, 18), (3, 63)), new[]
            {
                FrameEvent.Select(10, 1),
                FrameEvent.Place(10, 1, 5),
                FrameEvent.Place(10, 2, 6),
                FrameEvent.Select(20, 3),
                FrameEvent.Remove(30, 5),
                FrameEvent.ClearAll(40),
                FrameEvent.Place(0, 0, 0),
            })),
            new Frame(Groups((2, 33))),
            new Frame(new FrameSnapshot(Groups(), new[] { FrameEvent.Place(0, 0, 1), FrameEvent.Select(95, 2) })),
        };

        MidiFile file = service.FromFramesToMidiFile(original, 133);

        // as MidiFileService saves it
        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            file.Write(stream, MidiFileFormat.SingleTrack);
            bytes = stream.ToArray();
        }

        MidiFile reread;
        using (var stream = new MemoryStream(bytes)) reread = MidiFile.Read(stream);
        sb.AppendLine($"tracks in file: {reread.GetTrackChunks().Count()}");

        sb.AppendLine($"format 2: {bytes.Length} bytes, TPQN {((TicksPerQuarterNoteTimeDivision)reread.TimeDivision).TicksPerQuarterNote}, bpm {MidiUtils.GetBpmFromMidiFile(reread)}");

        var loaded = service.FromMidiFileToFrames(reread);
        bool allSame = loaded.Count == original.Count;

        for (int i = 0; i < Math.Min(loaded.Count, original.Count); i++)
        {
            bool same = loaded[i].GetSnapshot().HasSameContent(original[i].GetSnapshot());
            allSame &= same;
            sb.AppendLine($"  frame {i + 1} {(same ? "same" : "DIFFERENT")}: {Describe(loaded[i].GetSnapshot())}");
        }

        sb.AppendLine("  round trip " + (allSame ? "identical" : "NOT identical") + $" ({loaded.Count} frames)");

        // the first frame as a DAW would see it
        var timed = reread.GetTimedEvents().Take(40).Select(t => $"{t.Time}:{(t.Event is ControlChangeEvent cc ? $"CC{cc.ControlNumber}={cc.ControlValue}" : t.Event is MarkerEvent mk ? "\"" + mk.Text + "\"" : t.Event.EventType.ToString())}");
        sb.AppendLine("  first events: " + string.Join(" ", timed));

        // the same file with markers and CCs in separate tracks, as the default multi-track save (or a DAW) writes it
        byte[] splitBytes;
        using (var stream = new MemoryStream())
        {
            service.FromFramesToMidiFile(original, 133).Write(stream, MidiFileFormat.MultiTrack);
            splitBytes = stream.ToArray();
        }

        MidiFile split;
        using (var stream = new MemoryStream(splitBytes)) split = MidiFile.Read(stream);
        var splitFrames = service.FromMidiFileToFrames(split);
        sb.AppendLine($"split tracks ({split.GetTrackChunks().Count()}): {splitFrames.Count} frames");
        for (int i = 0; i < splitFrames.Count; i++)
        {
            var fs = splitFrames[i].GetSnapshot();
            bool sameEnd = !EmitterUtils.CheckGroupsDifferent(fs.GetEndGroups(), original[i].GetSnapshot().GetEndGroups());
            sb.AppendLine($"  frame {i + 1} {Describe(fs)}  end state {(sameEnd ? "same" : "DIFFERENT")}");
        }

        // format 1, as the app used to write it: TPQN 120, events 1 tick apart, no format marker
        var legacy = new MidiFile { TimeDivision = new TicksPerQuarterNoteTimeDivision(120) };
        var chunk = new TrackChunk();
        chunk.Events.Add(new SetTempoEvent(60_000_000 / 128));
        using (var manager = chunk.ManageTimedEvents())
        {
            var frameCells = new[] { Groups((1, 18), (0, 3)), Groups(), Groups((3, 40)) };
            for (int f = 0; f < frameCells.Length; f++)
            {
                long t = f * 120;
                manager.Objects.Add(new TimedEvent(new MarkerEvent($"START_FRAME_NO_{f + 1}"), t++));
                for (byte c = 0; c < 4; c++)
                {
                    manager.Objects.Add(new TimedEvent(new ControlChangeEvent((SevenBitNumber)10, (SevenBitNumber)c), t++));
                    manager.Objects.Add(new TimedEvent(new ControlChangeEvent((SevenBitNumber)12, (SevenBitNumber)64), t++));
                }
                for (byte c = 0; c < 4; c++)
                {
                    if (frameCells[f][c] == 0) continue;
                    manager.Objects.Add(new TimedEvent(new ControlChangeEvent((SevenBitNumber)10, (SevenBitNumber)c), t++));
                    for (byte i = 0; i < 64; i++)
                        if ((frameCells[f][c] & (1UL << i)) != 0)
                            manager.Objects.Add(new TimedEvent(new ControlChangeEvent((SevenBitNumber)11, (SevenBitNumber)i), t++));
                }
            }
            manager.Objects.Add(new TimedEvent(new MarkerEvent("END_SEQ"), 360));
        }
        legacy.Chunks.Add(chunk);

        var legacyFrames = service.FromMidiFileToFrames(legacy);
        sb.AppendLine($"format 1: {legacyFrames.Count} frames, bpm {MidiUtils.GetBpmFromMidiFile(legacy)}");
        for (int i = 0; i < legacyFrames.Count; i++)
            sb.AppendLine($"  frame {i + 1}: {Describe(legacyFrames[i].GetSnapshot())}");

        return sb.ToString();
    }

    public static string Cleanup()
    {
        Find<ClockEngine>().SetClockMode(ClockMode.Internal);
        Find<MidiOutputScheduler>().ClearOutputDevice();
        AppDomain.CurrentDomain.SetData("tm_ev_cap", null);
        return "cleaned";
    }
}
