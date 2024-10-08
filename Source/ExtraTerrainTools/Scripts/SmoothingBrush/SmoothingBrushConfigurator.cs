using Bindito.Core;

namespace TerrainTools.SmoothingBrush
{
    [Context("MapEditor")]
    public class SmoothingBrushConfigurator : IConfigurator 
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
			containerDefinition.Bind<SmoothingBrushTool>().AsSingleton();
			containerDefinition.Bind<SmoothingBrushToolPanel>().AsSingleton();

            containerDefinition.MultiBind<TerrainToolModule>().ToProvider<TerrainToolModuleProvider>().AsSingleton();
        }

        public class TerrainToolModuleProvider : IProvider<TerrainToolModule>
		{

			private readonly SmoothingBrushTool _tool;
			private readonly SmoothingBrushToolPanel _toolPanel;
			public TerrainToolModuleProvider(SmoothingBrushTool tool, SmoothingBrushToolPanel toolPanel)
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