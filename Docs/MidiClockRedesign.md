# MIDI clock, input and recording redesign — fact finding

Written 2026-10-23, revised the same day for device roles and record arming. Covers what the Tempera supports
over MIDI, what the app does today, and what has to change for it to (1) run on MIDI clock (internal, send, or
receive from any device such as a hardware sequencer or DAW), (2) use separate devices for the Tempera and for clock,
(3) receive the Tempera's emitter messages, and (4) record them into frames with timing inside the beat, starting
from an armed state on MIDI Start or on the first touch on the Tempera.

Sources: Tempera Owner's Manual for firmware 2.3 — [MIDI input](https://docs.beetlecrab.audio/tempera/midi.html),
[Settings](https://docs.beetlecrab.audio/tempera/settings.html),
[Emitters](https://docs.beetlecrab.audio/tempera/emitters.html),
[Specifications](https://docs.beetlecrab.audio/tempera/specifications.html),
[PDF](https://docs.beetlecrab.audio/tempera/_static/tempera_manual.pdf). DryWetMIDI 8.0.3, as bundled in `Assets/Melanchall`.

---

## 1. What the Tempera does over MIDI

### Implementation table (firmware 2.3)

| Function | Tempera sends | Tempera receives |
|---|---|---|
| Channel | 1–16 | 1–16 (or All) |
| Notes | 1–127 (Overlay Keyboard, if *Send MIDI notes* is on) | 1–127 |
| Pitch bend, channel AT, poly AT | – | yes |
| **Control Change** | **– (never sent)** | 0–119 |
| Program change | – | 0–127 (loads canvas by alphabetical position in current folder) |
| **Clock / Start / Stop** | **yes\*** | **yes\*** |

\* The manual lists clock, Start and Stop as **one row**. The settings page only says the tempo can be stopped
and resumed with Play, not that doing so sends or obeys MIDI Start/Stop. **Treat Start/Stop from the Tempera as
unverified and don't design around it** (hardware tests 5 and 9).

Ports: USB device, USB host and TRS (Type A/B switchable), all able to send and receive. TRS MIDI thru is optional.

### Emitter control — two separate protocols

**A. CC on the Tempera's main MIDI channel** (what the app uses today; receive only):

| CC | Value |
|---|---|
| 10 Active emitter | 0–3 (0 = blue) |
| 11 Place | cell 0–63 |
| 12 Remove | cell 0–63; **64 clears *all* placed emitters** (the app currently sends it once per colour, which is redundant) |

Cells are numbered from the top left, column by column, so column ("track") 1 = 0–7, track 2 = 8–15 and so on.
**This is exactly the app's bit index** (`EmitterUtils.PositionToIndex`).

**B. Notes on the Control MIDI channel** (Settings → *Control MIDI channel*, set to **In** or **Out**). With
**Out**, the Tempera **transmits** emitter placements as notes. This is the only way to receive emitter
data from the Tempera.

| Note | Meaning |
|---|---|
| 96–99 Note On | select active emitter (96 = blue … 99 = green) |
| 0–91 Note On | place the active emitter. Note = `track * 12 + row`, row 0 = top, rows 0–7 |
| 0–91 Note Off | remove the emitter at that cell |
| 100–107 Note On | arm/disarm recording on tracks 1–8 |

Conversions to the app's bit index (0–63):
- index → note: `note = (index / 8) * 12 + (index % 8)`
- note → index: `index = (note / 12) * 8 + (note % 12)`, valid only when `note % 12 < 8` and `note <= 91`

Beetlecrab's suggested uses for the Control channel are recording a performance into a DAW and mirroring two Temperas.

### Clock

- *Clock source* setting: **Send**, **Receive** or **Internal**, at **24 PPQN**. There's also a Stop/Play control for the tempo.
- On Receive, the Tempera works out the BPM from the incoming clock. **Continue and Song Position Pointer are not
  mentioned at all**, so assume the Tempera ignores them. Start/Stop are uncertain (see the table note above).
- Consequence: when the Tempera is the clock source, the app may get **tempo only, with no Start and so no beat
  position**. Recording therefore needs another way to decide where frame 1 begins (see *Beat phase* in section 4).
- Tempo-synced emitter features (grain *Sync time*, the Euclidean sequencer) follow this clock, so keeping the
  app and the Tempera on one clock also locks grain timing to the app's frames.

### Tempera-side emitter modes that affect what's sent or received

- **2-Lane** (per emitter): the same rule as the app's, enforced on the hardware. Placements arrive already resolved.
- **Lock**: a placed emitter can't be replaced by another colour. The app's model assumes replacement always succeeds.
- **Latch**: once all fingers lift, all emitters clear on the next placement.
- **Clear all** (Round + button on the Emitters page).
- Per-emitter MIDI channel (for notes; doesn't affect the control channel).

### Not in the manual — test on the hardware before building on it

1. With Control channel **Out**, are placements made by *incoming CC from the app* echoed back out as notes?
   If so, the app hears its own output (a feedback loop and duplicates while recording).
2. Does picking an emitter colour on the Tempera send 96–99 straight away, or only just before a placement?
3. What does *Clear all* send: a Note Off per cell, or nothing?
4. Does dragging across cells send rapid Note On/Off pairs, and is Note On with velocity 0 used for Note Off?
5. With Clock source = Send, are Start/Stop sent on Play/Stop? Is Continue ever sent? Is SPP ignored?
6. Usable BPM range when receiving clock (the app currently allows up to 1000 BPM, i.e. 400 pulses a second).
7. Port names on macOS and Windows: is the input port named "Tempera" too, so input and output can be paired automatically?
8. Can the Control channel Out be the same channel as the main receive channel, and does that cause clashes?
9. With Clock source = Receive, does the Tempera react to MIDI Start/Stop from a sequencer (restart its tempo,
   stop grain sync), or only to the clock pulses?
10. With Clock source = Send, is clock sent all the time, or only while the tempo is "playing"? (Clock that stops
    and starts without Start/Stop messages makes the app's tempo reading jump.)
11. Does the first placement after selecting a colour on the Tempera arrive as a select note (96–99) and then a
    place note, or can a place note arrive alone? The decoder needs the colour before the first placement.
12. Through USB host or TRS thru, can a sequencer's clock reach both the Tempera and the app at once, or does the
    app need to forward clock to the Tempera (clock out)?

---

## 2. What DryWetMIDI 8.0.3 provides

- `InputDevice.GetAll/GetByName`, `StartEventsListening()`, `EventReceived` (fires on a **native driver thread**).
  The event args have **no timestamp**, so the app must call `Stopwatch.GetTimestamp()` itself on arrival.
  `SilentNoteOnPolicy` can turn Note On velocity 0 into Note Off.
- Clock events exist as types: `TimingClockEvent` (F8), `StartEvent`, `StopEvent`, `ContinueEvent`, `SongPositionPointerEvent`.
- `MidiClock` with `HighPrecisionTickGenerator` has a **1 ms minimum interval**. A 24 PPQN pulse at 128 BPM is 19.5 ms,
  but at 300 BPM it's 8.3 ms and at 1000 BPM 2.5 ms, so 1 ms jitter is noticeable. **Recommendation: keep a custom
  high-priority thread with Stopwatch deadlines plus a final short spin**, as `PlaybackManager` already does, and compute
  every pulse from an absolute start time so errors don't accumulate.
- `DevicesWatcher` (macOS add/remove callbacks) covers input devices too. Windows still needs polling (`InputDevice.GetAll`).
- On Windows, MIDI ports are usually **exclusive**: if a DAW has the Tempera open, the app can't open it (input or output).

---

## 3. How the app worked before phase 1 (the parts that change)

Kept for reference. Phase 1 has since replaced the timing, sending and shared-list parts (see section 7, *Status*).

- **Time** = "one frame per beat". `PlaybackManager` has its own thread that sends a frame's CC changes, then
  waits `60 / bpm` seconds. `FrameSequenceManager` moves on when that wait finishes (with Pausing/Stopping/Seeking
  states to cover the wait).
- **Transforms** tick in `TransformManager.Update()` using `Time.time`, so they're tied to the Unity frame rate
  (about 16 ms of jitter at 60 fps) and not locked to the sequencer or any external clock.
- **Live edits** (mouse drawing) send CCs **directly from the main thread** (`PlaybackManager.AddEmitter`/`RemoveEmitter`),
  bypassing the playback thread.
- **Devices**: output only (`OutputDevice`), with one dropdown.
- **Frames**: `Frame` = `ulong[4]`, an absolute snapshot. No timing inside a frame.
- **Recording** = transform output inserted as new frames, only while stopped (the Record panel is disabled during playback).
- **Files**: one frame per quarter note at TPQN 120, clear-all and placements written 1 tick apart, `START_FRAME_NO_n` markers.
- **Existing hazard**: the Create and Frame panels stay enabled during playback (`UIStates.Playing`), so frames can be
  added or deleted while the playback thread reads the same `List<Frame>` (`FrameSequenceManager.frames` is
  `FrameManager.frames`). Recording during playback (needed below) makes this a real problem, so the redesign has to fix it.

---

## 4. Target design

```
                  ┌──────────────── Clock source ─────────────────────┐
 Internal/Send ──►│ ClockEngine internal clock (own thread, Stopwatch)│
 Receive ────────►│ ExternalClockFollower (F8, plus FA/FB/FC/SPP if   │◄── Clock-in device
                  │ the sender sends them)                            │    (sequencer, DAW or Tempera)
                  └──────────────────┬────────────────────────────────┘
                                     │ pulse n (24 PPQN), start/stop/continue
                                     ▼
                Transport (song position in ticks, beat phase, state)
                     │             │                     │
           Sequencer step    Transform step     Recorder (armed → trigger → capturing)
                     │             │                     ▲
                     ▼             ▼                     │
        MidiOutputScheduler (clock thread)       MidiInputRouter (one listener per open input)
        F8 first, then CC bursts                  ├─ Clock-in device:   clock msgs → ExternalClockFollower
             │                  │                 └─ Tempera-in device: control notes → TemperaControlDecoder
             ▼                  ▼                          → live mirror (main thread) + Recorder (trigger + capture)
     Tempera-out device   Clock-out device(s)
```

Principles:
1. **One timebase.** The transport's position (in ticks) is the only notion of time. Sequencer steps, transform
   steps and recording timestamps all come from it, whichever clock mode is active.
2. **All MIDI output goes through one sender thread** with priority for realtime messages (F8 is never delayed behind
   a burst of 70 CCs). That includes mouse edits, which currently send from the main thread.
3. **The main thread prepares; the clock thread sends.** Anything the clock thread reads (the frame sequence, the
   transform settings, the current frame) is published as an **immutable snapshot**, replaced whenever the main thread
   changes it. The clock thread never touches Unity objects. Results go back through queues that `Update()` drains,
   as `FrameSequenceManager` already does with its volatile flags.
4. **Input is stamped on arrival** (Stopwatch time → transport position), then queued. Clock messages are handled
   straight away on the driver thread so slave timing stays tight.
5. **Devices have roles, not one pairing.** The Tempera and the clock source are independent choices and can be
   different devices. Each input is listened to only for its role's messages.

### Device roles

| Role | What it carries | Choice |
|---|---|---|
| **Tempera out** | emitter CCs 10/11/12 (as today) | an output; default: the port named "Tempera" |
| **Tempera in** | Control-channel notes (placements, removals, colour select) | an input; default: the port named "Tempera" |
| **Clock in** | F8 clock, plus Start/Stop/Continue/SPP if the sender sends them | None, or any input: sequencer, DAW, or the Tempera |
| **Clock out** | F8 clock (+ Start/Stop/Continue/SPP options) | None, or one or more outputs (usually the Tempera) |

How the roles appear in the UI (section 6):
- **In** dropdown (header) = Tempera in. Clock in defaults to *Same as In*.
- **Out** dropdown (header) = Tempera out. Clock out defaults to *Same as Out*.
- **Clock from** / **Clock to** (Clock surface) override those defaults only when the clock uses a different device,
  e.g. a hardware sequencer. Most setups never change them.

- Several `InputDevice`s can be open at once (the Tempera and a sequencer), each with its own `EventReceived` handler.
  Clock messages are taken only from the Clock-in device; control notes only from the Tempera-in device on the
  configured Control channel. If Clock in and Tempera in are the same port (the Tempera as clock source), one
  listener does both.
- Roles are saved (PlayerPrefs, by port name) and re-opened when the port reappears.
- Windows: ports are usually exclusive, so a port the DAW already has open can't be opened by the app.

Typical setups:

| Setup | App clock mode | Clock from | Clock to | Tempera clock source | Notes |
|---|---|---|---|---|---|
| A. App is master | Send | – | Same as Out (Tempera) | Receive | App sends F8 and Start/Stop. Grain sync follows the app |
| B. Sequencer is master, app forwards | Receive + forward | Sequencer | Same as Out (Tempera) | Receive | All three share one clock. Start from the sequencer can trigger recording |
| C. Sequencer drives both directly | Receive | Sequencer | None | Receive (via its own MIDI in) | Needs a MIDI split or thru, or the Tempera's USB host (hardware test 12) |
| D. Tempera is master | Receive | Same as In (Tempera) | None | Send | Probably tempo only, no Start (tests 5 and 10). Recording starts on the first touch |
| E. No sync | Internal | – | – | Internal | Today's behaviour |

Only setups B and C need *Clock from* changed; everything else works with just the In and Out dropdowns.

### Clock modes

| App mode | App does |
|---|---|
| Internal | runs its own clock, sends no F8 |
| **Send** | runs its own clock and sends it to the Clock-out device(s) |
| **Receive** | follows the Clock-in device's F8. Uses Start/Stop/Continue/SPP when they arrive, but works without them. BPM is measured. Can also forward the clock to the Clock-out device(s) (setup B) |

In Receive mode the app's BPM slider becomes a read-out (moving average of pulse intervals) and its transport
buttons can't move the external clock. Play still works: it starts the sequencer on the next beat of the incoming
clock (see *Beat phase*). If the clock stops arriving for about half a second, the app shows "no clock" and pauses.

### Key decisions (recommendations in bold)

1. **Tick resolution stored in frames.** Options: 24 (= clock pulses) or 96 ticks per beat.
   **Store 96** (4 per pulse; also the MIDI file TPQN). When the app is master, play at 96. When following external clock, place
   sub-pulse events by interpolating within the measured pulse period, or round them to the nearest pulse (simpler, about 5 ms at 128 BPM).
   24 alone would be simpler throughout but gives about 20 ms resolution at 128 BPM and 42 ms at 60 BPM.
2. **Frame model.** **Keep frame = one beat**, so the slider, transforms, copy/paste and the file format stay as they are, and add to each frame:
   - `Start` snapshot (`ulong[4]`, what the frame is today: absolute state at tick 0, used for seeking and display);
   - `Events`: a sorted list of `{tick 0–95, type Select/Place/Remove/ClearAll, cell, emitter}` played during the beat.
   The end state is derived (Start + Events) and becomes the next frame's starting point when recording.
   Old frames are simply "Start with no Events". Alternatives, such as variable-length frames or one continuous event list
   with snapshots as a cache, are more flexible but touch everything.
3. **What edits do to Events.** Drawing on the grid while stopped edits `Start`. Clear frame clears both. Transforms read
   and write `Start`, and should probably drop Events (or apply the transform to Events too: shift and flip can map
   cell numbers inside events, swap can remap colours; random can't). Copy, paste and duplicate copy both.
4. **Transform timing.** Make each transform step a pure function (`groups + settings snapshot + beat → groups`); the bit
   maths is already isolated in the `Do*Transform` methods. Then call it on the clock thread at each beat boundary,
   so transforms land exactly on the beat in every clock mode, and post the result to the main thread for
   `FrameManager` and the UI. The alternative is computing one beat ahead on the main thread, which is simpler
   to refactor but adds a beat of latency to button presses.
5. **Recording semantics.** Recording is **armed** and then **triggered** (see *Record arming and triggers* below).
   Once triggered: source = Tempera input, Transforms, or both; mode = **Replace** (default: overwrite frames as the
   playhead passes) or Overdub (add events into existing frames); frames are appended past the end while recording.
   While recording from the Tempera, **stop sending emitter CCs** for the recorded frames to avoid echo (decided
   after hardware test 1).
6. **Live mirror.** Even when not recording, incoming Tempera placements should update the current frame and the grid
   ("follow the hardware"). Make it a toggle, so you can choose between the app and the hardware as the source of truth.
7. **Reverse playback with events inside the beat.** **Play frames in reverse order but each frame's events forwards**;
   true reversal would mean inverting each event.
8. **External Start position.** On MIDI Start, **play or record from the app's anchor frame**, matching pressing Play,
   unless an SPP arrives first (DAW case). Without Start, see *Beat phase*.

### Record arming and triggers

Record becomes a two-step control: **Arm**, then recording begins when the chosen **trigger** happens.

| Trigger | Starts recording when | Needs |
|---|---|---|
| **MIDI Start** | Start (FA) arrives from the Clock-in device | a sequencer/DAW that sends Start |
| **First touch** | the first placement note (Note On 0–91) arrives from the Tempera-in device | Tempera Control channel set to Out |
| **Either** (default) | whichever of the two comes first | – |
| **Now** | straight away (with the internal clock, or on the next beat of an external one) | – |

- While armed, the app shows ARM (flashing), keeps decoding Tempera notes for the live mirror and the current colour,
  but writes nothing into frames.
- Recording stops on MIDI Stop from the Clock-in device, on pressing Record again, or optionally after a set
  number of frames.
- Colour-select notes (96–99) never trigger: picking a colour isn't a musical event, but the decoder remembers it so
  the first placement gets the right colour (hardware test 11).
- The trigger note itself is recorded as the first event, at tick 0 of frame 1 (see *Beat phase*).
- The trigger choice and arm state go on the Record surface (section 6). Arm/disarm can also come from a keyboard
  shortcut, and later possibly from the Tempera's track-arm notes (100–107) if that proves useful.

### Beat phase (where frame 1 begins)

MIDI clock on its own gives **tempo but not position**: 24 pulses make a beat, but nothing says which pulse is
the first of a beat. If the app just counted 24 pulses from whenever it started listening, its frame boundaries
would sit at a random point inside the sender's beat. So the start of frame 1 is always set by an event:

| Situation | Frame 1, tick 0 is | Then |
|---|---|---|
| MIDI Start received | the **first F8 after Start** (as the MIDI spec defines) | each further 24 F8s = one frame; SPP, if sent, sets the position |
| First-touch trigger, following external clock without Start | the **trigger note's arrival, rounded to the nearest pulse** | frames every 24 pulses from that pulse |
| First-touch trigger, internal clock | the **trigger note's arrival time** (the clock restarts at that timestamp) | frames every beat from there |
| Play pressed while following external clock without Start | the **next pulse** after the press | frames every 24 pulses |

This needs `ClockEngine.Restart(atTimestamp)` (restart at a past timestamp, i.e. the note's arrival time, rather
than now) and an external-clock mode where pulses are counted from a chosen "pulse 0". Rounding to the nearest
pulse is at most half a pulse off: about 10 ms at 120 BPM.

### Starting state when recording from the Tempera

The app only knows emitters it **heard being placed**. Anything already on the Tempera before the app was listening
(or placed while the Control channel was off) is invisible to it. So the first recorded frame's `Start` snapshot can
be wrong unless both sides agree first. Options when arming:
- **Clear both** (default): send clear-all (CC 12 = 64) to the Tempera and start frame 1 empty;
- **Push app state**: send the app's current frame to the Tempera and use it as frame 1's `Start`;
- **Trust the mirror**: use whatever the live mirror currently shows (fine if the app has been listening all along).

Pushing or clearing sends CCs, so this depends on hardware test 1 (whether those come back as echoed notes).

---

## 5. Where the code changes

| Area | File(s) | Change |
|---|---|---|
| Clock (exists, extend) | `Midi/Clock/Managers/ClockEngine.cs` + new `ExternalClockFollower.cs`, `Core/Enums/ClockMode.cs` | Phase 1 built the internal clock. Add: per-pulse steps, `Restart(atTimestamp)`, external mode driven by received F8 with a chosen "pulse 0" (beat phase), Start/Stop/Continue/SPP handling, BPM measurement, "no clock" timeout, clock forwarding to Clock-out |
| Output (exists, extend) | `Midi/Output/Managers/MidiOutputScheduler.cs` | Phase 1 built it for the Tempera out. Add: realtime messages (F8/Start/Stop) sent first, to the Clock-out device(s) as well as or instead of the Tempera |
| **New** input | `Midi/Input/MidiInputRouter.cs`, `TemperaControlDecoder.cs` | Opens one `InputDevice` per role (or one shared one), stamps events on arrival, routes clock messages from Clock-in and control notes from Tempera-in, note ↔ cell conversion, remembers the selected colour |
| **New** recording | `Recording/Managers/MidiRecorder.cs`, `RecordingBuffer`, `Core/Enums/RecordTrigger.cs`, `RecordState.cs` | Idle / Armed / Recording states; triggers (MIDI Start, First touch, Either, Now); sets beat phase on trigger; starting-state option (clear / push / trust); captures timed events against transport ticks, builds frames, Replace/Overdub |
| **New** entity | `Frame/Entities/FrameEvent.cs`, `Core/Enums/FrameEventType.cs` | Timed event struct |
| Done in phase 1 | `PlaybackManager.cs` → `MidiOutputScheduler.cs`; `TransientState.cs` removed | – |
| Extend | `Midi/Playback/Managers/FrameSequenceManager.cs` | Phase 1 drives it by clock beats. Add: plays Events at their ticks, follows external Start/Stop/SPP, starts on the next beat in Receive mode |
| Major | `Frame/Entities/Frame.cs`, `Frame/Managers/FrameManager.cs` | Start + Events model, deep copies, publishing immutable sequence snapshots for the clock thread, applying recorder output, live mirror input |
| Major | `Transform/Managers/TransformManager.cs`, `Transform/Services/*` | Tick from transport beats instead of `Time.time`, pure step functions plus a settings snapshot, results posted back to the main thread |
| Major | `Midi/Core/Services/MidiTransformService.cs`, `Utils/MidiUtils.cs` | File format v2: TPQN 96, snapshot at the frame tick, Events at their offsets, format marker. Legacy loader for v1 (TPQN 120, 1-tick spacing). Optional import of DAW recordings of control-channel notes |
| Major | `Midi/Devices/Managers/DeviceManager.cs` | Enumerate inputs as well as outputs; hold the four role assignments (Tempera out/in, Clock in, Clock out); default Tempera in/out to the "Tempera" ports; reopen roles when ports reappear; per-role disconnect handling; Windows polling for both |
| Major | `Midi/Core/Managers/MidiTempoManager.cs`, `Controllers/MidiTempoEventController.cs` | BPM comes from the internal setting or the measured external clock. Read-only in Receive mode |
| Medium | `Midi/Core/Services/MidiImmediateManager.cs`, `Controllers/MidiImmediateEventController.cs` | Route through the output scheduler. Suppress while the transport plays or records |
| Medium | `Settings/Managers/MidiSettingsManager.cs`, `MidiConfig` asset | New settings: clock mode, control channel (in), device roles, record trigger, starting-state option, note constants (96, 100), clock-out options (send Start/Stop, Continue/SPP), no-clock timeout |
| Medium | `Recording/Controllers/RecordEventController.cs`, `UI/Recording/Managers/RecordUIManager.cs` | Record button arms/disarms; trigger, source and mode; ARM (flashing) vs REC display |
| Medium | `UI/State/Enums/UIStates.cs`, `UI/State/Managers/UIStateManager.cs` | New states: recording while playing, following external clock (transport and BPM locked), no input device |
| Medium | `UI/Devices/Managers/DeviceUiManager.cs`, header in `MainScene` (`TitlePanel`, `DevicePanel`) | Two dropdowns (In, Out), each with its own placeholders and current selection; `DevicePanel` stacked to about 90 px; title shrunk or replaced by the TM icon |
| Medium | `UI/Display/*`, `UI/Settings/*`, `MainViewUIManager`, `PlaybackUiManager` | Main view surfaces (see below), position read-out, clock and sync indicator, Clock from/to choices |
| Small | `Utils/EmitterUtils.cs` | `IndexToControlNote` / `ControlNoteToIndex` |
| Small | `Input/Core/Actions/InputActions.inputactions` (+ regenerate `InputActions.cs`) | Actions for the new buttons and surface switching |
| Unchanged | Grid, `MouseManager`, `LogMan`, `ConfigRegistry`, `AspectCorrectionService`, custom button classes, transform maths | – |

---

## 6. UI — keeping the current design

### Header: In and Out dropdowns

Today the title bar (`TitlePanel`, 720×100) holds the "TemperaMental" title on the left and `DevicePanel` (300×48)
on the right: a "Device" label and one 200×40 dropdown (output only).

Proposal: **two stacked dropdowns, labelled In and Out**, replacing the single Device dropdown.
- Each row about 36–40 px tall, so both fit in the 100 px title bar. `DevicePanel` grows to about 90 px tall and its
  background image is resized to match.
- **The title can shrink as much as needed, or be replaced by a TM icon** (planned), which frees horizontal room. The
  panel can then be wider, so long port names aren't cut off, or the labels can sit beside the dropdowns instead of above.
- Both default to the port named "Tempera" and remember the last choice (PlayerPrefs, by port name). The "Connect
  Midi Device" / "Select Midi Device" placeholders work as today, separately for each.
- Out keeps today's behaviour exactly. In opens the input and starts listening; with no input chosen, the app
  works as it does now (no mirror, no clock in, no recording from the Tempera).
- Keyboard: none, as today (dropdowns are mouse-only).

### Display surfaces

The display area already supports multiple views, each with its own LCD canvas and a **32 px button row** underneath
(`DisplayPanel/<View>/DisplayCanvas` + `ButtonPanel`). Main view = the BPM/Transform/Frame/Emitters LCD + Load/Append/Save.
Settings views have `<` / `>` buttons at each end of the row, cycled by `SettingsUIManager`.

Proposal: give the **main view switchable surfaces** that swap only the button row (and, if needed, a line of LCD text),
using the same `<` `>` arrangement as the settings views:

| Surface | Buttons (3–5 fit at the current widths) | LCD additions |
|---|---|---|
| File | Load · Append · Save (as now) | – |
| Clock | Source (Int / Send / Recv) · Clock from ◄► (Same as In / other inputs) · Clock to ◄► (Same as Out / other outputs / None) · Send Start/Stop on/off | Source + "SYNC" / "NO CLOCK" indicator; BPM shows the measured value in Recv; Clock from/to shown only when not "Same as …" |
| Record | Arm · Trigger (Start / Touch / Either / Now) · Source (Tempera / Transform / Both) · Mode (Replace / Overdub) · Start state (Clear / Push / Mirror) | ARM (flashing) / REC state, trigger name |

There's no separate Devices surface: In and Out live in the header, and the clock overrides on the Clock surface.

LCD: replace or add a **position read-out** (`frame.tick` or `bar.beat.tick`) next to the Frame counter while playing.
The Tempera's *Control channel* goes into the existing MIDI Settings view (it has room for one more row). Clock
and Record each have more options than fit in one row, so either those surfaces use two pages each, or the rarely
changed options (Clock from/to, Send Start/Stop, start state) move to the settings views. The settings views also
work, since they already page with `<` `>`.

Implementation: a small `MainViewSurfaceManager` (like `SettingsUIManager`) toggling child `ButtonPanel`s. Keyboard
handlers on inactive surfaces unsubscribe themselves (`OnDisable`), so shared keys across surfaces just work.

---

## 7. Suggested order of work

Each phase should leave the app working.

### Status

**Phase 1 — done (2026-10-23, commit `d7da7f4`).**
- New `Midi/Clock/`: `ClockEngine` (one high-priority thread, 96-tick transport anchored to `Stopwatch`, deadlines
  calculated so they don't drift, sleep then 2 ms spin, epochs on restart, `OnBeat` on the clock thread), `ClockEventController`
  (BPM from `MidiTempoManager`), `ClockBeat`.
- `PlaybackManager` → `Midi/Output/MidiOutputScheduler` (same GUID): a command queue drained only by the clock thread,
  frame diffs against the device state, consecutive frames collapsed, mouse edits queued instead of sent from the main thread.
  `PlaybackManagerEventController` → `MidiOutputEventController`.
- `FrameSequenceManager` advances on clock beats; play, resume and seek restart the clock; no more waiting states
  (`TransientState` removed).
- Transforms tick from clock beats (counted on the clock thread, processed in `Update`) instead of `Time.time`.
- Shared-list hazard fixed: the sequencer reads `FrameManager.PublishedSequence` (immutable snapshots).
- Scene: `ClockEngine`, `ClockEventController`, `MidiOutputScheduler`, `MidiOutputEventController` under `Midi`;
  BPM listeners moved to the clock; all UnityEvent target type names refreshed.
- Measured with a macOS virtual loopback device: 120 BPM → 499.97–499.99 ms per beat (SD ≤ 0.2 ms), 240 BPM →
  250.00 ms (SD 0.02 ms), no drift; seek sends in under 1 ms; pause, resume, stop and loop behave as before.

**In/Out dropdowns + MIDI monitor — done (2026-10-23/24, commit `4ebcf5a`).** Brought forward from phase 3.
- Header: stacked **In** and **Out** dropdowns (two `DeviceUIManager`s). `DeviceManager` handles inputs too,
  remembers the last-used devices and reselects them on start-up and reconnect.
- `Midi/Input/`: `MidiInputRouter` (opens the In device, timestamps on arrival, driver-thread and main-thread events),
  `MidiInputEventController`, `MidiMonitor` (logs IN and OUT with timings to `midi-monitor.log`, the console and the
  status line, labels Tempera Control-channel notes and the app's CCs, summarises clock).
- `MidiMessageData` / `MidiMessageKind`: thread-safe copies of MIDI messages. `EmitterUtils` has the note ↔ cell conversions.
- Tested with a loopback device: the app's CCs and simulated Tempera notes were logged and labelled correctly.

**Phase 2 — done (2026-10-24, commit `4ebcf5a`).**
- `ClockEngine`: Internal / Send modes. Send steps every pulse and sends F8 to the Out device ahead of the beat's
  frame; Start (play, seek), Continue (resume), Stop (pause, stop, end) when "Start/Stop" is on. Internal steps per beat.
- Transforms run on the clock thread under a lock (`System.Random` in the random transform); results are sent on the
  beat and applied to frames from `Update`.
- **Clock Settings view** (third App Settings page): Clock `<` `>` Internal/Send, Start/Stop Off/On, saved on close.
- `PlatformTimingActivity`: latency-critical activity on macOS (no App Nap or timer coalescing), `timeBeginPeriod(1)`
  on Windows. Without it, a background Editor's pulses were up to 19 ms late.
- `ClockEngine.TakeTimingStats()` + monitor reports of clock-thread lateness.
- Measured with a fake capture device at 120 BPM: pulses 20.833 ms ±0.001; frames 0.05 ms after the beat pulse;
  transforms 0.1 ms after it (before: up to 16 ms); Internal sends no pulses.
- Testing note: two Editor crashes came from DryWetMIDI virtual devices created by test code, not the app. Phase 2
  was then verified with a fake `IOutputDevice` instead (see `CLAUDE.local.md`).

**Phase 3 — done (2026-10-24, commit `7841529`).**
- `ClockEngine` Receive mode: driven by F8 from the clock source, processed on the clock thread. Song epochs follow the
  source's position after Start / Song Position / Continue (frozen while it's stopped); free epochs count pulses for
  sources without Start. The app's Play waits for the source's next beat when its position is known, otherwise the
  next pulse. Tempo is measured over 24 pulses and shown on the BPM display (slider locked). "No MIDI clock" after 0.5 s.
- Source transport drives the sequencer (`OnExternalTransport`): Start plays from the current frame, Stop pauses,
  Continue resumes, Song Position seeks to frame `beat + 1`. Clock and transport are forwarded to the clock
  destination unless it is the same device as the source.
- **Clock Devices view** (fourth App Settings page): Clock From (Same as In / any input) and Clock To (Same as Out /
  None / any output). `DeviceManager` opens separate clock devices only when needed and reports `ClockRouting`.
  `ClockInputRouter` listens to a separate clock input; the monitor logs it as `CLK`.
- Tested with a simulated master clock and fake output: transforms and frames land 0.03–0.3 ms after the master's beat
  pulses; Start/Stop/Continue/Song Position behave as above; app Play pressed mid-beat started on the next master beat;
  tempo 120 → 140 followed; clock loss detected; all pulses forwarded. Send mode re-tested with no change.

**Phase 4 — done (2026-10-24, commit `a7155e6`).**
- `TemperaControlDecoder`: Control-channel notes → select / place / remove / arm-track events (`TemperaControlEvent`,
  with arrival timestamps for recording later). The selected colour is remembered, and placements are resolved for the
  2-Lane settings.
- Device record: every Tempera change updates the output scheduler's view of the device (no send), so later frame
  diffs are correct whether or not the app follows.
- **Follow Tempera** mirror (on by default): changes from the hardware go into the current frame and grid, not while
  the sequencer plays. Loop protection: changes from the Tempera are never sent back, repeats are ignored, and colour
  selects the device already has are skipped.
- **Tempera Settings view** (fifth App Settings page): Control Channel − + (default 16) and Follow Tempera Off/On.
- Tested with simulated notes and fake output. Selection, place, echo (no change), remove, wrong channel ignored,
  follow off (frame untouched, hardware-only emitter removed by the next frame sent), 2-Lane resolution, and
  playback (frame untouched, device record updated) all behaved as above, and nothing was ever sent back to the Tempera.

**Phase 5 — done (2026-10-24, commit `e4b725e`).**
- Frames keep their start groups (one beat, as before) and gain timed **events** (`FrameEvent`: tick 0–95, Select /
  Place / Remove / ClearAll), in tick order. `FrameSnapshot` (start groups + events, immutable) is what the clock
  thread and the transforms see. Clear removes events; copy, paste, duplicate and recording keep them.
- Playback: each frame's start groups on its beat, then its events at their ticks. The clock gained `OnTick` and
  `ScheduleTick` for steps between beats; in Receive, scheduled ticks are timed from the measured pulse length
  (until a tempo is measured they wait for the next pulse). Reverse plays frames backwards and events forwards. The
  grid follows the events as they play; pause and stop return the grid and device to the frame's start.
- Transforms move events with the emitters (each placement or removal passed through as a lone emitter, so 2-Lane
  applies and off-grid ones drop). Swap remaps Select colours. Random drops events when it changes the emitters.
- File format 2: TPQN 96, single-track file, format marker, absolute start state per frame, a `FRAME_EVENTS` marker and
  events at their ticks as CCs (playable from a DAW). Format 1 files load as before. A split-track copy (as a DAW may
  save it) loads with tick-0 events folded into the start state.
- Clock beat/tick handlers no longer allocate per call (pulse SD 0.0004 ms during looping playback with clock out).
- Tested with fake output and a simulated master clock: events within 0.7 ms of their ticks in Internal and Send,
  within 0.05 ms in Receive with a measured tempo; transforms, edits, files, pause and the grid display as above.
- Not done: importing a DAW recording of the Tempera's Control-channel notes (optional extra). Nothing in the UI shows
  or creates events yet (phases 6 and 7).

**Phase 6 — done (2026-10-24, commit `463e381`).**
- `MidiRecorder` (main thread) and the sequencer record what's played on the Tempera. Record arms (button flashes);
  triggers: Start (Play from stopped, or the clock source's MIDI Start), Touch (first placement that changes what the
  Tempera shows), Either, Now. Frame 1 begins at the trigger: the first pulse after Start, or the touch itself
  (`ClockEngine.Restart(transport, atTimestamp)`; following clock, the pulse nearest the touch). Armed or triggered
  while playing, it punches in at that moment.
- Notes are placed by their arrival time: `ClockEngine.TryGetTickAt(timestamp)` gives the epoch and tick from the latest
  anchor or pulse, and the sequencer's beat history gives the frame.
- **Replace**: nothing is sent; frames are rebuilt as the playhead reaches them (each starting where the last ended)
  and added past the end; loop and reverse are ignored. **Overdub**: frames play as usual and placements/removals are
  added to the frame playing (colour selects aren't, since playback sends its own); loop layers each pass. Notes that
  change nothing at their tick (echoes of the app's own output, repeats) aren't recorded, so hardware test 1 no
  longer decides the design.
- **Starting state** (Replace from stopped), applied when armed: Clear, Send (the current frame), Keep (the device
  record). What's played while armed is part of it.
- Stops on Record, after a length (Off/1/2/4/8/16/32/64 frames, at the beat), on pause/stop or the source's MIDI Stop,
  or when the epoch changes (seek, Start, Song Position). Playback then carries on normally.
- **Record Settings** page (sixth App Settings page): Source (Tempera / Transforms: the v1 transform recording),
  Trigger, Mode, Start, Length. While recording only Playback, Device, Frame and Record panels are usable; drawing
  and Follow Tempera pause.
- Tested with simulated notes, a fake output and a simulated master clock: Replace by touch and by Play with a length,
  Overdub on a loop with echoes ignored, punch-in while playing, MIDI Start and touch triggers while following clock
  (touch on the nearest pulse), the Transforms source, panel states and the settings page. Phase 5 timing and phase 2
  transport rechecked unchanged.
- Fixed while here: the clock and Tempera settings objects (phases 3 and 4) were under the Settings button in the
  Canvas; moved to the `Settings` root.

**Phase 7 — done (2026-10-24, commit `f11be47`).**
- The main view's button row has three surfaces, stepped with `<` `>` at its ends (shown during playback too):
  **File** (Load · Append · Save), **Clock** (Clock mode · Start/Stop) and **Record** (Trigger · Mode · Start · Length).
  Buttons show a dimmed caption and the current value, and save the setting when pressed. The surface shown is
  remembered. Clock from/to, the Tempera settings and the recording source stay on the settings pages.
- LCD status line between the values and the message line: the clock (Internal clock / Sending clock / External
  clock / No clock) on the left, recording (Armed: trigger / Rec mode / Rec transforms) on the right.
- Gating: File locks during playback as before; the Clock surface locks while recording (a clock mode change would
  restart the clock's position under the recording); Record stays available.
- Not done: the position read-out (`frame.tick`) suggested in section 6. Playback already shows the frame, and
  within a beat the tick changes too fast to read.
- Checked in play mode with screenshots: switching, the remembered surface, stepping Trigger, arming (status and
  flashing button), panel states while playing and recording, and Receive with clock arriving and stopping.

Still open:
- **Hardware tests with the Tempera** (section 1 list, especially 1–5 and 9–12), now possible with the monitor.
  A step-by-step session for the record, save and replay loop is in [TemperaHardwareTest.md](TemperaHardwareTest.md).
  Test 1 (echo) is now handled either way; tests 2, 3, 4 and 11 decide whether the decoder's assumptions hold.
- Song Position isn't forwarded to the clock destination (it isn't a realtime message; the Tempera ignores it anyway).
- Each restart (play, resume, seek) gives one shortened pulse interval; check whether the Tempera's tempo reading blips.
- Max BPM 1000 still allowed while sending clock (2.5 ms pulses); decide a limit after hardware test 6.

1. **Clock engine + output scheduler, Internal mode only.** Replace the per-frame wait with a 24 PPQN pulse clock (96-tick
   transport). Sequencer and transforms are driven by it. All sends go through the scheduler. *Goal: same behaviour as
   today, tighter timing, transforms locked to the beat.* Fix the shared-list hazard here (immutable sequence snapshots).
2. **Clock Send.** Per-pulse steps, F8 + optional Start/Stop (+ Continue/SPP option) to the Clock-out device.
   Test with Tempera in Receive (setup A) and run hardware tests 6 and 9. Settle the max BPM for clock out.
3. **Device roles + clock Receive.** Input enumeration, the **In and Out header dropdowns**, the four roles
   (Clock from/to defaulting to In/Out), `MidiInputRouter` with several inputs open, `ExternalClockFollower`, BPM
   measurement, beat phase from Start (or from the next pulse on Play), "no clock" handling, forwarding to Clock out
   (setup B). Test with a sequencer or DAW, and with the Tempera on Send (setup D; hardware tests 5, 10 and 12).
   *The In/Out dropdowns don't depend on the clock work and can be built earlier, ahead of phase 2, for example
   with In just opening the input and logging what arrives.*
4. **Tempera control-channel input + live mirror.** Decoder (including remembered colour), grid follows hardware,
   run hardware tests 1–4, 8 and 11.
5. **Frame Events model + playback of events within the beat + file format v2** (with the legacy loader).
6. **MIDI recording with arming.** Idle/Armed/Recording, the four triggers, beat phase on trigger
   (`Restart(atTimestamp)` and external pulse 0), starting-state option, Replace first then Overdub, recording while
   playing, stop on MIDI Stop / Record / length, UI states.
7. **UI surfaces** (some controls can sit in the MIDI Settings view temporarily during phases 2–4).

---

## 8. Other notes

- Max BPM 1000 is far beyond typical MIDI clock use. Consider limiting it while sending clock (300 is common), pending hardware test 6.
- CC 12 value 64 clears **all** emitters on the Tempera (manual), so `SendClearAllEmitters` and the per-frame clear in saved files can
  send it once instead of four times. Keep the old pattern on file load for compatibility.
- When sending a full frame change as about 70 CCs over USB, bandwidth is fine; over TRS (31.25 kbaud) the same burst takes about 65 ms
  and would delay clock pulses. Another reason for realtime priority in the scheduler and for minimal diffs.
- The Tempera can also be driven by Control-channel **notes** instead of CCs. That's not needed for sending, but it means a
  DAW recording of the Tempera's Control channel could be imported as frames (a cheap extra in phase 5).
