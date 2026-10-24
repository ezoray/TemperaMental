using System.Linq;
using System.Text;
using TemperaMental.Core;
using TemperaMental.Recording;
using TemperaMental.UI.Pages;
using UnityEngine;
using Object = UnityEngine.Object;

// switches pages, presses UI buttons by path and reports the page groups, their texts and the status line
public static class UiHarness
{
    static Transform MainPanel => GameObject.Find("Canvas/MainPanel").transform;

    // the settings the UI buttons save, so a test can put them back
    static readonly string[] IntPrefs =
    {
        "MidiChannel", "MaxBpm", "ClockMode", "ClockSendStartStop", "TemperaControlChannel", "FollowTempera",
        "RecordSource", "RecordTrigger", "RecordMode", "RecordStartState", "RecordLength", "AppPage", "AppMode", "MainViewSurface"
    };
    static readonly string[] StringPrefs = { "ClockFromDevice", "ClockToDevice" };

    // Compose, Play, Transform, Record or Setup, pressed on its tab
    public static string Page(string name)
    {
        var tab = MainPanel.Find("TitlePanel/PageTabs/Tab" + name);
        if (tab == null) return "no tab " + name;
        tab.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        return "page " + name + "\n" + State();
    }

    // a button by its path under MainPanel, e.g. DisplayPanel/ButtonPanel/ClockGroup/ClockMode or SetupSheet/ChannelPlus
    public static string Press(string path)
    {
        var t = MainPanel.Find(path);
        if (t == null) return "no " + path;
        t.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        return "pressed " + path + "\n" + State();
    }

    // presses and releases a key (an InputSystem Key name, e.g. N or Space) through the input system, so it reaches
    // the buttons' key handlers in whatever mode is shown; reports the loop and playback state
    public static string Key(string name)
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return "no keyboard";
        var key = (UnityEngine.InputSystem.Key)System.Enum.Parse(typeof(UnityEngine.InputSystem.Key), name);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
        UnityEngine.InputSystem.InputSystem.Update();
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        UnityEngine.InputSystem.InputSystem.Update();

        var seq = Object.FindAnyObjectByType<TemperaMental.Midi.Playbacks.FrameSequenceManager>();
        var looped = typeof(TemperaMental.Midi.Playbacks.FrameSequenceManager)
            .GetField("isLooped", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(seq);
        return $"key {name}: page {Object.FindAnyObjectByType<PageUIManager>().Page}, looped {looped}, playback {seq.State}";
    }

    // places a red emitter at (2, 2) and a blue one at (0, 7), as if drawn on the grid
    public static string Draw()
    {
        var frames = Object.FindAnyObjectByType<TemperaMental.Frames.FrameManager>();
        frames.SetEmitterType(1);
        frames.AddEmitter(new Vector2Int(2, 2));
        frames.SetEmitterType(0);
        frames.AddEmitter(new Vector2Int(0, 7));
        return "drew: " + Frame();
    }

    // the current frame's emitters, one 64-bit mask per colour (Blue, Red, Yellow, Green)
    public static string Frame()
    {
        var frame = Object.FindAnyObjectByType<TemperaMental.Frames.FrameManager>().PublishedCurrentFrame;
        return string.Join(" ", frame.StartGroups.Select(g => g.ToString("X16")));
    }

    public static string Record()
    {
        Object.FindAnyObjectByType<MidiRecorder>().ToggleRecordTempera();
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
        var b = MainPanel.Find("RecordPanel/Record").GetComponent<UnityEngine.UI.Button>();
        var results = MainPanel.Find("DisplayPanel/ButtonPanel/TransformRowGroup/RecResults").GetComponent<UnityEngine.UI.Button>();
        return "record button colour " + b.image.color + ", rec results colour " + results.image.color;
    }

    // the saved settings, as "Key=value;..." to hand back to RestorePrefs
    public static string Prefs()
    {
        var parts = IntPrefs.Where(PlayerPrefs.HasKey).Select(k => k + "=" + PlayerPrefs.GetInt(k))
            .Concat(StringPrefs.Where(PlayerPrefs.HasKey).Select(k => k + "=s:" + PlayerPrefs.GetString(k)));
        return string.Join(";", parts);
    }

    // puts back the settings from Prefs; keys missing from it are deleted
    public static string RestorePrefs(string saved)
    {
        var values = saved.Split(';').Where(p => p.Contains("=")).ToDictionary(p => p.Split('=')[0], p => p.Substring(p.IndexOf('=') + 1));
        foreach (var key in IntPrefs.Concat(StringPrefs))
        {
            if (!values.TryGetValue(key, out var value)) PlayerPrefs.DeleteKey(key);
            else if (value.StartsWith("s:")) PlayerPrefs.SetString(key, value.Substring(2));
            else PlayerPrefs.SetInt(key, int.Parse(value));
        }
        PlayerPrefs.Save();
        return "restored: " + Prefs();
    }

    public static string State()
    {
        var sb = new StringBuilder();
        var pages = Object.FindAnyObjectByType<PageUIManager>();
        sb.AppendLine("page " + pages.Page);

        foreach (var group in MainPanel.GetComponentsInChildren<PageGroup>(true))
        {
            var cg = group.GetComponent<CanvasGroup>();
            if (cg.alpha < 0.5f) continue;

            var texts = group.GetComponentsInChildren<UnityEngine.UI.Button>(true).Select(b =>
                b.name + ":" + string.Join(" ", b.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Select(t => t.text.Trim())));
            var boxes = group.GetComponentsInChildren<UnityEngine.UI.Image>(true).Where(i => i.GetComponent<UnityEngine.UI.Button>() == null)
                .SelectMany(i => i.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Where(t => t.transform.parent == i.transform))
                .Select(t => t.transform.parent.name + "=" + t.text);
            sb.AppendLine($"{group.name} [{(cg.interactable ? "on" : "locked")}, raycasts {cg.blocksRaycasts}] " +
                string.Join(" | ", texts.Concat(boxes)));
        }

        var display = MainPanel.Find("DisplayPanel/MainView/DisplayCanvas");
        string Text(string n) => display.Find(n).GetComponent<TMPro.TextMeshProUGUI>().text;
        sb.AppendLine("status: [" + Text("ClockStatus") + "] [" + Text("SoundStatus") + "] [" + Text("RecordStatus") + "]  message: " + Text("Message"));
        return sb.ToString();
    }
}
