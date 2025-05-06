using Bindito.Core;

namespace TerrainTools.PathPainter
{
    [Context("MapEditor")]
	// [Context("Disabled")]
    public class PathPainterConfigurator : IConfigurator 
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
			containerDefinition.Bind<SplineDrawer>().AsSingleton();
			containerDefinition.Bind<PathPainterTool>().AsSingleton();
			containerDefinition.Bind<PathPainterToolPanel>().AsSingleton();

            containerDefinition.MultiBind<TerrainToolModule>().ToProvider<TerrainToolModuleProvider>().AsSingleton();
        }

        public class TerrainToolModuleProvider : IProvider<TerrainToolModule>
		{

			private readonly PathPainterTool _tool;
			private readonly PathPainterToolPanel _toolPanel;
			public TerrainToolModuleProvider(PathPainterTool tool, PathPainterToolPanel toolPanel)
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