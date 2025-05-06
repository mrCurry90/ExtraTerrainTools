using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using TerrainTools.EditorHistory;
using Timberborn.Localization;

namespace TerrainTools.NoiseGenerator
{
    public class NoiseGeneratorTool : ITerrainTool, ILoadableSingleton
    {
        private readonly string _keyToolTitle = "TerrainTools.Heightmap.Tool.Title"; // Heightmap Generator
        private static readonly string _keyWarningText = "TerrainTools.Heightmap.Tool.Warning"; // Changes affect the entire map.
        public override string Icon { get; } = "HeightmapToolIcon";

        private ToolDescription _toolDescription;

        public readonly NoiseGenerator _noiseGenerator;
        private readonly EditorHistoryService _historyService;

        public NoiseGeneratorTool(NoiseGenerator noiseGenerator, EditorHistoryService historyService, ILoc loc) : base(loc)
        {
            _noiseGenerator = noiseGenerator;
            _historyService = historyService;
        }

        public void Load()
        {
            var _builder = new ToolDescription.Builder(_loc.T(_keyToolTitle));
            // _builder.AddPrioritizedSection("Create a heightmap with a single click");
            _builder.AddPrioritizedSection("<color=#FFA500>" + _loc.T(_keyWarningText) + "</color>");
            _toolDescription = _builder.Build();
        }

        public override ToolDescription Description()
        {
            return _toolDescription;
        }

        public override string WarningText()
        {
            return "";
        }

        public void GenerateHeightMap(NoiseParameters parameters, bool clear)
        {
            var mode = clear ? NoiseGenerator.UpdateMode.ClearExisting : NoiseGenerator.UpdateMode.UpdateExisting;
            _historyService.BatchStart();
            _noiseGenerator.Generate(parameters, mode);
            _historyService.BatchStop();
        }

        public override void Enter()
        {

        }

        public override void Exit()
        {

        }
    }
}