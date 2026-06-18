namespace TemperaMental.Core
{
    public class TransformEmitter
    {
        public TransformDirections CurrentDirections;
        public int Rate;

        public TransformEmitter(TransformDirections currentDirections, int rate)
        {
            CurrentDirections = currentDirections;
            Rate = rate;
        }
    }
}
