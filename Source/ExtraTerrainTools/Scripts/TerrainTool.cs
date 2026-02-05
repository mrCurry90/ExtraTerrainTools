using Timberborn.Localization;
using Timberborn.ToolSystem;
using Timberborn.ToolSystemUI;

namespace TerrainTools
{
    public abstract class TerrainTool : ITool, IToolDescriptor
    {
        protected readonly ILoc _loc;

        public virtual string Icon { get; } = "";

        public abstract void Enter();

        public abstract void Exit();

        public abstract ToolDescription DescribeTool();

        private TerrainTool() { }

        protected TerrainTool(ILoc loc)
        {
            _loc = loc;
        }
    }
}