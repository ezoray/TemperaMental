using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Settings.App
{
    public class MidiSettingsUIManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI midiChannel;


        public void ActionOnMidiChannelChanged(int channel)
        {
            midiChannel.text = channel.ToString();
        }
    }
}
