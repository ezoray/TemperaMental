using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using TemperaMental.Midi.Transforms;
using TemperaMental.Transforms;
using TemperaMental.Utils;
using Object = UnityEngine.Object;

// Play-mode tests for notes, canvas recall and Hold. A fake output device records what the app sends.
public static class VoiceTestHarness
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

    static ConcurrentQueue<MidiMessageData> Captured => (ConcurrentQueue<MidiMessageData>)AppDomain.CurrentDomain.GetData("tm_voice_cap");

    static double Ms(long t) => t * 1000.0 / Stopwatch.Frequency;

    static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>();

    public static string Setup()
    {
        var captured = new ConcurrentQueue<MidiMessageData>();
        AppDomain.CurrentDomain.SetData("tm_voice_cap", captured);

        Find<MidiOutputScheduler>().SetOutputDevice(new CapturingDevice(captured));
        Find<MidiTempoManager>().SetBpm(120);
        Find<ClockEngine>().SetClockMode(ClockMode.Internal);

        Thread.Sleep(60);
        Drain();
        return "ready";
    }

    static void Drain() { while (Captured.TryDequeue(out _)) { } }

    static string Label(MidiMessageData m) => m.Kind switch
    {
        MidiMessageKind.ControlChange => $"CC{m.Data1}={m.Data2}",
        MidiMessageKind.NoteOn => $"NoteOn ch{m.Channel + 1} {FrameEvent.NoteName(m.Data1)} v{m.Data2}",
        MidiMessageKind.NoteOff => $"NoteOff ch{m.Channel + 1} {FrameEvent.NoteName(m.Data1)}",
        MidiMessageKind.ProgramChange => $"Program ch{m.Channel + 1} {m.Data1}",
        _ => m.Kind.ToString()
    };

    // what was sent since the reference, without emitter CCs or clock
    static string Sent(long reference, bool notesOnly = true)
    {
        var lines = new List<string>();
        while (Captured.TryDequeue(out var m))
        {
            if (m.Kind == MidiMessageKind.TimingClock) continue;
            if (notesOnly && m.Kind == MidiMessageKind.ControlChange) continue;
            lines.Add($"  {Ms(m.Timestamp - reference),8:F1} ms  {Label(m)}");
        }
        return lines.Count == 0 ? "  (nothing)\n" : string.Join("\n", lines) + "\n";
    }

    static ulong[] Groups(params (int emitter, int cell)[] cells)
    {
        var g = new ulong[4];
        foreach (var (e, c) in cells) g[e] |= 1UL << c;
        return g;
    }

    static void SetFrames(params FrameSnapshot[] content) => Find<FrameManager>().SetFrames(content.Select(c => new Frame(c)).ToList());

    // three frames: a short C4 in frame 1, E4 on channel 2 held from frame 2 into frame 3
    static FrameSnapshot[] NoteFrames() => new[]
    {
        new FrameSnapshot(Groups((1, 18)), new[] { FrameEvent.NoteOn(0, 0, 60, 100), FrameEvent.NoteOff(48, 0, 60) }),
        new FrameSnapshot(Groups((0, 3)), new[] { FrameEvent.NoteOn(24, 1, 64, 90) }),
        new FrameSnapshot(Groups((2, 40)), new[] { FrameEvent.NoteOff(72, 1, 64) }),
    };

    // plays from frame 1 to the end; notes at their ticks
    public static string PlayNotes()
    {
        var sequencer = Find<FrameSequenceManager>();
        SetFrames(NoteFrames());
        Find<FrameManager>().GoToSelectedFrame(1);
        Thread.Sleep(80); Drain();

        long start = Stopwatch.GetTimestamp();
        sequencer.TogglePlayPause(1);
        Thread.Sleep(1700);
        return "play from 1 (expect C4 on at 0 and off at 250; E4 on at 625 and off at 1375):\n" + Sent(start) + $"state {sequencer.State}";
    }

    // starts at frame 3, where E4 is already held: it's turned on at the start, then off at tick 72
    public static string StartInside()
    {
        var sequencer = Find<FrameSequenceManager>();
        SetFrames(NoteFrames());
        Find<FrameManager>().GoToSelectedFrame(3);
        Thread.Sleep(80); Drain();

        long start = Stopwatch.GetTimestamp();
        sequencer.TogglePlayPause(3);
        Thread.Sleep(700);
        return "play from 3 (expect E4 on at 0, off at 375):\n" + Sent(start) + $"state {sequencer.State}";
    }

    // pauses part way through E4, which is released; resuming turns it back on
    public static string PauseResume()
    {
        var sequencer = Find<FrameSequenceManager>();
        SetFrames(NoteFrames());
        Find<FrameManager>().GoToSelectedFrame(1);
        Thread.Sleep(80); Drain();

        long start = Stopwatch.GetTimestamp();
        sequencer.TogglePlayPause(1);
        Thread.Sleep(800);              // frame 2, after E4 started at 625
        sequencer.TogglePlayPause(1);   // pause: E4 off
        Thread.Sleep(200);
        sequencer.TogglePlayPause(1);   // resume: frame 2 again from its start, E4 on at its tick
        Thread.Sleep(400);
        sequencer.Stop(); sequencer.Stop();
        Thread.Sleep(100);
        return "pause at 800, resume at 1000, stop at 1400:\n" + Sent(start);
    }

    // looping frames 1-3: notes are right on each pass
    public static string Loop()
    {
        var sequencer = Find<FrameSequenceManager>();
        SetFrames(NoteFrames());
        Find<FrameManager>().GoToSelectedFrame(1);
        sequencer.ToggleLooping();
        Thread.Sleep(80); Drain();

        long start = Stopwatch.GetTimestamp();
        sequencer.TogglePlayPause(1);
        Thread.Sleep(3100);
        sequencer.Stop(); sequencer.Stop();
        sequencer.ToggleLooping();
        Thread.Sleep(100);
        return "two passes of 3 frames, then stop:\n" + Sent(start);
    }

    // canvas: three quick steps send one Program Change once they stop (CanvasCheck after a pause, Unity running)
    public static string CanvasStep()
    {
        Drain();
        var canvas = Find<CanvasManager>();
        canvas.StepCanvas(1); canvas.StepCanvas(1); canvas.StepCanvas(1);
        AppDomain.CurrentDomain.SetData("tm_voice_t", Stopwatch.GetTimestamp());
        return "canvas now " + canvas.Program + " (shown " + (canvas.Program + 1) + ")";
    }

    public static string CanvasCheck()
    {
        long start = (long)AppDomain.CurrentDomain.GetData("tm_voice_t");
        var status = UnityEngine.GameObject.Find("Canvas/MainPanel/DisplayPanel/MainView/DisplayCanvas/SoundStatus").GetComponent<TMPro.TextMeshProUGUI>().text;
        return "sent after the steps:\n" + Sent(start, false) + "status: [" + status + "]";
    }

    // a loaded file's canvas is sent at once; saving writes it, and loading reads it back
    public static string CanvasFile()
    {
        var sb = new StringBuilder();
        long start = Stopwatch.GetTimestamp();
        Find<SoundEventController>().ActionOnCanvasLoaded(4);
        Thread.Sleep(50);
        sb.Append("loaded canvas 4:\n" + Sent(start, false));

        var service = Find<MidiTransformService>();
        var frames = NoteFrames().Select(c => new Frame(c)).ToList();
        MidiFile file = service.FromFramesToMidiFile(frames, 120, 4);
        byte[] bytes;
        using (var stream = new MemoryStream()) { file.Write(stream, MidiFileFormat.SingleTrack); bytes = stream.ToArray(); }
        MidiFile reread;
        using (var stream = new MemoryStream(bytes)) reread = MidiFile.Read(stream);

        var loaded = service.FromMidiFileToFrames(reread);
        bool same = loaded.Count == frames.Count && loaded.Zip(frames, (a, b) => a.GetSnapshot().HasSameContent(b.GetSnapshot())).All(x => x);
        sb.AppendLine($"file: program {MidiUtils.GetProgramFromMidiFile(reread)}, {loaded.Count} frames, notes round trip {(same ? "identical" : "DIFFERENT")}");
        for (int i = 0; i < loaded.Count; i++)
            sb.AppendLine($"  frame {i + 1}: {string.Join(", ", loaded[i].Events.Select(e => e.ToString()))}");

        var none = service.FromFramesToMidiFile(frames, 120);
        sb.AppendLine("file without a canvas: program " + MidiUtils.GetProgramFromMidiFile(none));

        Find<SoundEventController>().ActionOnCanvasLoaded(-1);
        return sb.ToString();
    }

    // Hold: a note on the MIDI channel, moved when the channel changes, released when switched off
    public static string Hold()
    {
        var hold = Find<HoldNoteManager>();
        var sound = Find<SoundEventController>();
        Drain();
        long start = Stopwatch.GetTimestamp();

        sound.OnClickHold();
        Thread.Sleep(30);
        sound.ActionOnMidiChannelChanged(3);
        Thread.Sleep(30);
        sound.OnClickHold();
        Thread.Sleep(30);
        sound.ActionOnMidiChannelChanged(1);

        return "hold on, channel 3, hold off, channel 1:\n" + Sent(start) + "holding " + hold.IsHolding;
    }

    // quitting releases notes still on: the clock is switched off (as at quit) and back on
    public static string QuitRelease()
    {
        var hold = Find<HoldNoteManager>();
        var engine = Find<ClockEngine>();
        hold.SetHold(true);
        Thread.Sleep(50);
        Drain();

        long start = Stopwatch.GetTimestamp();
        engine.enabled = false;     // OnDisable: stops the clock thread, then releases notes
        string released = Sent(start);
        engine.enabled = true;
        hold.SetHold(false);        // already released on the device; this just turns the switch off
        Thread.Sleep(50);
        Drain();

        return "clock disabled with Hold on:\n" + released;
    }

    // transforms: Shift moves emitter events and leaves notes; Random drops emitter events and keeps notes
    public static string Transforms()
    {
        var frames = Find<FrameManager>();
        var transforms = Find<TransformManager>();
        var sb = new StringBuilder();
        var content = new FrameSnapshot(Groups((1, 18)), new[] { FrameEvent.NoteOn(0, 0, 60, 100), FrameEvent.Place(24, 0, 10), FrameEvent.NoteOff(48, 0, 60) });

        SetFrames(content);
        transforms.SetTransformType(TransformType.Shift);
        transforms.HandleDirectionChange(frames.GetCurrentFrameSnapshot(), 3);
        sb.AppendLine("shift right: " + string.Join(", ", frames.GetCurrentFrameSnapshot().Events.Select(e => e.ToString())));

        SetFrames(content);
        transforms.SetTransformType(TransformType.Random);
        transforms.HandleDirectionChange(frames.GetCurrentFrameSnapshot(), 3);
        sb.AppendLine("random right: " + string.Join(", ", frames.GetCurrentFrameSnapshot().Events.Select(e => e.ToString())));

        SetFrames(content);
        transforms.RandomiseEmitters(frames.GetCurrentFrameEmitters(), 5);
        sb.AppendLine("random slider: " + string.Join(", ", frames.GetCurrentFrameSnapshot().Events.Select(e => e.ToString())));

        transforms.SetTransformType(TransformType.Shift);
        Drain();
        return sb.ToString();
    }

    public static string Cleanup()
    {
        Find<HoldNoteManager>().SetHold(false);
        Thread.Sleep(30);
        Find<MidiOutputScheduler>().ClearOutputDevice();
        AppDomain.CurrentDomain.SetData("tm_voice_cap", null);
        return "cleaned";
    }
}
