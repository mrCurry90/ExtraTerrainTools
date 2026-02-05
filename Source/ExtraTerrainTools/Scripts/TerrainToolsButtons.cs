using System.Collections.Generic;
using Timberborn.BottomBarSystem;
using Timberborn.ToolButtonSystem;
using Timberborn.ToolSystem;

namespace TerrainTools
{
    public class TerrainToolsButtons : IBottomBarElementsProvider
    {
        private static readonly string ToolGroupSpecId = "ExtraTerrainTools.ToolGroup";
        private static readonly string ToolGroupSpecIconName = "TerrainToolsIcon";

        private readonly ToolButtonFactory _toolButtonFactory;
        private readonly ToolGroupButtonFactory _toolGroupButtonFactory;
        private readonly TerrainToolsManager _terrainToolsManager;
        private readonly ToolGroupService _toolGroupService;

        public TerrainToolsButtons(ToolButtonFactory toolButtonFactory, ToolGroupButtonFactory toolGroupButtonFactory, TerrainToolsManager terrainToolsManager, ToolGroupService toolGroupService)
        {
            _toolButtonFactory = toolButtonFactory;
            _toolGroupButtonFactory = toolGroupButtonFactory;
            _terrainToolsManager = terrainToolsManager;
            _toolGroupService = toolGroupService;
        }

        public IEnumerable<BottomBarElement> GetElements()
        {
            ToolGroupSpec toolGroupSpec = _toolGroupService.GetGroup(ToolGroupSpecId);
            ToolGroupButton toolGroupButton = _toolGroupButtonFactory.CreateGreen(toolGroupSpec);

            // Add tools
            foreach (TerrainTool tool in _terrainToolsManager.GetTools())
            {
                ToolButton button = _toolButtonFactory.Create(tool, tool.Icon != "" ? tool.Icon : ToolGroupSpecIconName, toolGroupButton.ToolButtonsElement);
                toolGroupButton.AddTool(button);
            }
            yield return BottomBarElement.CreateMultiLevel(toolGroupButton.Root, toolGroupButton.ToolButtonsElement);
        }
    }
}