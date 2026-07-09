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

        public void OnClickChangeTransformMode(int transformType)
        {
            transformManager.ChangeTransformMode((TransformType)transformType);
        }

        public void OnReset() => transformManager.ResetTransforms();

        public void OnClickDirection(int directionValue)
        {
            ulong[] emitterGroup = frameManager.GetCurrentFrameEmitters();
            transformManager.HandleDirectionChange(emitterGroup, directionValue);
        }

        public void OnClickToggleLatching() => transformManager.ToggleLatching();

        public void OnClickToggleWrapping() => transformManager.ToggleWrapping();

        public void ActionOnFramesLoaded(List<Frame> frames, bool isAppend) => transformManager.UnlatchTransforms();

        // transform rate slider after conversion by UI manager
        public void ActionOnTransformRateChanged(float rate) => transformManager.SetTransformRate((int)rate);

        // random slider
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

        public void OnClickSelectEmitter(int emitterId)
        {
            transformManager.SelectEmitter(emitterId);
        }

        public void OnClickSetTransformType(int transformType)
        {
            if (Enum.IsDefined(typeof(TransformType), transformType))
            {
                transformManager.SetTransformType((TransformType)transformType);
            }
        }
    }
}
