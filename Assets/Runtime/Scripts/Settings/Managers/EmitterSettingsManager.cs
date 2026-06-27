using TemperaMental.Applications.Config;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Settings
{
    public class EmitterSettingsManager : MonoBehaviour
    {
        bool[] twoLanes;

        [SerializeField] UnityEvent<int, bool> onEmitterTwoLaneChanged;

        private void Awake()
        {
            twoLanes = new bool[ConfigRegistry.Grid.EmitterCount];
            CurrentTwoLanes = twoLanes;
        }

        public void ToggleTwoLane(int emitterId)
        {
            twoLanes[emitterId] = !twoLanes[emitterId];
            onEmitterTwoLaneChanged?.Invoke(emitterId, twoLanes[emitterId]);
        }

        public static bool[] CurrentTwoLanes { get; private set; }
    }
}
