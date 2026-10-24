using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TemperaMental.Settings;
using TemperaMental.UI.Core;
using TMPro;
using UnityEngine;

namespace TemperaMental.Ui.Settings.App
{
    // 2-Lane per colour: Edit mode's colour buttons are dimmed while their 2-Lane is off. On / Off labels are optional
    public class EmitterSettingsUIManager : MonoBehaviour
    {
        [SerializeField] List<TextMeshProUGUI> twoLaneLabels = new List<TextMeshProUGUI>();

        [Header("Order: Blue, Red, Yellow, Green")]
        [SerializeField] List<DimmableButton> twoLaneButtons = new List<DimmableButton>();

        string onText;
        string offText;

        private void Awake()
        {
            onText = ConfigRegistry.UI.OnText;
            offText = ConfigRegistry.UI.OffText;
        }

        // 2-Lane isn't remembered and starts off; the manager raises no event for that, so show it here. DimmableButton
        // reads its colour in Awake, so this waits for Start
        private void Start()
        {
            bool[] twoLanes = EmitterSettingsManager.CurrentTwoLanes;

            for (int i = 0; twoLanes != null && i < twoLanes.Length; i++)
            {
                ActionOnEmitterTwoLaneChanged(i, twoLanes[i]);
            }
        }

        public void ActionOnEmitterTwoLaneChanged(int emitterId, bool isTwoLane)
        {
            if (emitterId < twoLaneLabels.Count && twoLaneLabels[emitterId] != null)
                twoLaneLabels[emitterId].text = isTwoLane ? onText : offText;

            if (emitterId < twoLaneButtons.Count && twoLaneButtons[emitterId] != null)
                twoLaneButtons[emitterId].SetDimmed(!isTwoLane);
        }
    }
}
