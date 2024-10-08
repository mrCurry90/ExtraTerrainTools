using System.Collections.Generic;
using Timberborn.BottomBarSystem;
using Timberborn.ToolSystem;

namespace TerrainTools 
{
    public class TerrainToolsButtons : IBottomBarElementsProvider {
        private static readonly string ToolGroupNameKey = "TerrainTools.ToolGroupButton";
        private static readonly string ToolGroupSpecIconName = "TerrainToolsIconCol";//"TerrainToolsIcon";

        private readonly ToolButtonFactory _toolButtonFactory;
        private readonly ToolGroupButtonFactory _toolGroupButtonFactory;
        private readonly TerrainToolsManager _terrainToolsManager;

        public TerrainToolsButtons(ToolButtonFactory toolButtonFactory, ToolGroupButtonFactory toolGroupButtonFactory, TerrainToolsManager terrainToolsManager) {
            _toolButtonFactory = toolButtonFactory;
            _toolGroupButtonFactory = toolGroupButtonFactory;
            _terrainToolsManager = terrainToolsManager;
        }

        public IEnumerable<BottomBarElement> GetElements()
        {        
            // Create tool group button
            TerrainToolsToolGroup toolGroup = new( ToolGroupNameKey, ToolGroupSpecIconName );
            ToolGroupButton toolGroupButton = _toolGroupButtonFactory.CreateBlue(toolGroup);

            // Add tools
            foreach (ITerrainTool tool in _terrainToolsManager.GetTools())
            {
                    var icon = tool.Icon != "" ? tool.Icon : ToolGroupSpecIconName;
                    tool.SetToolGroup(toolGroup);
                    ToolButton button = _toolButtonFactory.Create( tool, icon, toolGroupButton.ToolButtonsElement);
                    toolGroupButton.AddTool(button);
            }
            yield return BottomBarElement.CreateMultiLevel(toolGroupButton.Root, toolGroupButton.ToolButtonsElement);
        }

        // Tool Group definition class
        public class TerrainToolsToolGroup : ToolGroup
        {   
            private string _iconName;
            public override string IconName => _iconName;
            public TerrainToolsToolGroup(string nameLocKey, string iconName)
            {
                DisplayNameLocKey = nameLocKey;
                _iconName = iconName;
            }
        }

    }
}  