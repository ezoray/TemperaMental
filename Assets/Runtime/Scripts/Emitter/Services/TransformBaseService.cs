using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.Emitters
{
    // base class for transforms except Random which needs to be handled separately
    public abstract class TransformBaseService : MonoBehaviour
    {
        public abstract ulong[] DoTransform(ulong[] groups, TransformEmitterFlags activeEmitters, TransformDirectionFlags directions);
    }
}
