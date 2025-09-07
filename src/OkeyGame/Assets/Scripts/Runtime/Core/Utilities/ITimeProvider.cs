using Runtime.Core.Architecture;

namespace Runtime.Core.Utilities
{
    public interface ITimeProvider : IService
    {
        float Time { get; }
        float DeltaTime { get; }
        float UnscaledTime { get; }
        float UnscaledDeltaTime { get; }
        int FrameCount { get; }
    }
}
