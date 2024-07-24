using Bindito.Core;
using Timberborn.Beavers;

namespace TerrainTools.NoiseGenerator
{
	[Context("MapEditor")]
	public class NoiseGeneratorConfigurator : IConfigurator {
		public void Configure(IContainerDefinition containerDefinition) {
			// Tool bindings
			containerDefinition.Bind<NoiseGenerator>().AsSingleton();
			containerDefinition.Bind<NoiseGeneratorTool>().AsSingleton();
			containerDefinition.Bind<NoiseGeneratorToolPanel>().AsSingleton();
			containerDefinition.Bind<BeaverNameService>().AsSingleton();

			containerDefinition.MultiBind<TerrainToolModule>().ToProvider<TerrainToolModuleProvider>().AsSingleton();
		}

		public class TerrainToolModuleProvider : IProvider<TerrainToolModule>
		{

			private readonly NoiseGeneratorTool _tool;
			private readonly NoiseGeneratorToolPanel _toolPanel;
			public TerrainToolModuleProvider(NoiseGeneratorTool noiseGeneratorTool, NoiseGeneratorToolPanel noiseGeneratorToolPanel)
			{
				_tool = noiseGeneratorTool;
				_toolPanel = noiseGeneratorToolPanel;
			}

			public TerrainToolModule Get()
			{
				TerrainToolModule.Builder builder = new();

				builder.AddTool( new TerrainToolDefinition(_tool, _toolPanel) );

				return builder.Build();
			}
		}
	}
}