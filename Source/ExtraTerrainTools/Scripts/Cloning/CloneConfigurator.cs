using Bindito.Core;

namespace TerrainTools.Cloning
{
	[Context("MapEditor")]
	public class CloneConfigurator : IConfigurator
	{
		public void Configure(IContainerDefinition containerDefinition)
		{
			containerDefinition.Bind<CloneTool>().AsSingleton();
			containerDefinition.Bind<CloneToolPanel>().AsSingleton();

			containerDefinition.MultiBind<TerrainToolModule>().ToProvider<TerrainToolModuleProvider>().AsSingleton();
		}

		public class TerrainToolModuleProvider : IProvider<TerrainToolModule>
		{

			private readonly CloneTool _tool;
			private readonly CloneToolPanel _toolPanel;
			public TerrainToolModuleProvider(CloneTool tool, CloneToolPanel toolPanel)
			{
				_tool = tool;
				_toolPanel = toolPanel;
			}

			public TerrainToolModule Get()
			{
				TerrainToolModule.Builder builder = new();

				builder.AddTool(new TerrainToolDefinition(_tool, _toolPanel));

				return builder.Build();
			}
		}
	}
}