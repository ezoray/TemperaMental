using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "MidiConfig", menuName = "Scriptable Objects/MidiConfig")]
    public class MidiConfig : ScriptableObject
    {
        public string PrimaryDevice = "Tempera";

        // time between checking for device connection changes under Windows;
        public float PollingInterval = 0.5f;

        // midi file io
        public string FilterName = "Midi";
        public string FilterType = "mid";
        public string AppendTitle = "Append Midi File";
        public string LoadTitle = "Open Midi File";
        public string SaveTitle = "Save Midi File";

        public int DefaultBpm = 128;
        public int MinBpm = 10;
   

        // gap between sent messages (format 1 files used 120 ticks per frame; format 2 uses TicksPerBeat)
        public float EventIntervalMS = 0.0001f; // in seconds despite the name: gap between sent messages

        // internal clock
        public int TicksPerBeat = 96; // transport resolution, 4 ticks per MIDI clock pulse (24 PPQN)
        public int PulsesPerBeat = 24; // MIDI clock rate
        public float ClockSpinThresholdMs = 2f; // the clock thread busy-waits for this last part of each wait

        // clock settings views
        public bool DefaultSendStartStop = true;
        public string ClockModePrefsKey = "ClockMode";
        public string SendStartStopPrefsKey = "ClockSendStartStop";
        public string ClockFromPrefsKey = "ClockFromDevice"; // empty: same as In
        public string ClockToPrefsKey = "ClockToDevice"; // empty: same as Out
        public string ClockToNoneValue = "<none>";

        // following external clock
        public float ExternalClockTimeout = 0.5f; // seconds without a pulse before the clock counts as lost
        public int TempoMeasurePulses = 24; // pulses averaged for the measured tempo
        public float TempoDisplayHysteresis = 0.6f; // BPM change needed before the displayed tempo updates

        // midi view settings
        public int DefaultMidiChannel = 1;
        public int MidiChannelCount = 16;
        public int MaxBpm = 1000;
        public int MinimumMaxBpm = 200;

        public string MidiChannelPrefsKey = "MidiChannel";
        public string MaxBpmPrefsKey = "MaxBpm";

        // last chosen devices, reselected at start-up and when they reconnect
        public string OutputDevicePrefsKey = "MidiOutputDevice";
        public string InputDevicePrefsKey = "MidiInputDevice";

        // midi monitor: logs everything received and sent, to a file in Application.persistentDataPath
        public bool MonitorEnabled = true;
        public bool MonitorToConsole = true;
        public bool MonitorToStatusLine = true;
        public string MonitorFileName = "midi-monitor.log";
        public float MonitorClockTimeout = 0.5f; // seconds without a clock pulse before clock counts as stopped
        public int MonitorClockReportBeats = 4; // how often to report the clock's tempo while it runs

        // Tempera Control-channel notes (placement notes are 0–91: track * 12 + row)
        public int ControlSelectFirstNote = 96; // 96–99 select emitter 1–4
        public int ControlArmFirstNote = 100; // 100–107 arm tracks 1–8

        // the Tempera's Control channel listened to (Setup page), and whether the app follows the hardware (Compose page)
        public int DefaultControlChannel = 16;
        public bool DefaultFollowTempera = true;
        public string ControlChannelPrefsKey = "TemperaControlChannel";
        public string FollowTemperaPrefsKey = "FollowTempera";

        // recording from the Tempera (the Record page's row), saved as soon as they change
        public int DefaultRecordTrigger = 2; // Either
        public int DefaultRecordMode = 0; // Replace
        public int DefaultRecordStartState = 0; // Clear
        public int DefaultRecordLengthIndex = 0; // Off
        public int[] RecordLengths = { 0, 1, 2, 4, 8, 16, 32, 64 }; // frames; 0 is no limit
        public string RecordTriggerPrefsKey = "RecordTrigger";
        public string RecordModePrefsKey = "RecordMode";
        public string RecordStartStatePrefsKey = "RecordStartState";
        public string RecordLengthPrefsKey = "RecordLength";

        // canvas recall: a stepped canvas is sent once it stops changing for this long (seconds)
        public float CanvasSendDelay = 0.75f;

        // Hold: the note held so emitters sound without playing the Tempera (middle C), and its velocity
        public int HoldNote = 60;
        public int HoldVelocity = 100;

        // cc numbers for emitters
        public int ActivateCC = 10;
        public int PlaceCC = 11;
        public int RemoveCC = 12;

        public byte ClearEmittersValue = 64;

        // markers in midifile
        public string FrameStartPrefix = "START_FRAME_NO_";
        public string SeqEndMarker = "END_SEQ";

        // file format 2 (TicksPerBeat per frame, with timed events): a marker at the start of the file, and one in
        // each frame between its start state and its events
        public string FileFormatMarker = "TEMPERAMENTAL_FORMAT_2";
        public string FrameEventsMarker = "FRAME_EVENTS";
    }
}
