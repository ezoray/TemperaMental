using TemperaMental.Core;

namespace TemperaMental.Transforms
{
    public class SimpleModeState : ITransformModeState
    {
        int defaultRate;

        TransformEmitter emitter;
        TransformActiveEmitters activeEmitters;

        public SimpleModeState(int defaultRate)
        {
            this.defaultRate = defaultRate;

            emitter = new TransformEmitter(TransformDirections.None, defaultRate);
            activeEmitters = TransformActiveEmitters.All;
        }

        public TransformActiveEmitters GetActiveEmitters() => activeEmitters;

        public TransformActiveEmitters GetEmitter() => activeEmitters;

        public void SetEmitter(int emitterId)
        {
            // Toggle the emitter's active state
            TransformActiveEmitters flag = (TransformActiveEmitters)(1 << emitterId);
            activeEmitters ^= flag;
        }

        public TransformDirections GetDirections() => emitter.CurrentDirections;

        public bool ShouldEmitterFire(int emitterId, int tickCounter) => false;
        public TransformDirections GetDirections(int emitterId) => TransformDirections.None;

        public void SetDirections(TransformDirections directions) =>
            emitter.CurrentDirections = directions;

        public int GetRate() => emitter.Rate;

        public void SetRate(int rate) => emitter.Rate = rate;

        public bool ShouldFire(int tickCounter) => emitter.Rate > 0 && tickCounter % emitter.Rate == 0;

        public void Reset()
        {
            emitter.CurrentDirections = TransformDirections.None;
            emitter.Rate = defaultRate;
            activeEmitters = TransformActiveEmitters.All;
        }
    }
}