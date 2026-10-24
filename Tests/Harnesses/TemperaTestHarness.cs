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
using TemperaMental.Midi.Core;
using TemperaMental.Midi.Input;
using TemperaMental.Midi.Output;
using TemperaMental.Midi.Playbacks;
using TemperaMental.Settings;
using Object = UnityEngine.Object;

// Play-mode tests for Tempera input: simulated Control-channel notes go straight into the decoder (main thread),
// a fake output device records what the app sends. No CoreMIDI ports are involved.
public static class TemperaTestHarness
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

    static ConcurrentQueue<MidiMessageData> Captured => (ConcurrentQueue<MidiMessageData>)AppDomain.CurrentDomain.GetData("tm_tp_cap");

    public static string Setup()
    {
        var captured = new ConcurrentQueue<MidiMessageData>();
        AppDomain.CurrentDomain.SetData("tm_tp_cap", captured);
        Object.FindAnyObjectByType<MidiOutputScheduler>().SetOutputDevice(new CapturingDevice(captured));
        Thread.Sleep(50);
        while (captured.TryDequeue(out _)) { }
        return "ready; " + State();
    }

    // runs steps and reports, after each, the frame, the draw colour and what was sent.
    // steps: sel:N (select emitter 0-3)  on:NOTE  off:NOTE  ch:N (channel 1-16 for following notes, default 16)
    //        follow:0/1  pick:N (app colour button)  newframe  twolane:N  play  stop
    public static string Run(string script)
    {
        var decoder = Object.FindAnyObjectByType<TemperaControlDecoder>();
        var frames = Object.FindAnyObjectByType<FrameManager>();
        var settings = Object.FindAnyObjectByType<TemperaSettingsManager>();
        var sequencer = Object.FindAnyObjectByType<FrameSequenceManager>();
        int channel = 16;

        var sb = new StringBuilder();

        foreach (string token in script.Split(' '))
        {
            string[] parts = token.Split(':');
            string op = parts[0];
            int value = parts.Length > 1 ? int.Parse(parts[1]) : 0;
            long now = Stopwatch.GetTimestamp();

            switch (op)
            {
                case "sel": decoder.Decode(new MidiMessageData(now, MidiMessageKind.NoteOn, (byte)(channel - 1), 96 + value, 100)); break;
                case "on": decoder.Decode(new MidiMessageData(now, MidiMessageKind.NoteOn, (byte)(channel - 1), value, 100)); break;
                case "off": decoder.Decode(new MidiMessageData(now, MidiMessageKind.NoteOff, (byte)(channel - 1), value, 0)); break;
                case "ch": channel = value; break;
                case "follow": settings.SetFollowTempera(value != 0); break;
                case "pick": frames.SetEmitterType(value); break;
                case "newframe": frames.InsertFrame(); break;
                case "twolane": Object.FindAnyObjectByType<EmitterSettingsManager>().ToggleTwoLane(value); break;
                case "play": sequencer.TogglePlayPause(frames.GetCurrentFrameNumber()); break;
                case "loop": sequencer.ToggleLooping(); break;
                case "goto": frames.GoToSelectedFrame(value); break;
                case "state": sb.AppendLine("sequencer " + sequencer.State); break;
                case "stop": sequencer.Stop(); sequencer.Stop(); break;
            }

            // let the clock thread send anything queued
            Thread.Sleep(30);

            sb.AppendLine($"{token,-10} -> {State()} | sent: {Sent()}");
        }

        return sb.ToString();
    }

    static string State()
    {
        var frames = Object.FindAnyObjectByType<FrameManager>();
        ulong[] groups = frames.GetCurrentFrameEmitters();

        var cells = new List<string>();
        for (int c = 0; c < groups.Length; c++)
            for (int i = 0; i < 64; i++)
                if ((groups[c] & (1UL << i)) != 0) cells.Add($"{Colours[c]}@{i}");

        var colourField = typeof(FrameManager).GetField("currentEmitterId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        int colour = (int)colourField.GetValue(frames);

        return $"frame {frames.GetCurrentFrameNumber()} [{string.Join(" ", cells)}] draw={Colours[colour]}";
    }

    static string Sent()
    {
        var items = new List<string>();
        while (Captured.TryDequeue(out var e))
            items.Add(e.Kind == MidiMessageKind.ControlChange ? $"CC{e.Data1}={e.Data2}" : e.Kind.ToString());
        return items.Count == 0 ? "nothing" : string.Join(" ", items);
    }

    public static string Cleanup()
    {
        Object.FindAnyObjectByType<MidiOutputScheduler>().ClearOutputDevice();
        AppDomain.CurrentDomain.SetData("tm_tp_cap", null);
        return "cleaned";
    }
}
