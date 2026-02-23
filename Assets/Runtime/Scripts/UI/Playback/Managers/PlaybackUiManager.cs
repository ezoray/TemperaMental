using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tempera.Mental.Ui.Playbacks
{
    public class PlaybackUiManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI outputDevice;
        [SerializeField] Slider bpmSlider;
        [SerializeField] Dropdown deviceDropdown;


        private void Start()
        {
            
        }

        public void ActionOnSetOutputDevice(string deviceName)
        {
            outputDevice.text = deviceName;
        }
    }
}
