namespace TerrainTools
{
    public class TerrainToolDefinition
    {
        public ITerrainTool Tool { get; }
        public ITerrainToolFragment ToolPanel {get; }

        public TerrainToolDefinition(ITerrainTool tool, ITerrainToolFragment toolPanel)
        {
            Tool = tool;
            ToolPanel = toolPanel;
        }
    }
}