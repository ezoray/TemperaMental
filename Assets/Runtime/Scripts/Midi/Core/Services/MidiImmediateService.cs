using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using TemperaMental.Utils;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiImmediateService : MonoBehaviour
    {
        OutputDevice outputDevice;

        int activateCC;
        int placeCC;
        int removeCC;
        int emitterCount;
        int gridSize;
        byte clearEmittersValue;

        ulong[] previousGroups;

        Playback framePlayback;
        readonly TempoMap tempoMap = TempoMap.Default;

        volatile bool playbackFinishedFlag;
        volatile bool playbackErrorFlag;
        volatile bool playbackOverlapFlag;
        volatile string playbackErrorMessage;
        volatile int overlapRemainingEvents;

        int playedEventCount;
        int totalEventCount;

        // tracks playback state to determine whether to send midi immediately
        PlaybackState playbackState;

        private void OnEnable()
        {
            activateCC = ConfigRegistry.Midi.ActivateCC;
            placeCC = ConfigRegistry.Midi.PlaceCC;
            removeCC = ConfigRegistry.Midi.RemoveCC;
            clearEmittersValue = ConfigRegistry.Midi.ClearEmittersValue;

            emitterCount = ConfigRegistry.Grid.EmitterCount;
            gridSize = ConfigRegistry.Grid.GridWidth * ConfigRegistry.Grid.GridHeight;

            previousGroups = new ulong[emitterCount];
        }

        private void Update()
        {
            if (playbackOverlapFlag)
            {
                playbackOverlapFlag = false;
                LogMan.LogWarning($"New frame arrived before previous finished — {overlapRemainingEvents} events unplayed");
            }

            if (playbackFinishedFlag)
            {
                playbackFinishedFlag = false;
//                LogMan.Log($"Frame playback finished — {playedEventCount}/{totalEventCount} events played");
            }

            if (playbackErrorFlag)
            {
                playbackErrorFlag = false;
                LogMan.LogError($"Frame playback error: {playbackErrorMessage}");
            }
        }

        public void SetPlaybackState(PlaybackState newPlaybackState)
        {
            playbackState = newPlaybackState;

            if (playbackState == PlaybackState.Playing || playbackState == PlaybackState.Paused)
                DisposeFramePlayback();
        }

        // --- Frame playback ---

        public void SendFrame(ulong[] emitterGroups)
        {
            if (playbackState == PlaybackState.Playing || playbackState == PlaybackState.Paused) return;

            var events = BuildFrameEvents(emitterGroups);
            if (events.Count == 0) return;

            Array.Copy(emitterGroups, previousGroups, emitterCount);

            if (framePlayback != null)
            {
                if (framePlayback.IsRunning)
                {
                    overlapRemainingEvents = CountRemainingEvents();
                    playbackOverlapFlag = true;
                }

                DisposeFramePlayback();
            }

            totalEventCount = events.Count;
            playedEventCount = 0;

            framePlayback = new Playback(events, tempoMap, outputDevice);
            framePlayback.ErrorOccurred += OnPlaybackError;
            framePlayback.Finished += OnPlaybackFinished;
            framePlayback.EventPlayed += OnEventPlayed;

            framePlayback.Start();
        }

        // --- Callbacks (DryWetMidi thread — set flags only) ---

        void OnEventPlayed(object sender, MidiEventPlayedEventArgs e)
        {
            playedEventCount++;
        }

        void OnPlaybackFinished(object sender, EventArgs e)
        {
            playbackFinishedFlag = true;
        }

        void OnPlaybackError(object sender, PlaybackErrorOccurredEventArgs e)
        {
            playbackErrorMessage = e.Exception.Message;
            playbackErrorFlag = true;
        }

        int CountRemainingEvents() => totalEventCount - playedEventCount;

        void DisposeFramePlayback()
        {
            if (framePlayback == null) return;

            framePlayback.ErrorOccurred -= OnPlaybackError;
            framePlayback.Finished -= OnPlaybackFinished;
            framePlayback.EventPlayed -= OnEventPlayed;

            framePlayback.Stop();
            framePlayback.Dispose();
            framePlayback = null;

            playedEventCount = 0;
        }

        List<TimedEvent> BuildFrameEvents(ulong[] emitterGroups)
        {
            var events = new List<TimedEvent>();
            long tick = 0;

            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                ulong current = emitterGroups[emitterId];
                ulong previous = previousGroups[emitterId];

                ulong toRemove = previous & ~current;
                ulong toAdd = current & ~previous;

                if (toRemove == 0 && toAdd == 0) continue;

                events.Add(CC(activateCC, emitterId, tick++));

                if (toRemove != 0)
                {
                    int removeCount = BitCount(toRemove);
                    int keepCount = BitCount(current);

                    if (removeCount > 1 + keepCount)
                    {
                        // bulk clear then re-place survivors
                        events.Add(CC(removeCC, clearEmittersValue, tick++));

                        for (byte pos = 0; pos < gridSize; pos++)
                            if ((current & (1UL << pos)) != 0)
                                events.Add(CC(placeCC, pos, tick++));
                    }
                    else
                    {
                        // individual removes and adds interleaved by position
                        for (byte pos = 0; pos < gridSize; pos++)
                        {
                            if ((toRemove & (1UL << pos)) != 0)
                                events.Add(CC(removeCC, pos, tick++));
                            else if ((toAdd & (1UL << pos)) != 0)
                                events.Add(CC(placeCC, pos, tick++));
                        }
                    }
                }
                else
                {
                    for (byte pos = 0; pos < gridSize; pos++)
                        if ((toAdd & (1UL << pos)) != 0)
                            events.Add(CC(placeCC, pos, tick++));
                }
            }

            return events;
        }

        void StopFramePlayback()
        {
            if (framePlayback == null) return;
            framePlayback.Stop();
            framePlayback.Dispose();
            framePlayback = null;
        }

        // --- Immediate single-event paths ---

        public void AddEmitter(EmitterDetail emitterDetail)
        {
            byte emitterId = (byte)emitterDetail.EmitterId;
            byte pos = (byte)EmitterUtils.PositionToIndex(emitterDetail.Position);

            // clear this position from all emitters then set for the correct one
            for (byte i = 0; i < emitterCount; i++)
                previousGroups[i] &= ~(1UL << pos);
            previousGroups[emitterId] |= 1UL << pos;

            outputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)activateCC, (SevenBitNumber)emitterId));
            outputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)placeCC, (SevenBitNumber)pos));
        }

        public void RemoveEmitter(Vector2Int position)
        {
            byte pos = (byte)EmitterUtils.PositionToIndex(position);

            for (byte i = 0; i < emitterCount; i++)
                previousGroups[i] &= ~(1UL << pos);

            // no activate needed — remove applies to the active position regardless of emitter
            outputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)removeCC, (SevenBitNumber)pos));
        }

        public void SetEmitterType(int emitterId)
        {
            outputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)activateCC, (SevenBitNumber)emitterId));
        }

        // --- Device management ---

        public void SetOutputDevice(OutputDevice device)
        {
            outputDevice = device;
        }

        public void ClearOutputDevice()
        {
            StopFramePlayback();
            outputDevice = null;
        }

        private void OnDisable()
        {
            StopFramePlayback();
        }

        // --- Helpers ---

        TimedEvent CC(int cc, byte value, long tick) =>
            new TimedEvent(
                new ControlChangeEvent((SevenBitNumber)cc, (SevenBitNumber)value),
                tick
            );

        static int BitCount(ulong mask)
        {
            int count = 0;
            while (mask != 0) { mask &= mask - 1; count++; }
            return count;
        }
    }
}