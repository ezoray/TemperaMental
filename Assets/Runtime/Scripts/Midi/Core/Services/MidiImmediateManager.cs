using TemperaMental.Core;
using TemperaMental.Logs;
using TemperaMental.Midi.Playbacks;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiImmediateManager : MonoBehaviour
    {
        [SerializeField] PlaybackManager playbackManager;

        volatile PlaybackState playbackState;
        ulong[] pendingGroups;
        bool hasPending;

        private bool SequencerRunning => playbackState == PlaybackState.Playing;

        private void Update()
        {
            if (hasPending && !playbackManager.isPlaybackActive && !SequencerRunning)
            {
                hasPending = false;
                playbackManager.PlayFrame(pendingGroups);
                pendingGroups = null;
            }
        }
         
        public void AddEmitter(EmitterDetail emitterDetail)
        {
            playbackManager.AddEmitter(emitterDetail);
        }

        public void RemoveEmitter(Vector2Int position)
        {
            playbackManager.RemoveEmitter(position);
        }

        public void SetEmitterType(int emitterId)
        {
            playbackManager.SetEmitterType(emitterId);
        }


        public void SetPlaybackState(PlaybackState state)
        {
            playbackState = state;
            if (SequencerRunning)
            {
                hasPending = false;
                pendingGroups = null;
            }
        }

        public void SendFrame(ulong[] emitterGroups)
        {
            if (SequencerRunning) return;

            if (!playbackManager.PlayFrame(emitterGroups))
            {
                pendingGroups = emitterGroups;
                hasPending = true;
            }
            else
            {
                hasPending = false;
            }
        }
    }
}