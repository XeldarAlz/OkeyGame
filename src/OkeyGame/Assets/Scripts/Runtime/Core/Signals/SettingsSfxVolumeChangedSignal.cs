namespace Runtime.Core.Signals
{
    public readonly struct SettingsSfxVolumeChangedSignal
    {
        public readonly float Value;

        public SettingsSfxVolumeChangedSignal(float value)
        {
            Value = value;
        }
    }
}