using Timberborn.ToolSystem;

namespace TerrainTools
{
    public abstract class ITerrainTool : Tool
    {
        public virtual string Icon { get; } = "";

        public void SetToolGroup(ToolGroup toolGroup)
        {
            base.ToolGroup = toolGroup;
        }
    }
}