using Timberborn.Localization;
using Timberborn.ToolSystem;

namespace TerrainTools
{
    public abstract class TerrainTool : Tool
    {
        protected readonly ILoc _loc;

        public virtual string Icon { get; } = "";

        public void SetToolGroup(ToolGroup toolGroup)
        {
            base.ToolGroup = toolGroup;
        }

        private TerrainTool() { }

        protected TerrainTool(ILoc loc)
        {
            _loc = loc;
        }
    }
}