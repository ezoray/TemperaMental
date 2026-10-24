# Play-mode test harnesses

C# harnesses that test the app inside the open Unity Editor, in play mode, through the Unity CLI's `script run`.
They were used to test the MIDI clock, input and recording redesign (see `Docs/MidiClockRedesign.md`). They sit outside
`Assets/`, so Unity never compiles them into the app; `script run` compiles each one in memory when it's called, with
no domain reload.

Each harness swaps the MIDI output for a fake device that records everything the app sends, with timestamps from the
clock thread. External clock and Tempera notes are simulated. No MIDI ports are opened.

## Running

Needs the Unity CLI (`unity`), the project open in the Editor with the `com.unity.pipeline` package, and play mode.

```bash
unity command editor.playmode start --project-path="$PWD"
./Tests/Harnesses/run.sh EventsTestHarness Setup
./Tests/Harnesses/run.sh EventsTestHarness Play internal
./Tests/Harnesses/run.sh EventsTestHarness Cleanup
unity command editor.playmode stop --project-path="$PWD"
```

`run.sh <Harness> <Entry> [argument]` prints what the entry returns. Run a harness's `Setup` first (it installs the
fake output) and `Cleanup` at the end. `screenshot.sh <out.png>` saves the Game view without importing it into
`Assets/`.

## Rules

- **Never create a DryWetMIDI `VirtualDevice` or open MIDI inputs from harness code.** A virtual device can outlive a
  domain reload and crash the Editor from the MIDI thread.
- Keep the Tempera and other MIDI devices disconnected unless testing hardware on purpose: the app opens the
  "Tempera" ports and the last-used ones when play mode starts.
- **Don't refresh assets in play mode with script changes pending.** `AssetDatabase.Refresh`/`DeleteAsset`, or a
  `game_view --save_path` screenshot, makes Unity recompile mid-play, which the app doesn't survive. Leave play mode
  first.
- **Don't leave a settings page open when play mode ends**: it saves its settings to PlayerPrefs as it closes.
- **Take screenshots in play mode only.** In edit mode the Game view render leaves a changed Main Camera rect in
  memory, and the next scene save writes it.
- Pressing surface or settings buttons saves settings. Afterwards, delete keys the session created (e.g.
  `MainViewSurface`, `ClockMode`, `ClockSendStartStop`, `ClockFromDevice`, `ClockToDevice`, `RecordSource`,
  `RecordTrigger`, `RecordMode`, `RecordStartState`, `RecordLength`, `MidiOutputDevice`, `MidiInputDevice`).

## How they work

- **Each call compiles a fresh copy of the class**, so static fields don't survive between calls. State shared between
  calls lives in `AppDomain.CurrentDomain` data (e.g. the captured messages queue).
- **A call holds Unity's main thread**, so `Update` doesn't run while it waits. Results that need Unity frames
  come in pairs (`PauseStart` then `PauseCheck`). `RecordTestHarness` instead calls the recorder's, sequencer's and
  clock's `Update` itself every 5 ms.
- Compiling a call causes garbage collections, which pause the clock thread briefly. Don't read pulse jitter around a
  call as the app's own; measure with `JitterHarness Begin`/`End` while Unity runs freely.

## Harnesses

| Harness | Tests | Entry points |
|---|---|---|
| `ClockTestHarness` | Internal and Send clock: pulse spacing, frames on the beat, transport, latched transforms | `Setup` (Send mode, 8 frames, 120 BPM), `ToggleLoop`, `Play`, `Measure`, `StopAll`, `Transport`, `StartOrder`, `LatchShiftRight`, `Unlatch`, `ModeInternal`, `Drain`, `Cleanup` |
| `JitterHarness` | Pulse spacing (after `ClockTestHarness Setup`) | `Begin`, `End` (main thread free between), `Run <seconds>` (main thread held), `SequencerTicks 0/1` |
| `ReceiveTestHarness` | Following external clock (Receive mode) | `Setup`, `Master "<script>"` with tokens `bpm:N pulses:N start stop continue spp:N gap:MS play appstop`, `LatchShiftRight`, `ResetAll`, `Status`, `Cleanup` |
| `TemperaTestHarness` | Tempera Control-channel input, Follow Tempera, loop protection | `Setup`, `Run "<script>"` with tokens `sel:N on:NOTE off:NOTE ch:N follow:0/1 pick:N newframe twolane:N play stop loop goto:N state`, `Cleanup` |
| `EventsTestHarness` | Frame events: timing inside the beat, transforms, edits, file format 2 | `Setup`, `Play internal\|send`, `Reverse`, `Receive warm\|cold`, `Transforms`, `Edits`, `Files`, `PauseStart`/`PauseCheck`, `DisplayStart`/`DisplayCheck`, `LatchStart`/`LatchCheck`, `Cleanup` |
| `RecordTestHarness` | Recording from the Tempera: triggers, Replace, Overdub, punch-in, following clock | `Setup`, `ReplaceTouch`, `ReplaceStartLength`, `OverdubNow`, `PunchIn`, `Receive start\|touch`, `Notes`, `OverdubNotes`, `Transforms`, `UiStates`, `Cleanup` |
| `VoiceTestHarness` | Notes, canvas recall and Hold | `Setup`, `PlayNotes`, `StartInside`, `PauseResume`, `Loop`, `CanvasStep` then `CanvasCheck` (Unity must run between them), `CanvasFile`, `Hold`, `QuitRelease`, `Transforms`, `Cleanup` |
| `UiHarness` | Modes, their buttons, status line, panel locking, shortcuts | `State`, `Mode <Play\|Transform\|Edit\|Record\|Sound\|Settings>`, `Press <path under MainPanel>` (e.g. `ClockGroup/ClockMode`, `MidiGroup/ChannelPlus`), `Key <InputSystem key, e.g. N>`, `Record`, `Play`, `StopPlayback`, `Pulses <seconds>`, `RecordColour`, `Prefs` / `RestorePrefs <Prefs output>` (buttons save settings) |

In `TemperaTestHarness`, `play` reaches the frame manager through `Update`, so put `play` and the next step in
separate runs.
