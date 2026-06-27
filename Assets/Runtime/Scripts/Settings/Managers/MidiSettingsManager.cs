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

        [SerializeField] UnityEvent<int> onMidiChannelChanged;


        private void Awake()
        {
            midiChannelCount = ConfigRegistry.Midi.MidiChannelCount;

            midiChannel = PlayerPrefs.GetInt($"{ChannelPrefsKey}", ConfigRegistry.Midi.DefaultMidiChannel);

            onMidiChannelChanged?.Invoke(midiChannel);
        }

        public void CycleMidiChannel(int directionValue)
        {
            midiChannel = Mathf.Clamp(midiChannel + directionValue, 1, midiChannelCount);

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
