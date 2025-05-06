using Timberborn.Localization;
using Timberborn.ToolSystem;

namespace TerrainTools
{
    public abstract class ITerrainTool : Tool
    {
        protected readonly ILoc _loc;

        public virtual string Icon { get; } = "";

        public void SetToolGroup(ToolGroup toolGroup)
        {
            base.ToolGroup = toolGroup;
        }

        private ITerrainTool() { }
        
        protected ITerrainTool(ILoc loc)
        {
            _loc = loc;
        }     
    }
}