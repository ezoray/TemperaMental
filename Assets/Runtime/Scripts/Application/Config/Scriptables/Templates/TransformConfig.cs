using System.Collections.Generic;
using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "TransformConfig", menuName = "Scriptable Objects/TransformConfig")]
    public class TransformConfig : ScriptableObject
    {
        // transform trigger time relative to a single beat
        public List<TransformRatePair> RatePairs;

        private void Reset()
        {
            RatePairs = new List<TransformRatePair>
            {
                new TransformRatePair(0.0625f, "1/16"),
                new TransformRatePair(0.125f, "1/8"),
                new TransformRatePair(0.25f, "1/4"),
                new TransformRatePair(0.5f, "1/2"),
                new TransformRatePair(1f, "1/1"),
            };
        }
    }
}
