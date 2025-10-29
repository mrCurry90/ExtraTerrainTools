namespace TerrainTools
{
    public class TerrainToolDefinition
    {
        public TerrainTool Tool { get; }
        public TerrainToolFragment ToolPanel { get; }

        public TerrainToolDefinition(TerrainTool tool, TerrainToolFragment toolPanel)
        {
            Tool = tool;
            ToolPanel = toolPanel;
        }
    }
}