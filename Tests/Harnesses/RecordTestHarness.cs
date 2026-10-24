using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Midi.Clock;
using TemperaMental.Midi.Core;
using TemperaMental.Midi.Input;
using TemperaMental.Midi.Output;
using TemperaMental.Midi.Playbacks;
using TemperaMental.Recording;
using TemperaMental.Settings;
using TemperaMental.Utils;
using Object = UnityEngine.Object;

// Play-mode tests for recording from the Tempera (phase 6). Notes are fed to the decoder on the main thread at set
// times while the recorder's and sequencer's Update are pumped every few ms (Unity's own loop is held by the call).
// A fake output device records what's sent; external clock is simulated by a background thread. No CoreMIDI ports.
public static class RecordTestHarness
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

    static ConcurrentQueue<MidiMessageData> Captured => (ConcurrentQueue<MidiMessageData>)AppDomain.CurrentDomain.GetData("tm_rec_cap");

    static double Ms(long t) => t * 1000.0 / Stopwatch.Frequency;

    static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>();

    static StringBuilder report;
    static long origin;

    public static string Setup()
    {
        var captured = new ConcurrentQueue<MidiMessageData>();
        AppDomain.CurrentDomain.SetData("tm_rec_cap", captured);

        Find<MidiOutputScheduler>().SetOutputDevice(new CapturingDevice(captured));
        Find<ClockEventController>().ActionOnClockRoutingChanged(new ClockRouting(null, ClockDestination.SameAsOut, null, true));
        Find<MidiTempoManager>().SetBpm(120);
        Find<ClockEngine>().SetClockMode(ClockMode.Internal);

        var recorder = Find<MidiRecorder>();

        Thread.Sleep(60);
        Drain();
        return "ready, record state " + recorder.State;
    }

    static void Mark(string text)
    {
        report?.AppendLine($"  {Ms(Stopwatch.GetTimestamp() - origin),8:F1} ms  {text}");
    }

    static void Drain() { while (Captured.TryDequeue(out _)) { } }

    // --- driving ---

    static readonly MethodInfo recorderUpdate = typeof(MidiRecorder).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly MethodInfo sequencerUpdate = typeof(FrameSequenceManager).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly MethodInfo clockUpdate = typeof(ClockEngine).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);

    static void PumpOnce()
    {
        recorderUpdate.Invoke(Find<MidiRecorder>(), null);
        sequencerUpdate.Invoke(Find<FrameSequenceManager>(), null);
        clockUpdate.Invoke(Find<ClockEngine>(), null);
    }

    // waits until this many ms after the scenario began, pumping Update every 5 ms
    static void At(double ms)
    {
        while (Ms(Stopwatch.GetTimestamp() - origin) < ms)
        {
            Thread.Sleep(5);
            PumpOnce();
        }
    }

    static void Note(bool isOn, int note)
    {
        var decoder = Find<TemperaControlDecoder>();
        decoder.Decode(new MidiMessageData(Stopwatch.GetTimestamp(), isOn ? MidiMessageKind.NoteOn : MidiMessageKind.NoteOff, 15, note, isOn ? 100 : 0));
    }

    static void Select(int colour) { Mark("Tempera select " + Colours[colour]); Note(true, 96 + colour); }

    // a note played on the Tempera's Overlay Keyboard (channel 1-16, not the Control channel 16)
    static void Played(bool isOn, int channel, int note, int velocity = 100)
    {
        Mark($"Tempera note {(isOn ? "on" : "off")} ch{channel} {FrameEvent.NoteName(note)}");
        var decoder = Find<TemperaControlDecoder>();
        decoder.Decode(new MidiMessageData(Stopwatch.GetTimestamp(), isOn ? MidiMessageKind.NoteOn : MidiMessageKind.NoteOff,
            (byte)(channel - 1), note, isOn ? velocity : 0));
    }
    static void Place(int cell) { Mark("Tempera place @" + cell); Note(true, EmitterUtils.IndexToControlNote(cell)); }
    static void Remove(int cell) { Mark("Tempera remove @" + cell); Note(false, EmitterUtils.IndexToControlNote(cell)); }

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

    static void SetFrames(params FrameSnapshot[] content)
    {
        Find<FrameManager>().SetFrames(content.Select(c => new Frame(c)).ToList());
    }

    static void Begin(string title, RecordMode mode, RecordTrigger trigger, RecordStartState start, int length)
    {
        var recorder = Find<MidiRecorder>();
        recorder.SetSource(RecordSource.Tempera);
        recorder.SetMode(mode);
        recorder.SetTrigger(trigger);
        recorder.SetStartState(start);
        recorder.SetLength(length);

        stateListener = s => Mark("record state " + s);
        StateEvent.AddListener(stateListener);

        report = new StringBuilder();
        report.AppendLine($"== {title} ({mode}, {trigger}, start {start}, length {(length == 0 ? "off" : length.ToString())})");

        for (int i = 0; i < 10; i++) { Thread.Sleep(5); PumpOnce(); }
        Drain();
        origin = Stopwatch.GetTimestamp();
    }

    // each call compiles a new copy of this class, so the state listener is added per scenario
    static UnityEngine.Events.UnityAction<RecordState> stateListener;

    static UnityEngine.Events.UnityEvent<RecordState> StateEvent =>
        (UnityEngine.Events.UnityEvent<RecordState>)typeof(MidiRecorder).GetField("onRecordStateChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Find<MidiRecorder>());

    static void PressRecord() { Mark("press Record"); Find<MidiRecorder>().ToggleRecord(); }

    static string Finish()
    {
        var frames = Find<FrameManager>().GetFrames();
        var sequencer = Find<FrameSequenceManager>();

        report.AppendLine("  sent:");
        long last = 0;
        var line = new List<string>();
        long lineStart = 0;
        while (Captured.TryDequeue(out var m))
        {
            if (m.Kind == MidiMessageKind.TimingClock) continue;
            string label = m.Kind == MidiMessageKind.ControlChange ? $"CC{m.Data1}={m.Data2}"
                : m.Kind == MidiMessageKind.NoteOn ? $"NoteOn ch{m.Channel + 1} {FrameEvent.NoteName(m.Data1)}"
                : m.Kind == MidiMessageKind.NoteOff ? $"NoteOff ch{m.Channel + 1} {FrameEvent.NoteName(m.Data1)}"
                : m.Kind.ToString();
            if (line.Count > 0 && Ms(m.Timestamp - last) > 2)
            {
                report.AppendLine($"    {Ms(lineStart - origin),8:F1} ms  {string.Join(" ", line)}");
                line.Clear();
            }
            if (line.Count == 0) lineStart = m.Timestamp;
            line.Add(label);
            last = m.Timestamp;
        }
        if (line.Count > 0) report.AppendLine($"    {Ms(lineStart - origin),8:F1} ms  {string.Join(" ", line)}");

        report.AppendLine($"  frames ({frames.Count}), current {Find<FrameManager>().GetCurrentFrameNumber()}, sequencer {sequencer.State}, recorder {Find<MidiRecorder>().State}:");
        for (int i = 0; i < frames.Count; i++) report.AppendLine($"    {i + 1}: {Describe(frames[i].GetSnapshot())}");

        StateEvent.RemoveListener(stateListener);

        string result = report.ToString();
        report = null;
        return result;
    }

    static void StopAll()
    {
        var sequencer = Find<FrameSequenceManager>();
        sequencer.Stop();
        sequencer.Stop();
        for (int i = 0; i < 10; i++) { Thread.Sleep(5); PumpOnce(); }
    }

    // --- scenarios ---

    // Replace from stopped, started by a touch: the Tempera is cleared when armed, frame 1 starts at the touch,
    // frames past the end are added, nothing is sent while recording, Record stops it
    public static string ReplaceTouch()
    {
        SetFrames(new FrameSnapshot(Groups((1, 18)), null), new FrameSnapshot(Groups((0, 3)), null));
        Begin("replace by touch", RecordMode.Replace, RecordTrigger.Touch, RecordStartState.Clear, 0);

        PressRecord();
        At(100); Select(3);                 // a colour select doesn't trigger
        At(150); Place(10);                 // trigger: frame 1, tick 0
        At(450); Place(20);                 // frame 1, tick ~57
        At(800); Remove(10);                // frame 2, tick ~38
        At(820); Place(20);                 // a repeat: not recorded
        At(1300); Select(1);
        At(1310); Place(33);                // frame 3 (added), tick ~44
        At(1500); PressRecord();            // stop recording; playback carries on
        At(1600);
        string result = Finish();
        StopAll();
        return result;
    }

    // Replace started by Play with a length of 2: the current frame is sent when armed, recording ends after two
    // frames and playback carries on normally from frame 3
    public static string ReplaceStartLength()
    {
        SetFrames(new FrameSnapshot(Groups((1, 18)), null), new FrameSnapshot(Groups((0, 3)), null), new FrameSnapshot(Groups((2, 40)), null));
        Begin("replace by Play, length 2", RecordMode.Replace, RecordTrigger.Start, RecordStartState.Send, 2);

        PressRecord();
        At(100); Place(10);                 // armed for Start only: not a trigger, but part of the starting state
        At(200); Mark("press Play"); Find<FrameSequenceManager>().TogglePlayPause(Find<FrameManager>().GetCurrentFrameNumber());
        At(450); Place(11);                 // frame 1, tick ~48
        At(800); Remove(18);                // frame 2, tick ~19
        At(1300); Place(12);                // after the length: not recorded, playback has moved on to frame 3
        At(1900);
        string result = Finish();
        StopAll();
        return result;
    }

    // Overdub on a loop, started straight away: frames play as usual, touches are added to the playing frame, an echo
    // of what the app sent isn't recorded
    public static string OverdubNow()
    {
        var sequencer = Find<FrameSequenceManager>();
        SetFrames(new FrameSnapshot(Groups((1, 18)), new[] { FrameEvent.Place(48, 2, 40) }), new FrameSnapshot(Groups((0, 3)), null));
        if (!IsLooped()) sequencer.ToggleLooping();
        Begin("overdub now, looping", RecordMode.Overdub, RecordTrigger.Now, RecordStartState.Clear, 0);

        PressRecord();                      // starts playback from frame 1 at once
        At(130); Select(1); Place(5);       // frame 1, tick ~24: Red@5
        At(270); Select(2); Place(40);      // tick ~51: frame 1 placed Yellow@40 at tick 48, so this echo isn't recorded
        At(300); Select(1); Place(18);      // tick ~57: Red@18 is frame 1's start: also an echo
        At(640); Remove(3);                 // frame 2, tick ~27: removes its start emitter
        At(1130); Place(6);                 // frame 1 again (loop), tick ~24
        At(1400); PressRecord();
        At(1500);
        string result = Finish();
        StopAll();
        if (IsLooped()) sequencer.ToggleLooping();
        return result;
    }

    // punch in while playing: the frame keeps what came before the touch, the rest is recorded and nothing more is sent;
    // pausing ends it
    public static string PunchIn()
    {
        var sequencer = Find<FrameSequenceManager>();
        SetFrames(
            new FrameSnapshot(Groups((1, 18)), null),
            new FrameSnapshot(Groups((0, 3)), new[] { FrameEvent.Place(10, 2, 41), FrameEvent.Place(80, 2, 42) }),
            new FrameSnapshot(Groups((3, 60)), null));
        Begin("punch in while playing", RecordMode.Replace, RecordTrigger.Touch, RecordStartState.Clear, 0);

        Mark("press Play"); sequencer.TogglePlayPause(1);
        At(600); PressRecord();             // armed while playing
        At(750); Place(7);                  // frame 2, tick ~48: punch in
        At(1100); Place(8);                 // frame 3, tick ~19
        At(1300); Mark("press Play (pause)"); sequencer.TogglePlayPause(1);
        At(1400);
        string result = Finish();
        StopAll();
        return result;
    }

    // following external clock: armed for Start, the source's MIDI Start begins the recording on its first pulse.
    // "touch": armed for a touch, with the clock running and nothing playing, the recording starts on the pulse
    // nearest the touch
    public static string Receive(string kind)
    {
        var engine = Find<ClockEngine>();
        SetFrames(new FrameSnapshot(Groups(), null));
        engine.SetClockMode(ClockMode.Receive);
        Begin("following clock, " + kind, RecordMode.Replace, kind == "touch" ? RecordTrigger.Touch : RecordTrigger.Start, RecordStartState.Clear, 0);

        long pulseTicks = (long)(Stopwatch.Frequency * (500.0 / 24) / 1000);
        long firstPulseAfterStart = 0;
        int startPulse = 30;
        var master = new Thread(() =>
        {
            long next = origin + pulseTicks;
            for (int i = 0; i < 100; i++)
            {
                if (kind == "start" && i == startPulse)
                    engine.ReceiveExternal(new MidiMessageData(Stopwatch.GetTimestamp(), MidiMessageKind.Start));
                while (Stopwatch.GetTimestamp() < next) Thread.SpinWait(10);
                long now = Stopwatch.GetTimestamp();
                if (i == startPulse) firstPulseAfterStart = now;
                engine.ReceiveExternal(new MidiMessageData(now, MidiMessageKind.TimingClock));
                next += pulseTicks;
            }
        }) { Priority = ThreadPriority.Highest };
        master.Start();

        PressRecord();
        if (kind == "start")
        {
            At(700); Place(10);             // ~50 ms after the first pulse after Start (pulse 30 at 645.8 ms): tick ~9
            At(1400); Place(11);            // frame 2, tick ~73
        }
        else
        {
            At(700); Place(10);             // pulse 33 is at 708.3 ms, pulse 32 at 687.5: nearest is 33 (8 ms away)
            At(1300); Place(11);            // 591.7 ms after pulse 33: frame 2, tick ~17
        }
        At(1700); PressRecord();
        master.Join();
        At(2200);

        if (kind == "start") report.AppendLine($"  first pulse after Start at {Ms(firstPulseAfterStart - origin):F1} ms");
        string result = Finish();
        StopAll();
        engine.SetClockMode(ClockMode.Internal);
        return result;
    }

    // notes: a note held before arming and one played while armed start at the trigger; notes played are recorded at
    // their ticks; notes still held when Record stops are ended there
    public static string Notes()
    {
        SetFrames(new FrameSnapshot(Groups(), null));
        Played(true, 1, 60);                    // held before arming
        Begin("notes, replace by touch", RecordMode.Replace, RecordTrigger.Touch, RecordStartState.Clear, 0);

        PressRecord();
        At(100); Played(true, 1, 64, 90);       // while armed, before the trigger
        At(150); Place(10);                     // trigger: frame 1 tick 0
        At(400); Played(false, 1, 64);          // frame 1, tick ~48
        At(700); Played(true, 2, 67, 80);       // frame 2, tick ~10
        At(1100); PressRecord();                // C4 and G4 still held: ended at ~tick 86 of frame 2
        At(1250);
        string result = Finish();
        StopAll();
        Played(false, 1, 60);
        Played(false, 2, 67);
        return result;
    }

    // overdub on a loop: notes are added to the frame playing, and a note held from before starts at the trigger
    public static string OverdubNotes()
    {
        var sequencer = Find<FrameSequenceManager>();
        SetFrames(new FrameSnapshot(Groups((1, 18)), null), new FrameSnapshot(Groups((0, 3)), null));
        if (!IsLooped()) sequencer.ToggleLooping();
        Played(true, 1, 48);                    // held before arming (C3)
        Begin("overdub notes, now, looping", RecordMode.Overdub, RecordTrigger.Now, RecordStartState.Clear, 0);

        PressRecord();
        At(200); Played(true, 1, 69);           // frame 1, tick ~38
        At(600); Played(false, 1, 69);          // frame 2, tick ~19
        At(800); Played(false, 1, 48);          // frame 2, tick ~57
        At(1300); PressRecord();
        At(1400);
        string result = Finish();
        StopAll();
        if (IsLooped()) sequencer.ToggleLooping();
        return result;
    }

    // Source: Transforms keeps the old Record button
    public static string Transforms()
    {
        var recorder = Find<MidiRecorder>();
        var frames = Find<FrameManager>();
        recorder.SetSource(RecordSource.Transforms);

        var isRecordingField = typeof(FrameManager).GetField("isRecording", BindingFlags.Instance | BindingFlags.NonPublic);
        recorder.ToggleRecord();
        bool on = (bool)isRecordingField.GetValue(frames);
        recorder.ToggleRecord();
        bool off = (bool)isRecordingField.GetValue(frames);

        recorder.SetSource(RecordSource.Tempera);
        return $"transform recording toggled on={on}, then off={!off}; recorder state {recorder.State}";
    }

    // panels while recording, and the texts on Record mode.s buttons
    public static string UiStates()
    {
        var sb = new StringBuilder();
        var ui = Find<TemperaMental.UI.State.UIStateManager>();
        var panels = (List<UnityEngine.CanvasGroup>)typeof(TemperaMental.UI.State.UIStateManager).GetField("panels", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
        string[] names = { "Device", "File", "Frame", "Transform", "Direction", "Record", "Playback", "Create", "Select", "Clock" };
        string Panels() => string.Join(" ", panels.Select((p, i) => names[i] + (p.interactable ? "+" : "-")));

        ui.ActionOnPlaybackStateChanged(PlaybackState.Playing);
        sb.AppendLine("playing:            " + Panels());
        ui.ActionOnTemperaRecordStateChanged(RecordState.Recording);
        sb.AppendLine("recording:          " + Panels());
        ui.ActionOnTemperaRecordStateChanged(RecordState.Idle);
        ui.ActionOnPlaybackStateChanged(PlaybackState.Reset);
        sb.AppendLine("stopped:            " + Panels());

        var settings = Find<RecordSettingsManager>();
        // Record mode's buttons (each shows its setting's value)
        var group = UnityEngine.GameObject.Find("Canvas/MainPanel/RecordGroup").transform;
        string Texts() => string.Join(" | ", new[] { "Source", "Trigger", "Mode", "Start", "Length" }.Select(n => n + " " + group.Find(n + "/Value").GetComponent<TMPro.TextMeshProUGUI>().text));
        sb.AppendLine("buttons: " + Texts());
        settings.StepTrigger(); settings.StepMode(); settings.StepStartState(); settings.StepLength(); settings.StepLength();
        sb.AppendLine("stepped: " + Texts());
        // back to the defaults (not saved)
        settings.StepTrigger(); settings.StepTrigger(); settings.StepTrigger(); settings.StepMode(); settings.StepStartState(); settings.StepStartState();
        for (int i = 0; i < 6; i++) settings.StepLength();
        sb.AppendLine("restored: " + Texts());
        return sb.ToString();
    }

    static bool IsLooped() => (bool)typeof(FrameSequenceManager).GetField("isLooped", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Find<FrameSequenceManager>());

    public static string Cleanup()
    {
        Find<ClockEngine>().SetClockMode(ClockMode.Internal);
        Find<MidiOutputScheduler>().ClearOutputDevice();
        AppDomain.CurrentDomain.SetData("tm_rec_cap", null);
        return "cleaned";
    }
}
