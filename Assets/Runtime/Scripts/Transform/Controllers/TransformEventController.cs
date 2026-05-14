using System;
using System.Collections.Generic;
using TemperaMental.Core;
using TemperaMental.Frames;
using UnityEngine;

namespace TemperaMental.Transforms
{
    public class TransformEventController : MonoBehaviour
    {
        [SerializeField] TransformManager transformManager;
        [SerializeField] FrameManager frameManager;

        public void OnClickDirection(int directionValue)
        {
            ulong[] emitterGroup = frameManager.GetCurrentFrameEmitters();
            transformManager.HandleDirectionChange(emitterGroup, directionValue);
        }

        public void OnClickToggleLatch() => transformManager.ToggleTransformLatch();

        public void OnClickToggleWrapping() => transformManager.ToggleWrapping();

        public void ActionOnFramesLoaded(List<Frame> frames, bool isAppend)
        {
            transformManager.StopTransforms();
        }

        // slider
        public void ActionOnRandomValueChanged(float randomValue)
        {
            ulong[] emitterGroup = frameManager.GetCurrentFrameEmitters();

            transformManager.RandomiseEmitters(emitterGroup, Mathf.RoundToInt(randomValue));
        }

        public void ActionOnPlaybackStateChanged(PlaybackState playbackState) => transformManager.SetPlaybackState(playbackState);

        public void ActionOnBpmChanged(int newBpm)
        {
            transformManager.SetBpm(newBpm);
        }

        public void OnClickToggleEmitter(int emitterId)
        {
            transformManager.ToggleEmitter(emitterId);
        }

        public void OnClickSetTransformMode(int transformMode)
        {
            if (Enum.IsDefined(typeof(TransformMode), transformMode))
            {
                transformManager.SetTransformMode((TransformMode)transformMode);
            }
        }
    }
}
