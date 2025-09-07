using UnityEngine;

namespace Runtime.Core.Utilities
{
    public sealed class UnityRandomProvider : IRandomProvider
    {
        public int Range(int min, int max)
        {
            return Random.Range(min, max);
        }

        public float Range(float min, float max)
        {
            return Random.Range(min, max);
        }

        public float Value => Random.value;

        public bool Bool => Random.value > 0.5f;

        public void SetSeed(int seed)
        {
            Random.InitState(seed);
        }
    }
}
