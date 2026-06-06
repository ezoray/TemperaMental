using System.Collections.Generic;
using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "TransformConfig", menuName = "Scriptable Objects/TransformConfig")]
    public class TransformConfig : ScriptableObject
    {
        public int TicksPerBpm = 10;
        public float DefaultRate = 1f;

        // transform trigger time relative to a single beat
        public List<TransformRatePair> RatePairs;

        private void Reset()
        {
            RatePairs = new List<TransformRatePair>
            {
                new TransformRatePair(0.0625f, "16 Beats"),
                new TransformRatePair(0.125f, "8 Beats"),
                new TransformRatePair(0.25f, "4 Beats"),
                new TransformRatePair(0.5f, "2 Beats"),
                new TransformRatePair(1f, "1 Beat"),
            };
        }
    }
}
