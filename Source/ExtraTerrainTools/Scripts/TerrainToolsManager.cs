using System.Collections.Generic;
using System.Collections.Immutable;
using Timberborn.SingletonSystem;

namespace TerrainTools
{
    public class TerrainToolsManager : ILoadableSingleton
    {
        private ImmutableArray<TerrainToolModule> _toolModules;
        private List<ITerrainTool> _registeredTools;
        private List<ITerrainToolFragment> _registeredToolPanels;

        // Handles which tools are in the Terrain Tools set
        public TerrainToolsManager(IEnumerable<TerrainToolModule> toolModules) {
            _registeredTools = new();
            _registeredToolPanels = new();
            _toolModules = toolModules.ToImmutableArray();

            Utils.Log( "Loading tools..." );

            foreach (var mod in _toolModules)
            {
                foreach (var def in mod.ToolDefinitions)
                {
                    Utils.Log( "Tool: " + def.Tool?.GetType() + " ToolPanel: " + def.ToolPanel?.GetType() );
                    _registeredTools.Add(def.Tool);
                    if( def.ToolPanel != null )
                        _registeredToolPanels.Add(def.ToolPanel);
                }
            }
        }

        public void Load()
        {
        }

        public ImmutableArray<ITerrainTool> GetTools()
        {
            return _registeredTools.ToImmutableArray();
        }

        public ImmutableArray<ITerrainToolFragment> GetToolPanels()
        {
            return _registeredToolPanels.ToImmutableArray();
        }
    }
}