using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TMPro;
using UnityEngine;

namespace TemperaMental.Ui.Settings.App
{
    public class EmitterSettingsUIManager : MonoBehaviour
    {
        [SerializeField] List<TextMeshProUGUI> twoLaneLabels;

        string onText;
        string offText;


        private void Awake()
        {
            onText = ConfigRegistry.UI.OnText;
            offText = ConfigRegistry.UI.OffText;
        }

        public void ActionOnEmitterTwoLaneChanged(int emitterId, bool isTwoLane)
        {
            twoLaneLabels[emitterId].text = isTwoLane ? onText : offText;
        }
    }
}
