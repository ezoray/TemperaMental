using System.Collections.Generic;
using TemperaMental.Core;

namespace TemperaMental.Transforms
{
    public class IndividualModeState : ITransformModeState
    {
        const int BlueEmitter = 0;

        int defaultRate;

        List<TransformEmitter> emitters;
        int selectedEmitter;

        public int SelectedEmitter => selectedEmitter;


        public IndividualModeState(int defaultRate)
        {
            this.defaultRate = defaultRate;
            selectedEmitter = BlueEmitter;

            emitters = new List<TransformEmitter>
            {
                new TransformEmitter(TransformDirections.None, defaultRate),
                new TransformEmitter(TransformDirections.None, defaultRate),
                new TransformEmitter(TransformDirections.None, defaultRate),
                new TransformEmitter(TransformDirections.None, defaultRate),
            };
        }

        public TransformActiveEmitters GetActiveEmitters()
        {
            TransformActiveEmitters active = TransformActiveEmitters.None;

            for (int i = 0; i < emitters.Count; i++)
            {
                if (emitters[i].Rate > 0 && emitters[i].CurrentDirections != TransformDirections.None)
                    active |= (TransformActiveEmitters)(1 << i);
            }

            return active;
        }

        public TransformActiveEmitters GetEmitter()
        {
            return (TransformActiveEmitters)(1 << selectedEmitter);
        }

        public void SetEmitter(int emitterId) => selectedEmitter = emitterId;

        public TransformDirections GetDirections() => emitters[selectedEmitter].CurrentDirections;

        public void SetDirections(TransformDirections directions) =>
            emitters[selectedEmitter].CurrentDirections = directions;

        public int GetRate() => emitters[selectedEmitter].Rate;

        public void SetRate(int rate) => emitters[selectedEmitter].Rate = rate;

        // returns true if any active emitter should fire this tick
        public bool ShouldFire(int tickCounter)
        {
            foreach (var emitter in emitters)
            {
                if (emitter.Rate > 0 && tickCounter % emitter.Rate == 0)
                    return true;
            }

            return false;
        }

        public void Reset()
        {
            selectedEmitter = BlueEmitter;

            foreach (var emitter in emitters)
            {
                emitter.CurrentDirections = TransformDirections.None;
                emitter.Rate = defaultRate;
            }
        }

        public bool ShouldEmitterFire(int emitterId, int tickCounter)
        {
            int rate = emitters[emitterId].Rate;
            return rate > 0 && tickCounter % rate == 0;
        }

        public TransformDirections GetDirections(int emitterId) =>
            emitters[emitterId].CurrentDirections;
    }
}