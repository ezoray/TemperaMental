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

        int bpm;
        float masterTickRate;
        float masterTickTime;
        int masterTickCount;

        TransformMode transformMode;

        ulong[] originalGroups;
        ulong[] transformGroups;

        PlaybackState playbackState;

        [SerializeField] UnityEvent<TransformMode, TransformDetail> onTransformModeChanged;
        [SerializeField] UnityEvent<int, bool> onTransformEmitterChanged;
        [SerializeField] UnityEvent<ulong[]> onEmittersTransformed;

        [SerializeField] UnityEvent<bool> onLatchStateChanged;
        [SerializeField] UnityEvent<bool> onWrapStateChanged;
        [SerializeField] UnityEvent<TransformDirections, bool> onDirectionLatchStateChanged;
        [SerializeField] UnityEvent onTransformsReset;
        [SerializeField] UnityEvent<float> onTransformRateChanged;


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
            TransformDetail transformDetail = transformServices[(int)transformMode].GetTransformDetail();
            onTransformModeChanged?.Invoke(transformMode, transformDetail);
            onTransformRateChanged?.Invoke(transformDetail.Rate);
        }

        private void OnEnable()
        {
            foreach (var transformService in transformServices)
            {
                transformService.OnDirectionLatchStateChanged += ActionOnServiceDirectionLatchChanged;
                transformService.OnEmittersTransformed += ActionOnEmittersTransformed;
            }
        }

        private void Update()
        {
            if (Time.time >= masterTickTime)
            {
                masterTickTime = Time.time + masterTickRate;
                masterTickCount++;

                if (playbackState == PlaybackState.Stopped || playbackState == PlaybackState.Reset)
                { 
                    foreach (var transformService in transformServices)
                    {
                        if (!transformService.IsLatched || !transformService.TickAndCheck()) continue;

                        originalGroups = frameManager.GetCurrentFrameEmitters();
                        transformGroups = frameManager.GetCurrentFrameEmitters();

                        transformGroups = transformService.DoTransform(transformGroups);

                        if (EmitterUtils.CheckGroupsDifferent(originalGroups, transformGroups))
                        {
                            onEmittersTransformed?.Invoke(transformGroups);
                        }
                    }
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
            bool isLatched = transformServices[(int)transformMode].ToggleLatch(masterTickCount);
            onLatchStateChanged?.Invoke(isLatched);
        }

        public void SetTransformRate(float rate)
        {
            transformServices[(int)transformMode].SetTransformRate(rate, masterTickCount);

            onTransformRateChanged?.Invoke(rate);
        }

        public void SetBpm(int newBpm)
        {
            bpm = newBpm;
            masterTickRate = (60f / bpm) / 10f;
            masterTickTime = Mathf.Min(masterTickTime, Time.time + masterTickRate);
            foreach (var transformService in transformServices)
                transformService.RecalculateTicksPerFire();
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

                onTransformRateChanged?.Invoke(transformDetail.Rate);
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
                transformService.ResetTransform(masterTickCount);
            }

            LogMan.Log("Transforms Reset");

            onTransformsReset?.Invoke();
            onTransformRateChanged?.Invoke(1f);
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
