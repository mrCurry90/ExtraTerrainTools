

namespace TerrainTools.NoiseGenerator
{
    public class GeneratorStartedEvent 
    {
        public NoiseGenerator Generator { get; }

        public GeneratorStartedEvent( NoiseGenerator generator )
        {
            Generator = generator;
        }
    }
}