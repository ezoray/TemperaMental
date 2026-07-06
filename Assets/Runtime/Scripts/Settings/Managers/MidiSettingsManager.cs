using TemperaMental.Applications.Config;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Settings
{
    public class MidiSettingsManager : MonoBehaviour
    {
        string channelPrefsKey;
        string maxBpmPrefsKey;

        int midiChannel;
        int midiChannelCount;
        int maxBpm;
        int defaultMaxBpm;
        int minimumMaxBpm;

        [SerializeField] UnityEvent<int> onMidiChannelChanged;
        [SerializeField] UnityEvent<int> onMaxBpmChanged;

        private void Awake()
        {
            channelPrefsKey = ConfigRegistry.Midi.MidiChannelPrefsKey;
            maxBpmPrefsKey = ConfigRegistry.Midi.MaxBpmPrefsKey;

            midiChannelCount = ConfigRegistry.Midi.MidiChannelCount;
            midiChannel = PlayerPrefs.GetInt($"{channelPrefsKey}", ConfigRegistry.Midi.DefaultMidiChannel);

            onMidiChannelChanged?.Invoke(midiChannel);

            minimumMaxBpm = ConfigRegistry.Midi.MinimumMaxBpm;
            defaultMaxBpm = ConfigRegistry.Midi.MaxBpm;
            maxBpm = PlayerPrefs.GetInt($"{maxBpmPrefsKey}", ConfigRegistry.Midi.MaxBpm);

            onMaxBpmChanged?.Invoke(maxBpm);
        }

        public void UpdateMaxBpm(int directionValue)
        {
            directionValue *= 100;

            maxBpm = Mathf.Clamp(maxBpm + directionValue, minimumMaxBpm, defaultMaxBpm);

            onMaxBpmChanged?.Invoke(maxBpm);
        }

        public void UpdateMidiChannel(int directionValue)
        {
            midiChannel = Mathf.Clamp(midiChannel + directionValue, 1, midiChannelCount);

            onMidiChannelChanged?.Invoke(midiChannel);
        }

        public void SaveMidiSettings()
        {
            LogMan.LogTemp($"Midi Channel {midiChannel}, Max BPM {maxBpm}");

            PlayerPrefs.SetInt($"{channelPrefsKey}", midiChannel);
            PlayerPrefs.SetInt($"{maxBpmPrefsKey}", maxBpm);
            PlayerPrefs.Save();
        }

        public int MaxBpm { get => maxBpm; }
    }
}
