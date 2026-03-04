using Bindito.Core;


namespace TerrainTools.Erosion
{
    // [Context("MapEditor")]
    [Context("Disabled")]
    public class ErosionToolConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            containerDefinition.Bind<ErosionTool>().AsSingleton();
            containerDefinition.Bind<ErosionToolPanel>().AsSingleton();

            containerDefinition.MultiBind<TerrainToolModule>().ToProvider<TerrainToolModuleProvider>().AsSingleton();
        }

        public class TerrainToolModuleProvider : IProvider<TerrainToolModule>
        {

            private readonly ErosionTool _tool;
            private readonly ErosionToolPanel _toolPanel;
            public TerrainToolModuleProvider(ErosionTool tool, ErosionToolPanel toolPanel)
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