using TemperaMental.Core;

namespace TemperaMental.Transforms
{
    public interface ITransformModeState
    {
        TransformActiveEmitters GetEmitter();
        void SetEmitter(int emitterId);
        TransformActiveEmitters GetActiveEmitters();
        TransformDirections GetDirections();
        TransformDirections GetDirections(int emitterId);
        void SetDirections(TransformDirections directions);
        int GetRate();
        void SetRate(int rate);
        bool ShouldFire(int tickCounter);
        bool ShouldEmitterFire(int emitterId, int tickCounter);
        void Reset();
    }
}