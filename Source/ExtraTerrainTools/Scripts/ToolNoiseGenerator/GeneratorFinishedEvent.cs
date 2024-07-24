

using UnityEngine;

namespace TerrainTools.NoiseGenerator
{
    public class GeneratorProgressEvent 
    {
        public NoiseGenerator Generator { get; }
        public float Progress { get; }

        public GeneratorProgressEvent( NoiseGenerator generator, float progress )
        {
            Generator = generator;
            Progress = Mathf.Clamp(progress, 0, 1);
        }
    }    
}