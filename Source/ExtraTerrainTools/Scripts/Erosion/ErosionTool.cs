using Timberborn.Localization;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
using Timberborn.ToolSystemUI;
using UnityEngine;

namespace TerrainTools.Erosion
{
    public class ErosionTool : TerrainTool, ILoadableSingleton
    {
        private readonly ITerrainService _terrainService;
        private readonly TerrainToolsManipulationService _manipulationService;
        private HydraulicErosion _hydraulicErosion;

        public ErosionTool(ITerrainService terrainService, TerrainToolsManipulationService manipulationService, ILoc loc) : base(loc)
        {
            _terrainService = terrainService;
            _manipulationService = manipulationService;
        }

        public override ToolDescription DescribeTool()
        {
            var builder = new ToolDescription.Builder("Erosion Tool");
            return builder.Build();
        }

        public override void Enter()
        {
            Utils.Log("Entered ErosionTool");
        }

        public override void Exit()
        {
            Utils.Log("Exited ErosionTool");
        }

        public void Load()
        {
            _hydraulicErosion = new HydraulicErosion(_terrainService, _manipulationService);
        }

        public void Apply(float rainAmount, float evaporation, float erosionRate, float depositionRate, float capacityConstant, int simulationSteps)
        {
            Utils.Log("Applying erosion...");
            Vector2Int size = new(_terrainService.Size.x, _terrainService.Size.y);
            RectInt _area = new(Vector2Int.one, size);
            _hydraulicErosion.Initialize(
                _area,
                simulationSteps,
                rainAmount,
                evaporation,
                erosionRate,
                depositionRate,
                capacityConstant
            );
            var time = Time.realtimeSinceStartup;
            Utils.Log("Simulating...");
            _hydraulicErosion.Simulate();
            Utils.Log($"Simulation: {Time.realtimeSinceStartup - time} seconds");
            time = Time.realtimeSinceStartup;
            Utils.Log("Applying results...");
            _hydraulicErosion.ApplyResults();
            Utils.Log($"Applied results in: {Time.realtimeSinceStartup - time} seconds");
        }
    }
}