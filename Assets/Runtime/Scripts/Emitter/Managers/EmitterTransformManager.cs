using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Frames;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Emitters
{
    public class EmitterTransformManager : MonoBehaviour
    {
        [Header("Order: Random, Flip, Rotate, Swap, Shift")]
        [SerializeField] List<TransformBaseService> transformServices;
        [SerializeField] FrameManager frameManager;

        RandomTransformService randomTransformService;
        ShiftTransformService shiftTransformService;

        EmitterTransformMode transformMode;

        int bpm;
        float nextEventTime;
        float repeatRate;

        PlaybackState playbackState;

        [SerializeField] UnityEvent<EmitterTransformMode, EmitterTransformDetail> onTransformModeChanged;
        [SerializeField] UnityEvent<int, bool> onTransformEmitterChanged;
        [SerializeField] UnityEvent<ulong[]> onEmittersTransformed;

        [SerializeField] UnityEvent<bool> onLatchStateChanged;
        [SerializeField] UnityEvent<bool> onWrapStateChanged;
        [SerializeField] UnityEvent<TransformDirections, bool> onDirectionLatchStateChanged;


        private void Awake()
        {
            bpm = ConfigRegistry.Midi.DefaultBpm;
            repeatRate = 60f / bpm;
            nextEventTime = Time.time + repeatRate;

            randomTransformService = (RandomTransformService)transformServices[(int)EmitterTransformMode.Random];
            shiftTransformService = (ShiftTransformService)transformServices[(int)EmitterTransformMode.Shift];
            transformMode = EmitterTransformMode.Shift;
        }

        private void Start()
        {
            // fire initial state to subscribers (UI)
            EmitterTransformDetail detail = transformServices[(int)transformMode].GetTransformDetail();
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
                        ulong[] original = frameManager.GetCurrentFrameEmitters();
                        ulong[] transformed = frameManager.GetCurrentFrameEmitters();

                        foreach (var transformService in transformServices)
                        {
                            if (transformService.IsLatched)
                                transformed = transformService.DoTransform(transformed);
                        }

                        if (!GroupsEqual(original, transformed))
                            onEmittersTransformed?.Invoke(transformed);
                    }

                    nextEventTime = Time.time + repeatRate;
                }
            }
        }

        private bool GroupsEqual(ulong[] a, ulong[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
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

        public void ActionOnBpmChanged(int newBpm)
        {
            bpm = newBpm;
            repeatRate = 60f / bpm;
            nextEventTime = Time.time + repeatRate;
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

        public void SetTransformMode(EmitterTransformMode transformMode)
        {
            if (transformMode != this.transformMode)
            {
                this.transformMode = transformMode;

                EmitterTransformDetail transformDetail = transformServices[(int)transformMode].GetTransformDetail();

                onTransformModeChanged?.Invoke(transformMode, transformDetail);
            }
        }

        public void SetPlaybackState(PlaybackState newPlaybackState)
        {
            playbackState = newPlaybackState;

            if (playbackState == PlaybackState.Playing)
            {
                foreach (var transformService in transformServices)
                {
                    transformService.IsLatched = false;
                }

                onLatchStateChanged?.Invoke(false);
            }
        }

        private void ActionOnEmittersTransformed(ulong[] transformedGroups)
        {
            onEmittersTransformed?.Invoke(transformedGroups);
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
