# Tempera hardware test: record, save and replay

A short session with the real Tempera to check the parts that have only been tested with simulated notes. Test
numbers in brackets refer to the list in [MidiClockRedesign.md](MidiClockRedesign.md), section 1. About 20 minutes.

**Watch the MIDI monitor while you go.** Incoming messages flash on the display's message line, and everything is logged
with timings to `~/Library/Application Support/com_ezoray/TemperaMental/midi-monitor.log` (the previous run is kept
as `midi-monitor.prev.log`). Tempera notes are labelled, e.g. `Tempera: place track 1 row 1 (cell 0)`,
`Tempera: select Green`, `Tempera: remove …`. Track 1 is the left column and row 1 the top row.

## Setup

1. **Tempera**
   - Main MIDI channel: the app's MIDI channel (app: Settings → MIDI Settings, default 1). This is how the app
     plays to the Tempera.
   - Control MIDI channel: **Out**, on a different channel. Use 16, or whatever the app's Tempera Settings page says.
   - Clock source: Internal (this test uses the app's own clock).
   - Load a canvas you know, and note its name: playback needs the same canvas loaded.
2. **App**
   - Header: **In** and **Out** both set to the Tempera.
   - Tempera Settings: Follow Tempera **On**.
   - Record Settings (or the Record surface): Source **Tempera**, Trigger **Touch**, Mode **Replace**, Start
     **Clear**, Length **Off**.
   - Clock surface: Clock **Internal**.

## Steps

Stopped and not recording for steps 1–5: Follow Tempera mirrors each change onto the app's grid.

| # | Do this on the Tempera | Expect | If not, it means |
|---|---|---|---|
| 1 | Place an emitter in the top-left cell, then the bottom-right, then column 3 row 5 | The app's grid shows the same cells. The monitor shows `place track 1 row 1 (cell 0)`, `place track 8 row 8 (cell 63)`, `place track 3 row 5 (cell 20)` | The cell mapping is off: note whether it's mirrored up/down or left/right |
| 2 | Pick Green **without placing**, wait, then place one *(2)* | `select Green` appears as soon as you pick it; the placed emitter is green in the app | The colour is only sent with a placement, or not at all |
| 3 | Pick Red and place straight away, several times quickly *(11)* | Each burst is `select Red` then `place …`, and the app shows red | A place arrived before its select: placements could get the wrong colour |
| 4 | Remove an emitter the usual way, then drag a finger across a row *(4)* | `remove …` for the removal; the drag shows a run of places and removes, and the app's grid ends up matching | Note anything that doesn't appear, or appears twice |
| 5 | Use Clear All (Round + button on the Emitters page) on a few emitters; then try Latch if you use it *(3)* | A `remove …` for every emitter, and the app's grid clears | If nothing arrives, recordings can't capture Clear All or Latch, and replay would keep those emitters |
| 6 | In the app, draw an emitter with the mouse *(1)* | The monitor shows the OUT CCs. Note whether an IN `place …` note follows straight after (an echo) | Both are handled; just report which |

**The loop (steps 7–10)**

| # | Do this | Expect |
|---|---|---|
| 7 | In the app, press **Record** | The button flashes, the status line reads `Armed: Touch`, and the Tempera clears |
| 8 | Play a phrase on the Tempera for about 8 beats: a few placements, a colour change, a drag and a removal | The first placement starts recording (button solid, `Rec Replace`). Frames appear as the playhead moves |
| 9 | Press **Record** to stop, then **Stop**. **Save** the frames to a file | The frame count matches the beats you played |
| 10 | Clear the Tempera, then **Delete All** in the app, **Load** the file and press **Play** | The Tempera replays the phrase: same cells, colours and timing. Listen as well as watch |

**Optional**, if those pass:
- Overdub with Loop on: play over a looping recording. Your additions should come back on the next pass, and the app's
  own playback shouldn't be recorded twice.
- Trigger **Start** with a DAW sending clock and Start to the app (Clock **Receive**). Recording should begin on the
  DAW's first beat *(9, 12)*.
- Control channel on the **same** channel as the main channel: does anything clash? *(8)*

## Notes, canvas and Hold (if those pass)

Tempera: Overlay Keyboard on, **Send MIDI notes** on, keyboard channel not the Control channel.

| # | Do this | Expect |
|---|---|---|
| 11 | Play a few notes on the Overlay Keyboard | The monitor shows `NoteOn`/`NoteOff` on the keyboard's channel, with velocity |
| 12 | Use the keyboard's **Hold**, then **Clear** | One Note On per held note, and a Note Off for each on Clear (note what arrives) |
| 13 | Record (Replace) while holding a note and placing emitters, stop, save, reload, play | The replay sounds: the held note starts with frame 1 and the emitters move as played |
| 14 | On the Tempera surface, step **Canvas** to a number and wait | After a moment the Tempera loads that canvas (the nth in its current folder) |
| 15 | Save with a canvas set, load another canvas on the Tempera, then load the file | The file's canvas loads on the Tempera |
| 16 | Turn **Hold** on with emitters placed | The Tempera sounds without touching it; Hold off silences it |

## Report back

- Pass or fail for each step, and what you saw where it differed.
- The `midi-monitor.log` from the session (or the lines around anything odd).
- For step 10, whether it sounded the same as when you played it.
