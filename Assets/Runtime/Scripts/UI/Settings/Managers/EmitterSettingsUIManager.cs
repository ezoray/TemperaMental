using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Settings
{
    public class EmitterSettingsUIManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI[] emitterChannels;


        public void ActionOnEmitterMidiChannelChanged(int emitterId, int midiChannel)
        {
            emitterChannels[emitterId].text = midiChannel.ToString();
        }
    }
}
