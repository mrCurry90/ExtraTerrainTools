using Timberborn.SingletonSystem;
using Timberborn.Localization;
using Timberborn.UndoSystem;
using Timberborn.ToolSystemUI;

namespace TerrainTools.NoiseGenerator
{
    public class NoiseGeneratorTool : TerrainTool, ILoadableSingleton
    {
        private readonly string _keyToolTitle = "TerrainTools.Heightmap.Tool.Title"; // Heightmap Generator
        private static readonly string _keyWarningText = "TerrainTools.Heightmap.Tool.Warning"; // Changes affect the entire map.
        public override string Icon { get; } = "HeightmapToolIcon";

        private ToolDescription _toolDescription;

        public readonly NoiseGenerator _noiseGenerator;
        private readonly IUndoRegistry _undoRegistry;

        public NoiseGeneratorTool(NoiseGenerator noiseGenerator, IUndoRegistry undoRegistry, ILoc loc) : base(loc)
        {
            _noiseGenerator = noiseGenerator;
            _undoRegistry = undoRegistry;
        }

        public void Load()
        {
            var _builder = new ToolDescription.Builder(_loc.T(_keyToolTitle));
            // _builder.AddPrioritizedSection("Create a heightmap with a single click");
            _builder.AddPrioritizedSection("<color=#FFA500>" + _loc.T(_keyWarningText) + "</color>");
            _toolDescription = _builder.Build();
        }

        public override ToolDescription DescribeTool()
        {
            return _toolDescription;
        }

        public void GenerateHeightMap(NoiseParameters parameters, bool clear)
        {
            var mode = clear ? NoiseGenerator.UpdateMode.ClearExisting : NoiseGenerator.UpdateMode.UpdateExisting;
            _noiseGenerator.Generate(parameters, mode);
            _undoRegistry.CommitStack();
        }

        public override void Enter()
        {

        }

        public override void Exit()
        {
            _undoRegistry.CommitStack();
        }
    }
}