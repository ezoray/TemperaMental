using System;
using TemperaMental.Core;
using TemperaMental.Frames;
using UnityEngine;

namespace TemperaMental.Emitters
{
    public class EmitterTransformEventController : MonoBehaviour
    {
        [SerializeField] EmitterTransformManager transformManager;
        [SerializeField] FrameManager frameManager;

        public void OnClickDirection(int directionValue)
        {
            ulong[] emitterGroup = frameManager.GetCurrentFrameEmitters();
            transformManager.HandleDirectionChange(emitterGroup, directionValue);
        }

        public void OnClickToggleLatch() => transformManager.ToggleTransformLatch();

        public void OnClickToggleWrapping() => transformManager.ToggleWrapping();

        // slider
        public void ActionOnRandomValueChanged(float randomValue)
        {
            ulong[] emitterGroup = frameManager.GetCurrentFrameEmitters();

            transformManager.RandomiseEmitters(emitterGroup, Mathf.RoundToInt(randomValue));
        }

        public void ActionOnPlaybackStateChanged(PlaybackState playbackState) => transformManager.SetPlaybackState(playbackState);

        public void ActionOnBpmChanged(int newBpm)
        {
            transformManager.ActionOnBpmChanged(newBpm);
        }

        public void OnClickToggleEmitter(int emitterId)
        {
            transformManager.ToggleEmitter(emitterId);
        }

        public void OnClickSetTransformMode(int transformMode)
        {
            if (Enum.IsDefined(typeof(EmitterTransformMode), transformMode))
            {
                transformManager.SetTransformMode((EmitterTransformMode)transformMode);
            }
        }
    }
}
