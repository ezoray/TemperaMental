using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Settings.Transforms
{
    public class TransformModeSettingsUIManager : MonoBehaviour
    {
        const int ModeCount = 2;

        [SerializeField] TextMeshProUGUI[] modes;

        string[] modeNames;


        private void Awake()
        {
            modeNames = new string[ModeCount];
            modeNames[(int)TransformMode.Simple] = ConfigRegistry.Transform.SimpleMode;
            modeNames[(int)TransformMode.Individual] = ConfigRegistry.Transform.IndividualMode;
        }

        public void ActionOnTransformModeChanged(TransformType transformType, TransformMode transformMode)
        {
            modes[(int)transformType].text = modeNames[(int)transformMode];
        }
    }
}
