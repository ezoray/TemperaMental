namespace TemperaMental.Applications.Config
{
    [System.Serializable]
    public struct TransformRatePair
    {
        public float Value;
        public string Label;

        public TransformRatePair(float value, string label)
        {
            Value = value;
            Label = label;
        }
    }
}
