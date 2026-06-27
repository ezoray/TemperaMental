using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Settings
{
    public class EmitterSettingsEventController : MonoBehaviour
    {
        [SerializeField] EmitterSettingsManager settingsManager;


        public void OnClickTwoLane(int emitterId)
        {
            settingsManager.ToggleTwoLane(emitterId);
        }
    }
}
