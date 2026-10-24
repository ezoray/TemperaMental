# Notes, canvas recall and parameters: design

Follows the [MIDI clock, input and recording redesign](MidiClockRedesign.md). Goal: a recording saved and replayed
later should *sound* the way it was played on the Tempera.

## What the Tempera accepts beyond the emitter CCs

From the manual (firmware 2.3, *MIDI input*, *Settings*, *Emitters*, *Overlay Keyboard*):

- **It needs a voice to make sound**: "To make sound, Tempera needs an emitter placed and voice played." Placing
  emitters (all the app sends today) is silent unless notes are held, on the Overlay Keyboard (its *Hold* works like a
  sustain pedal) or from a MIDI keyboard.
- **Notes**: received on the main MIDI channel, or per emitter (each emitter's *Channel*, or *Global*). Notes played
  on the Overlay Keyboard are **sent** on the keyboard's own *Channel* when *Send MIDI notes* is on. Velocity is sent;
  poly pressure isn't.
- **Program Change 0–127**: loads a canvas from the current canvas's folder, by alphabetical position.
- **CC 13–119**: ADSR, reverb, delay, filter, chorus, the 8 track volumes, 16 parameters for each of the 4 emitters
  (volume, grain length, density, shape, pan, tune spread, octave, relative X/Y, spray X/Y, tone, effects send), and
  10 modulator sizes. Also CC 1 (modwheel), CC 64 (damper), pitch bend and aftertouch. **The Tempera never sends
  CCs**, so these can be sent but not recorded, and the app can't read their current values.
- Control channel notes 100–107 arm tracks for audio recording.

## Step 1: canvas recall (built)

- A **canvas number** (1–128 as shown, Program Change 0–127, or none) belongs to the sequence. It's saved in the file
  as a Program Change at time 0, so a DAW playing the file recalls it too, and loading a file sends it to the Tempera.
  Appending keeps the current canvas.
- Stepping it with the Tempera surface's Canvas − / + buttons (hold to scroll) sends it once it stops changing for
  0.75 s, so scrolling past canvases doesn't load each one. The display's status line shows it.
- Limits: it's a position in a folder, so it recalls the right canvas only while that folder and its names stay the
  same. Loading a canvas takes the Tempera a moment.

## Step 2: notes (built)

- **Frames carry note events** alongside the emitter events: Note On (channel, note, velocity) and Note Off, at their
  ticks. A note can last across frames. Transforms leave notes where they are, and Random keeps them.
- **Recording**: notes arriving from the Tempera on any channel except the Control channel are recorded, in Replace and
  Overdub (turn *Send MIDI notes* off on the Tempera to record emitters only). Notes already held when a recording
  starts (including one held before arming) are recorded as Note Ons at its start, and notes still on when it ends get
  a Note Off there, so a held drone is captured and nothing hangs.
- **Playback**: notes are sent at their ticks on their channels. At the start of every frame the sequencer works out
  which notes should be sounding there (from the frames before it) and corrects what's sounding, so starting part way
  through, seeking, looping and reverse all leave the right notes held. Pausing, stopping and the end of playback
  release them. A Replace recording releases them as it starts and sends none.
- **Live notes win**: the sequencer doesn't turn on a note that's being held on the Tempera right now, and only sends a
  Note Off for a note it turned on itself. Without this, overdubbing a held note would double it at the next frame, and
  playback could cut a note being played live.
- **File format 2** stores notes as Note On / Note Off events in each frame's events, so DAW playback sounds too.
  Older versions of the app ignore them.
- The output scheduler keeps track of the notes it has turned on and releases them when the app quits.

## Step 3: Hold (built)

- A **Hold** switch on the Tempera surface holds one note (middle C, note 60, velocity 100, on the app's MIDI channel) until it's
  switched off, so emitter patterns sound without touching the Tempera: drawing, transforms and playback of
  recordings without notes. It moves with the MIDI channel setting and isn't remembered between runs.
- If a recording plays the same note on the same channel, its Note Off also ends the held note.

## The Tempera surface (built)

A fourth surface on the main view's button row: **Canvas −**, **Canvas +** (hold to scroll) and **Hold**. The middle of
the display's status line shows the canvas and *Hold* while they're set.

## Tested (simulated, fake output)

- Notes play at their ticks; starting at a frame inside a held note turns it on; pause releases and resume restores;
  looping is right on every pass; stopping releases everything.
- Recording: notes held before arming and played while armed start at the trigger; notes land at their ticks; notes
  still held at the end are ended there; Replace sends nothing; Overdub doesn't double live notes and plays them back on
  the next pass.
- Canvas: three quick steps send one Program Change once they stop; a loaded file's canvas is sent at once; files keep
  the canvas and notes exactly.
- Hold follows the MIDI channel; quitting (the clock switching off) releases held notes. Shift moves emitter events and
  leaves notes; Random keeps notes.

## Step 4: parameters (not built)

- Live controls on a surface for a few parameters (per-colour emitter volume, filter cutoff, reverb and delay mix),
  and a **parameter set saved with each file** and sent on load, alongside the canvas.
- Later, per-frame parameter events (automation), played like note events. Since the Tempera can't report values,
  the first value sent jumps the parameter, and recordings can't capture knob moves made on the Tempera.
- Per-emitter parameters fit the app's four colours, so transforms could one day act on them too.

## Hardware checks (add to the hardware test)

- With *Send MIDI notes* on, do Overlay Keyboard notes arrive with velocity, and does *Hold* send a single Note On (and
  a Note Off on *Clear*)?
- Does the Tempera send anything for notes it receives from the app (an echo)?
- Does Program Change load the expected canvas, and how long is the gap in sound while it loads?
