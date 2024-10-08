

namespace TerrainTools.NoiseGenerator
{
    public class GeneratorFinishedEvent 
    {
        public NoiseGenerator Generator { get; }

        public GeneratorFinishedEvent( NoiseGenerator generator )
        {
            Generator = generator;
        }
    }    
}