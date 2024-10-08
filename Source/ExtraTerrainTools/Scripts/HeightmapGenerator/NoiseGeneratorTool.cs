using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using TerrainTools.EditorHistory;

namespace TerrainTools.NoiseGenerator
{
    public class NoiseGeneratorTool : ITerrainTool, ILoadableSingleton
    {
        public static readonly string _toolTitle = "Heightmap Generator";
        private ToolDescription _toolDescription;

        public readonly NoiseGenerator _noiseGenerator;
        private readonly EditorHistoryService _historyService;

        public NoiseGeneratorTool( NoiseGenerator noiseGenerator, EditorHistoryService historyService )
        {
            _noiseGenerator = noiseGenerator;
            _historyService = historyService;
        }

        public void Load()
        {
            var _builder = new ToolDescription.Builder(_toolTitle);
            // _builder.AddPrioritizedSection("Create a heightmap with a single click");
            _builder.AddPrioritizedSection("<color=#FFA500>Changes affect the entire map.</color>");
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