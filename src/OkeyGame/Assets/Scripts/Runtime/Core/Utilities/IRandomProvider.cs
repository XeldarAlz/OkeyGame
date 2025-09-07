using Runtime.Core.Architecture;

namespace Runtime.Core.Utilities
{
    public interface IRandomProvider : IService
    {
        int Range(int min, int max);
        float Range(float min, float max);
        float Value { get; }
        bool Bool { get; }
        void SetSeed(int seed);
    }
}
