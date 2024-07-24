using System.Collections.Generic;
using System.Collections.Immutable;

namespace TerrainTools
{
    public class TerrainToolModule
    {
        public class Builder
        {
            private readonly List<TerrainToolDefinition> _toolDefinitions = new();

            public void AddTool(TerrainToolDefinition toolDefinition)
            {
                _toolDefinitions.Add(toolDefinition);
            }

            public TerrainToolModule Build()
            {
                return new TerrainToolModule(_toolDefinitions);
            }
        }

        public ImmutableArray<TerrainToolDefinition> ToolDefinitions { get; }

        private TerrainToolModule(IEnumerable<TerrainToolDefinition> toolFragments)
        {
            ToolDefinitions = toolFragments.ToImmutableArray();
        }
    }    
}