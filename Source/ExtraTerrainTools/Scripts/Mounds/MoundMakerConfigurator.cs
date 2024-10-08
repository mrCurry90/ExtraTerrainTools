using Bindito.Core;

namespace TerrainTools.MoundMaker
{
    [Context("MapEditor")]
    public class MoundMakerConfigurator : IConfigurator 
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
			containerDefinition.Bind<MoundMakerTool>().AsSingleton();
			containerDefinition.Bind<MoundMakerToolPanel>().AsSingleton();

            containerDefinition.MultiBind<TerrainToolModule>().ToProvider<TerrainToolModuleProvider>().AsSingleton();
        }

        public class TerrainToolModuleProvider : IProvider<TerrainToolModule>
		{

			private readonly MoundMakerTool _tool;
			private readonly MoundMakerToolPanel _toolPanel;
			public TerrainToolModuleProvider(MoundMakerTool tool, MoundMakerToolPanel toolPanel)
			{
				_tool = tool;
				_toolPanel = toolPanel;
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