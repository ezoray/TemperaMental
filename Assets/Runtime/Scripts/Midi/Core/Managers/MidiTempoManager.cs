using TemperaMental.Applications.Config;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Core
{
    public class MidiTempoManager : MonoBehaviour
    { 
        int bpm;

        [SerializeField] UnityEvent<int> onBpmChanged;


        private void Awake()
        {
            bpm = ConfigRegistry.Midi.DefaultBpm;
        }
 
        public void SetBpm(int newBpm)
        {
            if (newBpm == bpm) return;

            bpm = newBpm;

            onBpmChanged?.Invoke(bpm);
        }

        public int GetBpm()
        {
            return bpm;
        }
    }
}
