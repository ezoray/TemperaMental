using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TemperaMental.UI.Settings.Transforms
{
    // Simple or Individual: the Transform page's Mode button shows the selected transform's mode. Swap has no
    // Individual mode, so the button is off while Swap is selected. Texts per transform type are optional.
    public class TransformModeSettingsUIManager : MonoBehaviour
    {
        const int ModeCount = 2;

        [Header("Optional, by TransformType")]
        [SerializeField] TextMeshProUGUI[] modes = new TextMeshProUGUI[0];

        [Header("The selected transform's mode")]
        [SerializeField] TextMeshProUGUI currentMode;
        [SerializeField] Button currentModeButton;

        string[] modeNames;

        private void Awake()
        {
            modeNames = new string[ModeCount];
            modeNames[(int)TransformMode.Simple] = ConfigRegistry.Transform.SimpleMode;
            modeNames[(int)TransformMode.Individual] = ConfigRegistry.Transform.IndividualMode;
        }

        public void ActionOnTransformModeChanged(TransformType transformType, TransformMode transformMode)
        {
            int index = (int)transformType;
            if (index < modes.Length && modes[index] != null) modes[index].text = modeNames[(int)transformMode];
        }

        public void ActionOnTransformChanged(TransformType transformType, TransformDetail transformDetail)
        {
            if (currentMode != null) currentMode.text = modeNames[(int)transformDetail.TransformMode];
            if (currentModeButton != null) currentModeButton.interactable = transformType != TransformType.Swap;
        }
    }
}
