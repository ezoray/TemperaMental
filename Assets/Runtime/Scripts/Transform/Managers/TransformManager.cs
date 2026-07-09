using System;
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
        const float SecondsInMinute = 60f;

        [Header("Order: Shift, Random, Flip, Rotate, Swap")]
        [SerializeField] List<TransformBaseService> transformServices;
        [SerializeField] FrameManager frameManager;

        RandomTransformService randomTransformService;
        ShiftTransformService shiftTransformService;

        int bpm;
        float masterTickRate;
        float masterTickTime;

        TransformType currentTransformType;

        ulong[] originalGroups;
        ulong[] transformGroups;

        PlaybackState playbackState;

        [SerializeField] UnityEvent<TransformType, TransformDetail> onTransformChanged;
        [SerializeField] UnityEvent<ulong[]> onEmittersTransformed;
        [SerializeField] UnityEvent<TransformDirections, bool> onDirectionLatchStateChanged;
        [SerializeField] UnityEvent<TransformActiveEmitters> onTransformsReset;
        [SerializeField] UnityEvent<int> onTransformRateChanged;
        [SerializeField] UnityEvent<TransformType, TransformMode> onTransformModeChanged;

        private void Awake()
        {
            randomTransformService = (RandomTransformService)transformServices[(int)TransformType.Random];
            shiftTransformService = (ShiftTransformService)transformServices[(int)TransformType.Shift];
            currentTransformType = TransformType.Shift;

            originalGroups = new ulong[ConfigRegistry.Grid.EmitterCount];
            transformGroups = new ulong[ConfigRegistry.Grid.EmitterCount];
        }

        private void OnEnable()
        {
            foreach (var transformService in transformServices)
            {
                transformService.OnDirectionLatchStateChanged += ActionOnServiceDirectionLatchChanged;
                transformService.OnEmittersTransformed += ActionOnEmittersTransformed;
            }
        }

        private void Start()
        {
            // fire initial state to subscribers (UI)
            TransformDetail transformDetail = transformServices[(int)currentTransformType].GetTransformDetail();
            onTransformChanged?.Invoke(currentTransformType, transformDetail);
            onTransformRateChanged?.Invoke(transformDetail.Rate);
        }

        private void Update()
        {
            if (Time.time >= masterTickTime)
            {
                masterTickTime = Time.time + masterTickRate;

                if (playbackState == PlaybackState.Stopped || playbackState == PlaybackState.Reset)
                {
                    originalGroups = frameManager.GetCurrentFrameEmitters();
                    Array.Copy(originalGroups, transformGroups, originalGroups.Length);

                    foreach (var transformService in transformServices)
                    {
                        if (!transformService.IsLatched || !transformService.TickAndCheck()) continue;
                        ulong[] resultGroups = transformService.DoTransform(transformGroups);
                        Array.Copy(resultGroups, transformGroups, transformGroups.Length);
                    }

                    if (EmitterUtils.CheckGroupsDifferent(originalGroups, transformGroups))
                    {
                        onEmittersTransformed?.Invoke(transformGroups);
                    }
                }
            }
        }

        public void HandleDirectionChange(ulong[] emitterGroup, int directionValue)
        {
            transformServices[(int)currentTransformType].HandleDirectionChange(emitterGroup, directionValue);
        }

        public void ToggleWrapping()
        {
            shiftTransformService.ToggleWrap();
        }

        public void ToggleLatching()
        {
            transformServices[(int)currentTransformType].ToggleLatch();            
        }

        public void SetTransformRate(int rate)
        {
            transformServices[(int)currentTransformType].SetTransformRate(rate);
            onTransformRateChanged?.Invoke(rate);
        }

        public void SetBpm(int newBpm)
        {
            bpm = newBpm;
            masterTickRate = SecondsInMinute / bpm;
            masterTickTime = Mathf.Min(masterTickTime, Time.time + masterTickRate);
        }

        public void RandomiseEmitters(ulong[] emitterGroup, int targetCount)
        {
            ulong[] transformedGroups = randomTransformService.DoRandomTransform(emitterGroup, targetCount, randomTransformService.TransformMode);
            onEmittersTransformed?.Invoke(transformedGroups);
        }

        public void SelectEmitter(int emitterId)
        {
            transformServices[(int)currentTransformType].SelectEmitter(emitterId);
        }

        public void ChangeTransformMode(TransformType transformType)
        {
            TransformMode transformMode = transformServices[(int)transformType].ChangeTransformMode();
            onTransformModeChanged?.Invoke(transformType, transformMode);

            if (transformType == currentTransformType)
            {
                TransformDetail transformDetail = transformServices[(int)transformType].GetTransformDetail();
                onTransformChanged?.Invoke(transformType, transformDetail);
                onTransformRateChanged?.Invoke(transformDetail.Rate);
            }
        }

        public void SetTransformType(TransformType transformType)
        {
            if (transformType != currentTransformType)
            {
                currentTransformType = transformType;

                TransformDetail transformDetail = transformServices[(int)transformType].GetTransformDetail();
                onTransformChanged?.Invoke(transformType, transformDetail);
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
                transformService.ResetTransform();
            }

            LogMan.Log("Transforms Reset");

            onTransformsReset?.Invoke(transformServices[(int)currentTransformType].GetActiveEmitters());
            onTransformRateChanged?.Invoke(ConfigRegistry.Transform.DefaultRate);
        }

        public void UnlatchTransforms()
        {
            foreach (var transformService in transformServices)
            {
                transformService.ClearLatch();
            }
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