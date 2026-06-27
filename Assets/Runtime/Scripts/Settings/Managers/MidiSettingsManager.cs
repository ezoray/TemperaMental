using TemperaMental.Applications.Config;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Settings
{
    public class MidiSettingsManager : MonoBehaviour
    {
        const string ChannelPrefsKey = "MidiChannel";

        int midiChannel;
        int midiChannelCount;

        [SerializeField] UnityEvent<int> onMidiChannelUpdated; // updates UI
        [SerializeField] UnityEvent<int> onMidiChannelChanged;


        private void Awake()
        {
            midiChannelCount = ConfigRegistry.Midi.MidiChannelCount;

            midiChannel = PlayerPrefs.GetInt($"{ChannelPrefsKey}", ConfigRegistry.Midi.DefaultMidiChannel);

            onMidiChannelUpdated?.Invoke(midiChannel);
            onMidiChannelChanged?.Invoke(midiChannel);
        }

        public void CycleMidiChannel(int directionValue)
        {
            midiChannel = Mathf.Clamp(midiChannel + directionValue, 1, midiChannelCount);

            onMidiChannelUpdated?.Invoke(midiChannel);
        }

        public void SetMidiChannel()
        {
            SaveChannel();

            onMidiChannelChanged?.Invoke(midiChannel);
        }

        private void SaveChannel()
        {
            PlayerPrefs.SetInt($"{ChannelPrefsKey}", midiChannel);
            PlayerPrefs.Save();
        }
    }
}
