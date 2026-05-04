using TemperaMental.Applications.Config;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Recording
{
    public class RecordUIManager : MonoBehaviour
    {
        [SerializeField] Button recordButton;

        Color defaultOffColor;
        Color recordOnColor;


        private void Awake()
        {
            defaultOffColor = ConfigRegistry.UI.DefaultColor;
            recordOnColor = ConfigRegistry.UI.RedColor;
        }

        public void ActionOnRecordStateChanged(bool isOn)
        {
            recordButton.image.color = isOn ? recordOnColor : defaultOffColor;
        }
    }
}
