using System.Linq;
using System.Text;
using TemperaMental.Core;
using TemperaMental.Recording;
using UnityEngine;
using Object = UnityEngine.Object;

// presses UI buttons by path and reports the main view's surfaces, texts and panel states
public static class UiHarness
{
    static Transform MainView => GameObject.Find("Canvas/MainPanel/DisplayPanel/MainView").transform;

    public static string Press(string path)
    {
        var t = MainView.Find(path);
        if (t == null) return "no " + path;
        t.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        return "pressed " + path + "\n" + State();
    }

    public static string Record()
    {
        Object.FindAnyObjectByType<MidiRecorder>().ToggleRecord();
        return "record pressed\n" + State();
    }

    // a simulated clock source for a few seconds, on its own thread (ends by itself)
    public static string Pulses(string seconds)
    {
        var engine = Object.FindAnyObjectByType<TemperaMental.Midi.Clock.ClockEngine>();
        int count = (int)(double.Parse(seconds) * 48);
        long pulse = (long)(System.Diagnostics.Stopwatch.Frequency * (500.0 / 24) / 1000);
        new System.Threading.Thread(() =>
        {
            long next = System.Diagnostics.Stopwatch.GetTimestamp() + pulse;
            for (int i = 0; i < count; i++)
            {
                while (System.Diagnostics.Stopwatch.GetTimestamp() < next) System.Threading.Thread.Sleep(1);
                engine.ReceiveExternal(new TemperaMental.Midi.Core.MidiMessageData(System.Diagnostics.Stopwatch.GetTimestamp(), TemperaMental.Midi.Core.MidiMessageKind.TimingClock));
                next += pulse;
            }
        }) { IsBackground = true }.Start();
        return "sending " + count + " pulses";
    }

    public static string Play()
    {
        var seq = Object.FindAnyObjectByType<TemperaMental.Midi.Playbacks.FrameSequenceManager>();
        seq.TogglePlayPause(Object.FindAnyObjectByType<TemperaMental.Frames.FrameManager>().GetCurrentFrameNumber());
        return "play pressed, " + seq.State;
    }

    public static string StopPlayback()
    {
        var seq = Object.FindAnyObjectByType<TemperaMental.Midi.Playbacks.FrameSequenceManager>();
        seq.Stop(); seq.Stop();
        return "stopped, " + seq.State;
    }

    public static string RecordColour()
    {
        var b = GameObject.Find("Canvas/MainPanel/RecordPanel/Record").GetComponent<UnityEngine.UI.Button>();
        return "record button colour " + b.image.color;
    }

    public static string State()
    {
        var sb = new StringBuilder();
        var mv = MainView;
        string Text(string p) { var t = mv.Find(p); return t == null ? "?" : t.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text.Replace("\n", " / "); }
        foreach (var panel in new[] { "ButtonPanel", "ClockButtonPanel", "RecordButtonPanel" })
        {
            var p = mv.Find(panel);
            var cg = p.GetComponent<CanvasGroup>();
            sb.AppendLine($"{panel}: active {p.gameObject.activeSelf}, interactable {cg.interactable}: " +
                string.Join(" | ", p.Cast<Transform>().Select(b => b.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text.Replace("\n", " / "))));
        }
        sb.AppendLine("status: [" + Text("DisplayCanvas/ClockStatus") + "] [" + Text("DisplayCanvas/RecordStatus") + "]  message: " + Text("DisplayCanvas/Message"));
        return sb.ToString();
    }
}
