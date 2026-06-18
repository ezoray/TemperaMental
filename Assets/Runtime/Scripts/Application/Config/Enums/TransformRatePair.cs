namespace TemperaMental.Applications.Config
{
    [System.Serializable]
    public struct TransformRatePair
    {
        public int Value;
        public string Label;

        public TransformRatePair(int value, string label)
        {
            Value = value;
            Label = label;
        }
    }
}
