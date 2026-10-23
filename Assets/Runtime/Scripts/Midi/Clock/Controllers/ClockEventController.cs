using UnityEngine;

namespace TemperaMental.Midi.Clock
{
    public class ClockEventController : MonoBehaviour
    {
        [SerializeField] ClockEngine clockEngine;

        public void ActionOnBpmChanged(int newBpm) => clockEngine.SetBpm(newBpm);
    }
}
