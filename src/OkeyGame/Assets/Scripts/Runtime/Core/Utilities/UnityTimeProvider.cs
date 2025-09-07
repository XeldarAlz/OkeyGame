namespace Runtime.Core.Utilities
{
    public sealed class UnityTimeProvider : ITimeProvider
    {
        public float Time => UnityEngine.Time.time;
        public float DeltaTime => UnityEngine.Time.deltaTime;
        public float UnscaledTime => UnityEngine.Time.unscaledTime;
        public float UnscaledDeltaTime => UnityEngine.Time.unscaledDeltaTime;
        public int FrameCount => UnityEngine.Time.frameCount;
    }
}
