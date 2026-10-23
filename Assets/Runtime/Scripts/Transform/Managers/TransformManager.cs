using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Midi.Clock;
using TemperaMental.Midi.Output;
using TemperaMental.Midi.Playbacks;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Transforms
{
    // Latched transforms run on the clock thread at each beat (while the sequencer isn't playing), so their
    // output is sent exactly on the beat. Each result is also queued for the main thread, which applies it to the
    // current frame (or records it) in Update.
    // The transform services are shared between the clock thread and the main thread (button presses), so every
    // use of them is guarded by transformLock. UnityEvents are raised on the main thread only.
    public class TransformManager : MonoBehaviour
    {
        [Header("Order: Shift, Random, Flip, Rotate, Swap")]
        [SerializeField] List<TransformBaseService> transformServices;
        [SerializeField] FrameManager frameManager;
        [SerializeField] ClockEngine clockEngine;
        [SerializeField] MidiOutputScheduler outputScheduler;
        [SerializeField] FrameSequenceManager sequenceManager;

        RandomTransformService randomTransformService;
        ShiftTransformService shiftTransformService;

        readonly object transformLock = new object();

        TransformType currentTransformType;

        // clock thread
        ulong[] beatGroups;
        ulong[] lastSeenPublished;
        ulong[] lastBeatOutput;

        // results of latched transforms, from the clock thread to the main thread
        readonly ConcurrentQueue<ulong[]> beatResults = new ConcurrentQueue<ulong[]>();

        // result of an immediate (button press) transform, raised once the lock is released
        ulong[] immediateResult;

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

            beatGroups = new ulong[ConfigRegistry.Grid.EmitterCount];
        }

        private void OnEnable()
        {
            foreach (var transformService in transformServices)
            {
                transformService.OnDirectionLatchStateChanged += ActionOnServiceDirectionLatchChanged;
                transformService.OnEmittersTransformed += ActionOnEmittersTransformed;
            }

            clockEngine.OnBeat += ActionOnClockBeat;
        }

        private void Start()
        {
            // fire initial state to subscribers (UI)
            TransformDetail transformDetail = GetCurrentTransformDetail();
            onTransformChanged?.Invoke(currentTransformType, transformDetail);
            onTransformRateChanged?.Invoke(transformDetail.Rate);
        }

        private void Update()
        {
            // apply every result in order: when recording, each one becomes a new frame
            while (beatResults.TryDequeue(out ulong[] result))
            {
                onEmittersTransformed?.Invoke(result);
            }
        }

        // clock thread
        private void ActionOnClockBeat(ClockBeat beat)
        {
            PlaybackState state = sequenceManager.State;

            if (state != PlaybackState.Stopped && state != PlaybackState.Reset)
            {
                lastSeenPublished = null;
                lastBeatOutput = null;
                return;
            }

            ulong[] published = frameManager.PublishedCurrentGroups;

            if (published == null) return;

            // if the main thread hasn't published anything since the last beat (it may not have applied our last
            // result yet), carry on from that result so a slow frame doesn't repeat the same step
            ulong[] input = published == lastSeenPublished && lastBeatOutput != null ? lastBeatOutput : published;
            lastSeenPublished = published;

            lock (transformLock)
            {
                Array.Copy(input, beatGroups, beatGroups.Length);

                foreach (var transformService in transformServices)
                {
                    if (!transformService.IsLatched || !transformService.TickAndCheck()) continue;

                    ulong[] resultGroups = transformService.DoTransform(beatGroups);
                    Array.Copy(resultGroups, beatGroups, beatGroups.Length);
                }
            }

            if (!EmitterUtils.CheckGroupsDifferent(input, beatGroups))
            {
                lastBeatOutput = input;
                return;
            }

            ulong[] output = (ulong[])beatGroups.Clone();
            lastBeatOutput = output;

            outputScheduler.EnqueueFrame(output);
            beatResults.Enqueue(output);
        }

        public void HandleDirectionChange(ulong[] emitterGroup, int directionValue)
        {
            lock (transformLock)
            {
                transformServices[(int)currentTransformType].HandleDirectionChange(emitterGroup, directionValue);
            }

            RaiseImmediateResult();
        }

        public void ToggleWrapping()
        {
            lock (transformLock)
            {
                shiftTransformService.ToggleWrap();
            }
        }

        public void ToggleLatching()
        {
            lock (transformLock)
            {
                transformServices[(int)currentTransformType].ToggleLatch();
            }
        }

        public void SetTransformRate(int rate)
        {
            lock (transformLock)
            {
                transformServices[(int)currentTransformType].SetTransformRate(rate);
            }

            onTransformRateChanged?.Invoke(rate);
        }

        public void RandomiseEmitters(ulong[] emitterGroup, int targetCount)
        {
            ulong[] transformedGroups;

            lock (transformLock)
            {
                transformedGroups = (ulong[])randomTransformService
                    .DoRandomTransform(emitterGroup, targetCount, randomTransformService.TransformMode).Clone();
            }

            onEmittersTransformed?.Invoke(transformedGroups);
        }

        public void SelectEmitter(int emitterId)
        {
            lock (transformLock)
            {
                transformServices[(int)currentTransformType].SelectEmitter(emitterId);
            }
        }

        public void ChangeTransformMode(TransformType transformType)
        {
            TransformMode transformMode;

            lock (transformLock)
            {
                transformMode = transformServices[(int)transformType].ChangeTransformMode();
            }

            onTransformModeChanged?.Invoke(transformType, transformMode);

            if (transformType == currentTransformType)
            {
                TransformDetail transformDetail = GetCurrentTransformDetail();
                onTransformChanged?.Invoke(transformType, transformDetail);
                onTransformRateChanged?.Invoke(transformDetail.Rate);
            }
        }

        public void SetTransformType(TransformType transformType)
        {
            if (transformType != currentTransformType)
            {
                currentTransformType = transformType;

                TransformDetail transformDetail = GetCurrentTransformDetail();
                onTransformChanged?.Invoke(transformType, transformDetail);
                onTransformRateChanged?.Invoke(transformDetail.Rate);
            }
        }

        public void SetPlaybackState(PlaybackState newPlaybackState)
        {
            if (newPlaybackState == PlaybackState.Playing)
            {
                UnlatchTransforms();
            }
        }

        public void ResetTransforms()
        {
            TransformActiveEmitters activeEmitters;

            lock (transformLock)
            {
                foreach (var transformService in transformServices)
                {
                    transformService.ResetTransform();
                }

                activeEmitters = transformServices[(int)currentTransformType].GetActiveEmitters();
            }

            LogMan.Log("Transforms Reset");

            onTransformsReset?.Invoke(activeEmitters);
            onTransformRateChanged?.Invoke(ConfigRegistry.Transform.DefaultRate);
        }

        public void UnlatchTransforms()
        {
            lock (transformLock)
            {
                foreach (var transformService in transformServices)
                {
                    transformService.ClearLatch();
                }
            }
        }

        private TransformDetail GetCurrentTransformDetail()
        {
            lock (transformLock)
            {
                return transformServices[(int)currentTransformType].GetTransformDetail();
            }
        }

        // raised by a service during HandleDirectionChange, inside the lock; kept until the lock is released
        private void ActionOnEmittersTransformed(ulong[] transformedGroups)
        {
            immediateResult = (ulong[])transformedGroups.Clone();
        }

        private void RaiseImmediateResult()
        {
            ulong[] result = immediateResult;
            immediateResult = null;

            if (result == null) return;

            if (EmitterUtils.CheckGroupsDifferent(frameManager.GetCurrentFrameEmitters(), result))
            {
                onEmittersTransformed?.Invoke(result);
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

            clockEngine.OnBeat -= ActionOnClockBeat;
        }
    }
}
