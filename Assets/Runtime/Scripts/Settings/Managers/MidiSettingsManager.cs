using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Settings
{
    public class MidiSettingsManager : MonoBehaviour
    {
        const string ChannelPrefsKey = "MidiChannel";

        int midiChannel;
        int midiChannelCount;

        [SerializeField] UnityEvent<int> onMidiChannelChanged;


        private void Awake()
        {
            midiChannelCount = ConfigRegistry.Midi.MidiChannelCount;

            midiChannel = PlayerPrefs.GetInt($"{ChannelPrefsKey}", ConfigRegistry.Midi.DefaultMidiChannel);

            onMidiChannelChanged?.Invoke(midiChannel);
        }

        public void CycleMidiChannel(int direction)
        {
            midiChannel = Mathf.Clamp(midiChannel + direction, 1, midiChannelCount);

            onMidiChannelChanged?.Invoke(midiChannel);
        }

        public void SetMidiChannel()
        {
            SaveChannel();

            LogMan.LogTemp("Output MIDI Channel: " + midiChannel);

            onMidiChannelChanged?.Invoke(midiChannel);
        }

        private void SaveChannel()
        {
            PlayerPrefs.SetInt($"{ChannelPrefsKey}", midiChannel);
            PlayerPrefs.Save();
        }
    }
}
