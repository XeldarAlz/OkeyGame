namespace Runtime.Core.Signals
{
    public readonly struct SettingsMusicVolumeChangedSignal
    {
        public readonly float Value;

        public SettingsMusicVolumeChangedSignal(float value)
        {
            Value = value;
        }
    }
}
