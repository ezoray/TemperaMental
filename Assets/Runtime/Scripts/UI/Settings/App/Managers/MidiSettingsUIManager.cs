using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Settings.App
{
    public class MidiSettingsUIManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI midiChannel;
        [SerializeField] TextMeshProUGUI maxBpm;


        public void ActionOnMaxBpmChanged(int channel)
        {
            maxBpm.text = channel.ToString();
        }

        public void ActionOnMidiChannelChanged(int channel)
        {
            midiChannel.text = channel.ToString();
        }
    }
}
