using TemperaMental.Core;
using TemperaMental.Midi.Input;
using TemperaMental.Midi.Output;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace TemperaMental.Midi.Core
{
    // sends edits made outside the sequencer (drawing, frame changes, colour selection) to the output scheduler
    public class MidiImmediateManager : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("playbackManager")] MidiOutputScheduler outputScheduler;

        volatile PlaybackState playbackState;

        private bool SequencerRunning => playbackState == PlaybackState.Playing;

        // changes that came from the Tempera are already on it, so they're never sent back (loop protection)
        public void AddEmitter(EmitterDetail emitterDetail)
        {
            if (emitterDetail.IsFromTempera) return;

            outputScheduler.EnqueueAddEmitter(emitterDetail.EmitterId, EmitterUtils.PositionToIndex(emitterDetail.Position));
        }

        public void RemoveEmitter(EmitterDetail emitterDetail)
        {
            if (emitterDetail.IsFromTempera) return;

            outputScheduler.EnqueueRemoveEmitter(EmitterUtils.PositionToIndex(emitterDetail.Position));
        }

        // whatever is done on the Tempera changes what it shows, followed or not, so the record used for frame diffs is
        // kept up to date. Call this before the change is applied to the frame, so a colour selected on the Tempera
        // isn't sent back to it
        public void TrackTemperaChange(TemperaControlKind kind, int emitterId, int cell)
        {
            switch (kind)
            {
                case TemperaControlKind.SelectEmitter:
                    outputScheduler.NoteDeviceSelected(emitterId);
                    break;

                case TemperaControlKind.PlaceEmitter:
                    outputScheduler.NoteDevicePlaced(emitterId, cell);
                    break;

                case TemperaControlKind.RemoveEmitter:
                    outputScheduler.NoteDeviceRemoved(cell);
                    break;
            }
        }

        public void SetEmitterType(int emitterId)
        {
            outputScheduler.EnqueueSelectEmitter(emitterId);
        }

        // called from the sequencer, possibly on the clock thread
        public void SetPlaybackState(PlaybackState state)
        {
            playbackState = state;
        }

        public void SendFrame(ulong[] emitterGroups)
        {
            // the sequencer owns the output while it plays
            if (SequencerRunning) return;

            outputScheduler.EnqueueFrame(emitterGroups);
        }
    }
}
