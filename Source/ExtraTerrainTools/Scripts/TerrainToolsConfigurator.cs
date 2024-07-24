using Bindito.Core;
using Timberborn.BottomBarSystem;
using Timberborn.ToolPanelSystem;

namespace TerrainTools 
{
	[Context("MapEditor")]
	public class TerrainToolsConfigurator : IConfigurator {
		public void Configure(IContainerDefinition containerDefinition) {
			// TerrainTools manager
			containerDefinition.Bind<TerrainToolsManager>().AsSingleton();

			// Service bindings
			containerDefinition.Bind<ResetService>().AsSingleton();
			containerDefinition.Bind<TerrainToolPanelFactory>().AsSingleton();

			// BottomBarModule bindings
			containerDefinition.Bind<TerrainToolsButtons>().AsSingleton();
			containerDefinition.MultiBind<BottomBarModule>().ToProvider<BottomBarModuleProvider>().AsSingleton();

			// ToolPanelModule bindings			
			containerDefinition.MultiBind<ToolPanelModule>().ToProvider<ToolPanelModuleProvider>().AsSingleton();
		}

		public class ToolPanelModuleProvider : IProvider<ToolPanelModule>
		{
			private readonly TerrainToolsManager _toolManager;
			public ToolPanelModuleProvider(TerrainToolsManager terrainToolsManager)
			{
				_toolManager = terrainToolsManager;
			}

			public ToolPanelModule Get()
			{
				ToolPanelModule.Builder builder = new();

				foreach( var panel in _toolManager.GetToolPanels() )
				{
					builder.AddFragment(panel, panel.Order);
				}

				return builder.Build();
			}
		}

		public class BottomBarModuleProvider : IProvider<BottomBarModule>
		{	
			private readonly TerrainToolsButtons _terrainToolButtons;
			public BottomBarModuleProvider(
        		TerrainToolsButtons terrainToolButtons
			) {
				_terrainToolButtons = terrainToolButtons;
			}
			public BottomBarModule Get()
			{
				var builder = new BottomBarModule.Builder();
				builder.AddMiddleSectionElements(_terrainToolButtons);
				return builder.Build();
			}
		}

	}
}