using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Settings
{
    public class EmitterSettingsManager : MonoBehaviour
    {
        const string ChannelPrefsKey = "EmitterMidiChannel_";

        List<int> emitterMidiChannels;
        int midiChannelCount = 16;

        [SerializeField] UnityEvent<int, int> onEmitterMidiChannelChanged;
        [SerializeField] UnityEvent<List<int>> onEmitterMidiChannelsChanged;

        private void Awake()
        {
            midiChannelCount = ConfigRegistry.Midi.MidiChannelCount;

            emitterMidiChannels = new List<int>();

            for (int i = 0; i < ConfigRegistry.Grid.EmitterCount; i++)
            {
                int channel = PlayerPrefs.GetInt($"{ChannelPrefsKey}{i}", ConfigRegistry.Midi.DefaultMidiChannel);

                emitterMidiChannels.Add(channel);

                onEmitterMidiChannelChanged?.Invoke(i, channel);
            }
        }

        private void SaveChannels()
        {
            for (int i = 0; i < emitterMidiChannels.Count; i++)
            {
                PlayerPrefs.SetInt($"{ChannelPrefsKey}{i}", emitterMidiChannels[i]);
            }

            PlayerPrefs.Save();
        }

        public void CycleChannel(int emitterId, int direction)
        {
            int midiChannel = Mathf.Clamp(emitterMidiChannels[emitterId] + direction, 1, midiChannelCount);

            emitterMidiChannels[emitterId] = midiChannel;

            onEmitterMidiChannelChanged?.Invoke(emitterId, midiChannel);
        }

        public void SetEmitterChannels(DisplayViewType viewType)
        {
            LogMan.Log("SendEmitterChannels: " + viewType);

            SaveChannels();

            onEmitterMidiChannelsChanged?.Invoke(emitterMidiChannels);
        }
    }
}
