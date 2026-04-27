using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Applications.Config
{
    [CreateAssetMenu(fileName = "EmitterConfig", menuName = "Scriptable Objects/EmitterConfig")]
    public class EmitterConfig : ScriptableObject
    {
        // set values on instance - seems scriptables don't like OR'ed flags
        public TransformDirections RandomTransformDirections;
        public TransformDirections FlipTransformDirections;
        public TransformDirections RotateTransformDirections;
        public TransformDirections SwapTransformDirections;
        public TransformDirections ShiftTransformDirections;
    }
}
