using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using UnityEngine;

namespace TemperaMental.Midi.Core
{
    public class MidiImmediateService : MonoBehaviour
    {
        OutputDevice outputDevice;
        int currentEmitterId;
        int activateCC;
        int placeCC;
        int removeCC;
        int gridWidth;
        int gridHeight;
        int gridSize;
        int emitterCount;

        // reusable bitmask buffers, one ulong per emitter (bits 0–63 map to grid positions)
        ulong[] previousGroups;
        ulong[] currentGroups;

        bool isEnabled;

        private void OnEnable()
        {
            activateCC = ConfigRegistry.Midi.ActivateCC;
            placeCC = ConfigRegistry.Midi.PlaceCC;
            removeCC = ConfigRegistry.Midi.RemoveCC;

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;
            gridSize = gridWidth * gridHeight;

            emitterCount = ConfigRegistry.Grid.EmitterCount;
            currentEmitterId = ConfigRegistry.Grid.DefaultEmitterId;

            previousGroups = new ulong[emitterCount];
            currentGroups = new ulong[emitterCount];
        }

        // send only when playback is off
        public void EnableSendingByPlaybackState(PlaybackState playbackState)
        { 
            switch (playbackState)
            {
                case PlaybackState.Idle:
                    isEnabled = true;
                    break;

                case PlaybackState.Playing:
                    isEnabled = false;
                    break;

                case PlaybackState.Paused:
                    isEnabled = false;
                    break;
            }
        }

        // send placed emitters, don't send emitters that are already lit
        public void SendEmitters(List<EmitterDetail> emitterDetails)
        {
            FillGroups(currentGroups, emitterDetails);

            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                ulong toRemove = previousGroups[emitterId] & ~currentGroups[emitterId];
                ulong toAdd = currentGroups[emitterId] & ~previousGroups[emitterId];

                if (toRemove == 0 && toAdd == 0) continue;

                if (emitterId != currentEmitterId)
                {
                    currentEmitterId = emitterId;
                    SendEmitterChangeMessage(emitterId);
                }

                for (byte pos = 0; pos < gridSize; pos++)
                {
                    bool remove = (toRemove & (1UL << pos)) != 0;
                    bool add = (toAdd & (1UL << pos)) != 0;

                    if (add)
                        SendEmitterMessage(placeCC, pos);
                    else if (remove)
                        SendEmitterMessage(removeCC, pos);
                }
            }

            CopyGroups(currentGroups, previousGroups);
        }

        public void RemoveEmitter(Vector2Int position)
        {
            byte pos = (byte)PositionToIndex(position);

            // clear any emitter set
            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
                previousGroups[emitterId] &= ~(1UL << pos);

            SendEmitterMessage(removeCC, pos);
        }

        public void AddEmitter(EmitterDetail emitterDetail)
        {
            byte emitterId = (byte)emitterDetail.EmitterId;
            byte pos = (byte)PositionToIndex(emitterDetail.Position);

            // clear this position from all emitters first, then set the correct one
            for (byte i = 0; i < emitterCount; i++)
                previousGroups[i] &= ~(1UL << pos);

            previousGroups[emitterId] |= 1UL << pos;

            if (emitterId != currentEmitterId)
            {
                currentEmitterId = emitterId;
                SendEmitterChangeMessage(emitterId);
            }

            SendEmitterMessage(placeCC, pos);
        }

        public void SetEmitterType(int emitterId)
        {
            if (emitterId != currentEmitterId)
            {
                currentEmitterId = emitterId;
                SendEmitterChangeMessage(emitterId);
            }
        }

        public void ClearOutputDevice()
        {
            outputDevice = null;
        }

        public void SetOutputDevice(OutputDevice outputDevice)
        {
            this.outputDevice = outputDevice;
        }

        // fill current groups with set emitters for this frame
        private void FillGroups(ulong[] groups, List<EmitterDetail> emitters)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                groups[i] = 0;
            }

            for (int i = 0; i < emitters.Count; i++)
            {
                byte emitterId = (byte)emitters[i].EmitterId;
                byte pos = (byte)PositionToIndex(emitters[i].Position);
                groups[emitterId] |= 1UL << pos;
            }
        }

        // move current set emitters to previous for comparison when new emitters added
        private static void CopyGroups(ulong[] source, ulong[] dest)
        {
            for (int i = 0; i < source.Length; i++)
                dest[i] = source[i];
        }

        // hack we're not honouring isEnabled here to allow emitter change during playback (drawing is allowed during playback)
        // this is a quick fix and needs a better solution
        private void SendEmitterChangeMessage(int emitterId)
        {
            if (outputDevice != null)
            {
                outputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)activateCC, (SevenBitNumber)emitterId));
            }
        }

        private void SendEmitterMessage(int cmdCC, int value)
        {
            if (outputDevice != null && isEnabled)
            {
                outputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)cmdCC, (SevenBitNumber)value));
            }
        }

        // change tilemap-based position to cc value index
        private int PositionToIndex(Vector2Int pos)
        {
            int flippedY = (gridHeight - 1) - pos.y;
            return pos.x * gridHeight + flippedY;
        }
    }
}