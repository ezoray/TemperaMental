using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Transforms
{
    public class TransformManager : MonoBehaviour
    {
        // transforms are called in this order so place shift first to prevent it interfering with other transforms
        [Header("Order: Shift, Random, Flip, Rotate, Swap")]
        [SerializeField] List<TransformBaseService> transformServices;
        [SerializeField] FrameManager frameManager;

        RandomTransformService randomTransformService;
        ShiftTransformService shiftTransformService;

        TransformMode transformMode;

        ulong[] originalGroups;
        ulong[] transformGroups;

        int bpm;
        float nextEventTime;
        float repeatRate;

        PlaybackState playbackState;

        [SerializeField] UnityEvent<TransformMode, TransformDetail> onTransformModeChanged;
        [SerializeField] UnityEvent<int, bool> onTransformEmitterChanged;
        [SerializeField] UnityEvent<ulong[]> onEmittersTransformed;

        [SerializeField] UnityEvent<bool> onLatchStateChanged;
        [SerializeField] UnityEvent<bool> onWrapStateChanged;
        [SerializeField] UnityEvent<TransformDirections, bool> onDirectionLatchStateChanged;
        [SerializeField] UnityEvent onTransformsReset;


        private void Awake()
        {
            randomTransformService = (RandomTransformService)transformServices[(int)TransformMode.Random];
            shiftTransformService = (ShiftTransformService)transformServices[(int)TransformMode.Shift];
            transformMode = TransformMode.Shift;

            originalGroups = new ulong[ConfigRegistry.Grid.MaxEmitters];
            transformGroups = new ulong[ConfigRegistry.Grid.MaxEmitters];
        }

        private void Start()
        {
            // fire initial state to subscribers (UI)
            TransformDetail detail = transformServices[(int)transformMode].GetTransformDetail();
            onTransformModeChanged?.Invoke(transformMode, detail);
        }

        private void OnEnable()
        {
            foreach (var transformService in transformServices)
            {
                transformService.OnDirectionLatchStateChanged += ActionOnServiceDirectionLatchChanged;
                transformService.OnEmittersTransformed += ActionOnEmittersTransformed;
            }
        }

        void Update()
        {
            if (Time.time >= nextEventTime)
            {
                if (playbackState == PlaybackState.Stopped || playbackState == PlaybackState.Reset)
                {
                    bool anyLatched = false;
                    foreach (var transformService in transformServices)
                        if (transformService.IsLatched) { anyLatched = true; break; }

                    if (anyLatched)
                    {
                        originalGroups = frameManager.GetCurrentFrameEmitters();
                        transformGroups = frameManager.GetCurrentFrameEmitters();

                        foreach (var transformService in transformServices)
                        {
                            if (transformService.IsLatched)
                                transformGroups = transformService.DoTransform(transformGroups);
                        }

                        if (EmitterUtils.CheckGroupsDifferent(originalGroups, transformGroups))
                        {
                            onEmittersTransformed?.Invoke(transformGroups);
                        }
                    }

                    nextEventTime = Time.time + repeatRate;
                }
            }
        }

        public void HandleDirectionChange(ulong[] emitterGroup, int directionValue)
        {
            transformServices[(int)transformMode].HandleDirectionChange(emitterGroup, directionValue);
        }

        public void ToggleWrapping()
        {
            bool isWrapping = shiftTransformService.ToggleWrap();

            onWrapStateChanged?.Invoke(isWrapping);
        }

        public void ToggleTransformLatch()
        {
            bool isLatched = transformServices[(int)transformMode].ToggleLatch();

            onLatchStateChanged?.Invoke(isLatched);
        }       

        public void SetBpm(int newBpm)
        {
            bpm = newBpm;
            repeatRate = 60f / bpm;
            nextEventTime = Mathf.Min(nextEventTime, Time.time + repeatRate);
        }

        public void RandomiseEmitters(ulong[] emitterGroup, int targetCount)
        {
            ulong[] transformedGroups = randomTransformService.DoRandomTransform(emitterGroup, targetCount);
            onEmittersTransformed?.Invoke(transformedGroups);
        }

        public void ToggleEmitter(int emitterId)
        {
            bool isActive = transformServices[(int)transformMode].ToggleEmitter(emitterId);

            onTransformEmitterChanged?.Invoke(emitterId, isActive);
        }

        public void SetTransformMode(TransformMode transformMode)
        {
            if (transformMode != this.transformMode)
            {
                this.transformMode = transformMode;

                TransformDetail transformDetail = transformServices[(int)transformMode].GetTransformDetail();

                onTransformModeChanged?.Invoke(transformMode, transformDetail);
            }
        }

        public void SetPlaybackState(PlaybackState newPlaybackState)
        {
            playbackState = newPlaybackState;

            if (playbackState == PlaybackState.Playing)
            {
                UnlatchTransforms();
            }
        }

        public void ResetTransforms()
        {
            foreach (var transformService in transformServices)
            {
                transformService.Reset();
            }

            LogMan.Log("Transforms Reset");

            onTransformsReset?.Invoke();
        }

        public void UnlatchTransforms()
        {
            foreach (var transformService in transformServices)
            {
                transformService.ClearLatch();
            }

            onLatchStateChanged?.Invoke(false);
        }

        private void ActionOnEmittersTransformed(ulong[] transformedGroups)
        {
            originalGroups = frameManager.GetCurrentFrameEmitters();

            if (EmitterUtils.CheckGroupsDifferent(originalGroups, transformedGroups))
            {
                onEmittersTransformed?.Invoke(transformedGroups);
            }
        }

        private void ActionOnServiceDirectionLatchChanged(TransformDirections directions, bool state)
        {
            onDirectionLatchStateChanged?.Invoke(directions, state);
        }

        private void OnDisable()
        {
            foreach (var transformService in transformServices)
            {
                transformService.OnDirectionLatchStateChanged -= ActionOnServiceDirectionLatchChanged;
                transformService.OnEmittersTransformed -= ActionOnEmittersTransformed;
            }
        }
    }
}
