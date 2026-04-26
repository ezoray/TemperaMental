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

        public void OnClickDirection(int directionId)
        {
            ulong[] emitterGroup = frameManager.GetCurrentFrameEmitters();
            transformManager.DoTransform(emitterGroup, directionId);
        }

        public void OnClickToggleLatch() => transformManager.ToggleLatch();

        public void OnClickToggleWrapping() => transformManager.ToggleWrapping();

        // slider
        public void ActionOnRandomValueChanged(float randomValue)
        {
            ulong[] emitterGroup = frameManager.GetCurrentFrameEmitters();

            transformManager.RandomiseEmitters(emitterGroup, Mathf.RoundToInt(randomValue));
        }

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
