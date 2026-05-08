using TemperaMental.Core;
using TemperaMental.Midi.Playbacks;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiImmediateService : MonoBehaviour
    {
        [SerializeField] PlaybackManager playbackManager;

        PlaybackState playbackState;
        ulong[] pendingGroups;
        bool hasPending;


        private void Update()
        {
            if (hasPending && !playbackManager.IsFramePlaybackActive)
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
        }

        public void SendFrame(ulong[] emitterGroups)
        {
            if (playbackState == PlaybackState.Playing) return;

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