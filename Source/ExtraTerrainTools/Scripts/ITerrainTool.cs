using Timberborn.ToolSystem;

namespace TerrainTools
{
    public abstract class ITerrainTool : Tool
    {
        public virtual string Icon { get; } = "";
    }
}