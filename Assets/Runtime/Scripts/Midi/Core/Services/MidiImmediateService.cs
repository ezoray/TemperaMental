using TemperaMental.Core;
using TemperaMental.Midi.Playbacks;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiImmediateService : MonoBehaviour
    {
        [SerializeField] FrameMidiPlayer midiPlayer;

        PlaybackState playbackState;
        ulong[] pendingGroups;
        bool hasPending;


        private void Update()
        {
            if (hasPending && !midiPlayer.IsFramePlaybackActive)
            {
                hasPending = false;
                midiPlayer.PlayFrame(pendingGroups);
                pendingGroups = null;
            }
        }

        public void AddEmitter(EmitterDetail emitterDetail)
        {
    //        if (playbackState == PlaybackState.Playing) return;
            midiPlayer.AddEmitter(emitterDetail);
        }

        public void RemoveEmitter(Vector2Int position)
        {
   //         if (playbackState == PlaybackState.Playing) return;

            midiPlayer.RemoveEmitter(position);
        }

        public void SetEmitterType(int emitterId)
        {
            midiPlayer.SetEmitterType(emitterId);
        }


        public void SetPlaybackState(PlaybackState state)
        {
            playbackState = state;
        }

        public void SendFrame(ulong[] emitterGroups)
        {
            if (playbackState == PlaybackState.Playing) return;

            if (!midiPlayer.PlayFrame(emitterGroups))
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