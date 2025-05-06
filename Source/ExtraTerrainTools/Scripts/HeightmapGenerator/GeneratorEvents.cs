

using UnityEngine;

namespace TerrainTools.NoiseGenerator
{
    public class GeneratorStartedEvent
    {
        public NoiseGenerator Generator { get; }

        public GeneratorStartedEvent(NoiseGenerator generator)
        {
            Generator = generator;
        }
    }
    public class GeneratorProgressEvent
    {
        public NoiseGenerator Generator { get; }
        public float Progress { get; }

        public GeneratorProgressEvent(NoiseGenerator generator, float progress)
        {
            Generator = generator;
            Progress = Mathf.Clamp(progress, 0, 1);
        }
    }

    public class GeneratorFinishedEvent
    {
        public NoiseGenerator Generator { get; }

        public GeneratorFinishedEvent(NoiseGenerator generator)
        {
            Generator = generator;
        }
    }
}