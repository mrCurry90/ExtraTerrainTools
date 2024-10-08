using Bindito.Core;

namespace TerrainTools.EditorHistory
{
    [Context("MapEditor")]
    public class TerrainToolsConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            containerDefinition.Bind<EditorHistoryService>().AsSingleton();
        }
    }    
}

