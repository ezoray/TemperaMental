using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Utils;
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
        int emitterCount;
        int gridSize;

        // tracks playback state to determine whether to send midi immediately
        PlaybackState playbackState;

        Stopwatch stopwatch;
        Thread sendThread;
        ConcurrentQueue<(int commandCC, int value)> messageQueue;
        volatile bool isSendThreadRunning;

        // delay between midi messages in milliseconds
        long midiSendIntervalMS;

        private void OnEnable()
        {
            activateCC = ConfigRegistry.Midi.ActivateCC;
            placeCC = ConfigRegistry.Midi.PlaceCC;
            removeCC = ConfigRegistry.Midi.RemoveCC;

            emitterCount = ConfigRegistry.Grid.EmitterCount;
            currentEmitterId = ConfigRegistry.Grid.DefaultEmitterId;
            gridSize = ConfigRegistry.Grid.GridWidth * ConfigRegistry.Grid.GridHeight;

            midiSendIntervalMS = ConfigRegistry.Midi.MidiSendIntervalMS;

            playbackState = PlaybackState.Idle;

            messageQueue = new ConcurrentQueue<(int, int)>();
            StartSendThread();
        }

        public void SetPlaybackState(PlaybackState newPlaybackState)
        {
            playbackState = newPlaybackState;
        }

        public void SendFrame(ulong[] emitterGroups)
        {
            if (playbackState == PlaybackState.Playing) return;

            // clear the queue and restart
            while (messageQueue.TryDequeue(out _)) { }

            // enqueue clears
            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                messageQueue.Enqueue((activateCC, emitterId));
                messageQueue.Enqueue((removeCC, ConfigRegistry.Midi.ClearEmittersValue));
            }

            // enqueue placements
            for (byte emitterId = 0; emitterId < emitterCount; emitterId++)
            {
                ulong group = emitterGroups[emitterId];

                if (group == 0) continue;

                messageQueue.Enqueue((activateCC, emitterId));

                for (byte pos = 0; pos < gridSize; pos++)
                {
                    if ((group & (1UL << pos)) != 0)
                    {
                        messageQueue.Enqueue((placeCC, pos));
                    }
                }
            }
        }

        private void StartSendThread()
        {
            isSendThreadRunning = true;
            stopwatch = Stopwatch.StartNew();
            sendThread = new Thread(SendThreadLoop) { IsBackground = true };
            sendThread.Start();
        }

        private void SendThreadLoop()
        {
            long nextMessageTime = 0;

            while (isSendThreadRunning)
            {
                if (stopwatch.ElapsedMilliseconds >= nextMessageTime)
                {
                    if (messageQueue.TryDequeue(out var message))
                    {
                        if (outputDevice != null)
                        {
                            outputDevice.SendEvent(new ControlChangeEvent(
                                (SevenBitNumber)message.commandCC,
                                (SevenBitNumber)message.value));
                        }

                        nextMessageTime = stopwatch.ElapsedMilliseconds + midiSendIntervalMS;
                    }
                }

                Thread.Sleep(1);
            }
        }

        private void StopSendThread()
        {
            isSendThreadRunning = false;
            sendThread?.Join();
            sendThread = null;
        }

        public void RemoveEmitter(Vector2Int position)
        {
            SendEmitterMessage(removeCC, EmitterUtils.PositionToIndex(position));
        }

        public void AddEmitter(EmitterDetail emitterDetail)
        {
            SendEmitterChangeMessage(emitterDetail.EmitterId);
            SendEmitterMessage(placeCC, EmitterUtils.PositionToIndex(emitterDetail.Position));
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

        private void SendEmitterChangeMessage(int emitterId)
        {
            currentEmitterId = emitterId;

            if (outputDevice != null)
            {
                outputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)activateCC, (SevenBitNumber)emitterId));
            }
        }

        private void SendEmitterMessage(int commandCC, int value)
        {
            if (outputDevice != null)
            {
                outputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)commandCC, (SevenBitNumber)value));
            }
        }

        private void OnDisable()
        {
            StopSendThread();
        }
    }
}