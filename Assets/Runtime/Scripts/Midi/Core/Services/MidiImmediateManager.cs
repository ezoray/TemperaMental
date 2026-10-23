using TemperaMental.Core;
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

        public void AddEmitter(EmitterDetail emitterDetail)
        {
            outputScheduler.EnqueueAddEmitter(emitterDetail.EmitterId, EmitterUtils.PositionToIndex(emitterDetail.Position));
        }

        public void RemoveEmitter(EmitterDetail emitterDetail)
        {
            outputScheduler.EnqueueRemoveEmitter(EmitterUtils.PositionToIndex(emitterDetail.Position));
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
