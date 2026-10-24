using TemperaMental.Applications.Config;
using TemperaMental.Midi.Core;
using TemperaMental.Settings;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Input
{
    // Turns notes on the Tempera's Control MIDI channel (Settings → Control MIDI channel → Out on the Tempera) into
    // emitter actions: 96–99 select a colour, 0–91 Note On places the selected colour, Note Off removes, 100–107 arm
    // a track. The selected colour is remembered, since a placement note doesn't carry it.
    // Runs on the main thread from the In device's messages; other channels are ignored.
    public class TemperaControlDecoder : MonoBehaviour
    {
        [SerializeField] MidiInputRouter inputRouter;

        [SerializeField] UnityEvent<TemperaControlEvent> onControlEvent = new UnityEvent<TemperaControlEvent>();

        int selectFirstNote;
        int armFirstNote;
        int emitterCount;
        int trackCount;

        // 0–15
        int controlChannel;
        int selectedEmitter;

        private void Awake()
        {
            selectFirstNote = ConfigRegistry.Midi.ControlSelectFirstNote;
            armFirstNote = ConfigRegistry.Midi.ControlArmFirstNote;
            emitterCount = ConfigRegistry.Grid.EmitterCount;
            trackCount = ConfigRegistry.Grid.GridWidth;

            controlChannel = ConfigRegistry.Midi.DefaultControlChannel - 1;
            selectedEmitter = ConfigRegistry.Grid.DefaultEmitterId;
        }

        private void OnEnable()
        {
            inputRouter.MessageReceivedOnMainThread += Decode;
        }

        // channel 1–16
        public void SetControlChannel(int channel)
        {
            controlChannel = channel - 1;
        }

        public int SelectedEmitter => selectedEmitter;

        public void Decode(MidiMessageData message)
        {
            if (message.Kind != MidiMessageKind.NoteOn && message.Kind != MidiMessageKind.NoteOff) return;
            if (message.Channel != controlChannel) return;

            int note = message.Data1;
            bool isNoteOn = message.Kind == MidiMessageKind.NoteOn;

            if (note >= selectFirstNote && note < selectFirstNote + emitterCount)
            {
                if (!isNoteOn) return;

                selectedEmitter = note - selectFirstNote;
                Raise(TemperaControlKind.SelectEmitter, -1, 0, message.Timestamp);
                return;
            }

            if (note >= armFirstNote && note < armFirstNote + trackCount)
            {
                if (isNoteOn) Raise(TemperaControlKind.ArmTrack, -1, note - armFirstNote + 1, message.Timestamp);
                return;
            }

            if (EmitterUtils.TryControlNoteToIndex(note, out int cell))
            {
                if (isNoteOn)
                {
                    // the Tempera gives a 2-Lane emitter its pair of tracks whatever colour is selected; the app's
                    // 2-Lane settings mirror the Tempera's, so resolve the colour the same way
                    int laneOwner = EmitterUtils.GetLaneOwner(cell, EmitterSettingsManager.CurrentTwoLanes);
                    int placedEmitter = laneOwner != -1 ? laneOwner : selectedEmitter;

                    Raise(TemperaControlKind.PlaceEmitter, placedEmitter, cell, 0, message.Timestamp);
                }
                else
                {
                    Raise(TemperaControlKind.RemoveEmitter, selectedEmitter, cell, 0, message.Timestamp);
                }
            }
        }

        private void Raise(TemperaControlKind kind, int cell, int track, long timestamp)
        {
            Raise(kind, selectedEmitter, cell, track, timestamp);
        }

        private void Raise(TemperaControlKind kind, int emitterId, int cell, int track, long timestamp)
        {
            onControlEvent?.Invoke(new TemperaControlEvent(kind, emitterId, cell, track, timestamp));
        }

        private void OnDisable()
        {
            inputRouter.MessageReceivedOnMainThread -= Decode;
        }
    }
}
