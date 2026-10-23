using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using TemperaMental.Applications.Config;
using TemperaMental.Logs;
using TemperaMental.Midi.Clock;
using TemperaMental.Midi.Core;
using TemperaMental.Midi.Output;
using TemperaMental.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TemperaMental.Midi.Input
{
    // Logs every MIDI message received (IN) and sent (OUT) with timings, mainly for testing how devices such as the
    // Tempera behave. Written to Application.persistentDataPath/midi-monitor.log (the previous run is kept as
    // midi-monitor.prev.log), to the Unity console, and incoming messages briefly to the status line.
    // Clock pulses are summarised (tempo and timing spread) instead of being logged one by one.
    public class MidiMonitor : MonoBehaviour
    {
        [SerializeField] MidiInputRouter inputRouter;
        [SerializeField] MidiInputRouter clockInputRouter;
        [SerializeField] MidiOutputScheduler outputScheduler;
        [SerializeField] ClockEngine clockEngine;

        // IN: the In device, CLK: a separate Clock from device, OUT: everything the app sends
        enum Source { In, ClockIn, Out }

        readonly struct Entry
        {
            public readonly Source Source;
            public readonly MidiMessageData Message;

            public Entry(Source source, MidiMessageData message)
            {
                Source = source;
                Message = message;
            }

            public bool IsInput => Source != Source.Out;

            public string Direction => Source == Source.In ? "IN " : Source == Source.ClockIn ? "CLK" : "OUT";
        }

        class ClockStats
        {
            public bool IsRunning;
            public long LastPulse;
            public long PulseCount;

            public int WindowCount;
            public double WindowSum;
            public double WindowSumSquares;
            public double WindowMin;
            public double WindowMax;

            public void ResetWindow()
            {
                WindowCount = 0;
                WindowSum = 0;
                WindowSumSquares = 0;
                WindowMin = double.MaxValue;
                WindowMax = double.MinValue;
            }
        }

        static readonly string[] EmitterNames = { "Blue", "Red", "Yellow", "Green" };

        readonly ConcurrentQueue<Entry> entries = new ConcurrentQueue<Entry>();
        readonly List<Entry> batch = new List<Entry>();
        readonly StringBuilder lines = new StringBuilder();
        readonly ClockStats inClock = new ClockStats();
        readonly ClockStats clockInClock = new ClockStats();
        readonly ClockStats outClock = new ClockStats();

        StreamWriter writer;
        string logPath;

        double timestampToMs;
        long sessionStart;
        long lastLineTime;

        int pulsesPerBeat;
        int reportPulses;
        long clockTimeoutTicks;
        int selectFirstNote;
        int armFirstNote;
        int activateCC;
        int placeCC;
        int removeCC;
        int clearEmittersValue;
        int gridHeight;

        bool toConsole;
        bool toStatusLine;
        bool isActiveSensingReported;

        string lastStatusMessage;

        private void Awake()
        {
            timestampToMs = 1000.0 / Stopwatch.Frequency;

            pulsesPerBeat = ConfigRegistry.Midi.PulsesPerBeat;
            reportPulses = ConfigRegistry.Midi.MonitorClockReportBeats * pulsesPerBeat;
            clockTimeoutTicks = (long)(ConfigRegistry.Midi.MonitorClockTimeout * Stopwatch.Frequency);
            selectFirstNote = ConfigRegistry.Midi.ControlSelectFirstNote;
            armFirstNote = ConfigRegistry.Midi.ControlArmFirstNote;
            activateCC = ConfigRegistry.Midi.ActivateCC;
            placeCC = ConfigRegistry.Midi.PlaceCC;
            removeCC = ConfigRegistry.Midi.RemoveCC;
            clearEmittersValue = ConfigRegistry.Midi.ClearEmittersValue;
            gridHeight = ConfigRegistry.Grid.GridHeight;

            toConsole = ConfigRegistry.Midi.MonitorToConsole;
            toStatusLine = ConfigRegistry.Midi.MonitorToStatusLine;

            if (!ConfigRegistry.Midi.MonitorEnabled)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!ConfigRegistry.Midi.MonitorEnabled) return;

            OpenLog();

            inputRouter.MessageReceived += OnMessageReceived;
            clockInputRouter.MessageReceived += OnClockInputReceived;
            outputScheduler.MessageSent += OnMessageSent;
        }

        private void OpenLog()
        {
            try
            {
                logPath = Path.Combine(Application.persistentDataPath, ConfigRegistry.Midi.MonitorFileName);
                string previousPath = Path.ChangeExtension(logPath, ".prev.log");

                if (File.Exists(logPath))
                {
                    File.Copy(logPath, previousPath, true);
                }

                writer = new StreamWriter(logPath, false, Encoding.UTF8) { AutoFlush = true };
                writer.WriteLine($"TemperaMental MIDI monitor, started {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine("    time s       +ms  dir   message");
            }
            catch (Exception ex)
            {
                writer = null;
                LogMan.LogWarning($"MIDI monitor can't write its log file: {ex.Message}");
            }

            sessionStart = Stopwatch.GetTimestamp();
            lastLineTime = sessionStart;
        }

        // driver thread
        private void OnMessageReceived(MidiMessageData message)
        {
            entries.Enqueue(new Entry(Source.In, message));
        }

        // driver thread
        private void OnClockInputReceived(MidiMessageData message)
        {
            entries.Enqueue(new Entry(Source.ClockIn, message));
        }

        // clock thread
        private void OnMessageSent(MidiMessageData message)
        {
            entries.Enqueue(new Entry(Source.Out, message));
        }

        private void Update()
        {
            batch.Clear();

            while (entries.TryDequeue(out Entry entry))
            {
                batch.Add(entry);
            }

            // input and output arrive from different threads, so put them back in time order
            batch.Sort((a, b) => a.Message.Timestamp.CompareTo(b.Message.Timestamp));

            foreach (Entry entry in batch)
            {
                Process(entry);
            }

            long now = Stopwatch.GetTimestamp();
            CheckClockTimeout(inClock, "IN ", now);
            CheckClockTimeout(clockInClock, "CLK", now);
            CheckClockTimeout(outClock, "OUT", now);

            Flush();
        }

        private void Process(Entry entry)
        {
            MidiMessageData message = entry.Message;
            string direction = entry.Direction;

            switch (message.Kind)
            {
                case MidiMessageKind.TimingClock:
                    ClockStats stats = entry.Source == Source.In ? inClock : entry.Source == Source.ClockIn ? clockInClock : outClock;
                    OnPulse(stats, direction, message.Timestamp, entry.IsInput);
                    return;

                case MidiMessageKind.ActiveSensing:
                    if (!isActiveSensingReported)
                    {
                        isActiveSensingReported = true;
                        AddLine(message.Timestamp, direction, "Active sensing (further ones not logged)");
                    }
                    return;
            }

            string description = Describe(message);
            AddLine(message.Timestamp, direction, description);

            if (entry.IsInput)
            {
                lastStatusMessage = direction + " " + description;
            }
        }

        private void OnPulse(ClockStats stats, string direction, long timestamp, bool isInput)
        {
            if (!stats.IsRunning)
            {
                stats.IsRunning = true;
                stats.PulseCount = 0;
                stats.ResetWindow();

                AddLine(timestamp, direction, "Clock started");

                if (isInput) lastStatusMessage = direction + " clock started";
            }
            else
            {
                double interval = (timestamp - stats.LastPulse) * timestampToMs;

                stats.WindowCount++;
                stats.WindowSum += interval;
                stats.WindowSumSquares += interval * interval;
                stats.WindowMin = Math.Min(stats.WindowMin, interval);
                stats.WindowMax = Math.Max(stats.WindowMax, interval);
            }

            stats.LastPulse = timestamp;
            stats.PulseCount++;

            if (stats.WindowCount >= reportPulses)
            {
                ReportClock(stats, direction, timestamp);
            }
        }

        private void ReportClock(ClockStats stats, string direction, long timestamp)
        {
            if (stats.WindowCount == 0) return;

            double mean = stats.WindowSum / stats.WindowCount;
            double variance = Math.Max(0, stats.WindowSumSquares / stats.WindowCount - mean * mean);
            double bpm = 60000.0 / (mean * pulsesPerBeat);

            string report = string.Format(
                "Clock {0:F2} BPM, pulse {1:F3} ms ±{2:F3} (min {3:F3}, max {4:F3}) over {5} pulses",
                bpm, mean, Math.Sqrt(variance), stats.WindowMin, stats.WindowMax, stats.WindowCount);

            // for the app's own clock, also say how punctual the clock thread was
            if (stats == outClock && clockEngine != null)
            {
                ClockTimingStats timing = clockEngine.TakeTimingStats();
                report += $"; clock thread {timing.LateSteps} of {timing.Steps} steps over 1 ms late (worst {timing.MaxLatenessMs:F2} ms)";
            }

            AddLine(timestamp, direction, report);

            stats.ResetWindow();
        }

        private void CheckClockTimeout(ClockStats stats, string direction, long now)
        {
            if (!stats.IsRunning || now - stats.LastPulse < clockTimeoutTicks) return;

            ReportClock(stats, direction, stats.LastPulse);

            stats.IsRunning = false;
            AddLine(stats.LastPulse, direction,
                $"Clock stopped after {stats.PulseCount} pulses ({stats.PulseCount / (double)pulsesPerBeat:F1} beats)");

            if (direction != "OUT") lastStatusMessage = direction + " clock stopped";
        }

        private string Describe(MidiMessageData message)
        {
            string channel = message.IsChannelMessage ? $"ch{message.Channel + 1,-2} " : "";

            switch (message.Kind)
            {
                case MidiMessageKind.NoteOn:
                    return $"{channel}Note On  {message.Data1,3} v{message.Data2,-3}  {DescribeControlNote(message.Data1, true)}";

                case MidiMessageKind.NoteOff:
                    return $"{channel}Note Off {message.Data1,3}        {DescribeControlNote(message.Data1, false)}";

                case MidiMessageKind.ControlChange:
                    return $"{channel}CC {message.Data1,3} = {message.Data2,-3}   {DescribeEmitterCC(message.Data1, message.Data2)}";

                case MidiMessageKind.ProgramChange:
                    return $"{channel}Program change {message.Data1}";

                case MidiMessageKind.PitchBend:
                    return $"{channel}Pitch bend {message.Data1}";

                case MidiMessageKind.ChannelPressure:
                    return $"{channel}Channel pressure {message.Data1}";

                case MidiMessageKind.PolyPressure:
                    return $"{channel}Poly pressure note {message.Data1} = {message.Data2}";

                case MidiMessageKind.SongPosition:
                    return $"Song position {message.Data1} (beat {message.Data1 / 4.0:F2})";

                case MidiMessageKind.SysEx:
                    return $"SysEx ({message.Data1} bytes)";

                default:
                    return message.Kind.ToString();
            }
        }

        // meaning of a note if it's on the Tempera's Control channel
        private string DescribeControlNote(int note, bool isNoteOn)
        {
            if (note >= selectFirstNote && note < selectFirstNote + EmitterNames.Length)
            {
                return isNoteOn ? $"Tempera: select {EmitterNames[note - selectFirstNote]}" : "";
            }

            if (note >= armFirstNote && note < armFirstNote + 8)
            {
                return isNoteOn ? $"Tempera: arm track {note - armFirstNote + 1}" : "";
            }

            if (EmitterUtils.TryControlNoteToIndex(note, out int index))
            {
                return $"Tempera: {(isNoteOn ? "place" : "remove")} {DescribeCell(index)}";
            }

            return "";
        }

        // meaning of the emitter CCs the app sends
        private string DescribeEmitterCC(int controller, int value)
        {
            if (controller == activateCC)
            {
                return value < EmitterNames.Length ? $"select {EmitterNames[value]}" : "";
            }

            if (controller == placeCC)
            {
                return $"place {DescribeCell(value)}";
            }

            if (controller == removeCC)
            {
                return value == clearEmittersValue ? "clear all" : $"remove {DescribeCell(value)}";
            }

            return "";
        }

        private string DescribeCell(int index)
        {
            return $"track {index / gridHeight + 1} row {index % gridHeight + 1} (cell {index})";
        }

        private void AddLine(long timestamp, string direction, string text)
        {
            double time = (timestamp - sessionStart) * timestampToMs / 1000.0;
            double delta = (timestamp - lastLineTime) * timestampToMs;

            lastLineTime = timestamp;

            lines.AppendFormat("{0,10:F4} {1,9:F3}  {2}  {3}\n", time, delta, direction, text);
        }

        private void Flush()
        {
            if (lines.Length > 0)
            {
                string text = lines.ToString();
                lines.Clear();

                try
                {
                    writer?.Write(text);
                }
                catch (Exception ex)
                {
                    writer = null;
                    LogMan.LogWarning($"MIDI monitor stopped writing its log file: {ex.Message}");
                }

                if (toConsole)
                {
                    Debug.Log("MIDI monitor\n" + text);
                }
            }

            if (toStatusLine && lastStatusMessage != null)
            {
                LogMan.LogTemp(lastStatusMessage);
            }

            lastStatusMessage = null;
        }

        private void OnDisable()
        {
            if (inputRouter != null) inputRouter.MessageReceived -= OnMessageReceived;
            if (clockInputRouter != null) clockInputRouter.MessageReceived -= OnClockInputReceived;
            if (outputScheduler != null) outputScheduler.MessageSent -= OnMessageSent;

            Flush();

            writer?.Dispose();
            writer = null;
        }

        public string LogPath => logPath;
    }
}
