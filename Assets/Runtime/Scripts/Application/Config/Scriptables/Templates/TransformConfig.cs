using System.Collections.Generic;
using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "TransformConfig", menuName = "Scriptable Objects/TransformConfig")]
    public class TransformConfig : ScriptableObject
    {
        public int DefaultRate = 16;

        public string SimpleMode = "Simple";
        public string IndividualMode = "Individual";

        // transform trigger time relative to a single beat
        public List<TransformRatePair> RatePairs;

        private void Reset()
        {
            RatePairs = new List<TransformRatePair>
            {
                new TransformRatePair(0, "Disabled"),
                new TransformRatePair(1, "16 Beats"),
                new TransformRatePair(2, "8 Beats"),
                new TransformRatePair(4, "4 Beats"),
                new TransformRatePair(8, "2 Beats"),
                new TransformRatePair(16, "Per Beat"),
            };
        }
    }
}
