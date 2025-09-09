namespace Runtime.Core.Signals
{
    public readonly struct SceneLoadingProgressSignal
    {
        public readonly float Value;

        public SceneLoadingProgressSignal(float value)
        {
            Value = value;
        }
    }
}