using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Settings.App
{
    public class MidiSettingsUIManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI midiChannel;


        public void ActionOnMidiChannelUpdated(int channel)
        {
            midiChannel.text = channel.ToString();
        }
    }
}
