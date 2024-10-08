using Timberborn.Coordinates;

namespace TerrainTools.EditorHistory
{
    internal class ObjectChangedEvent
    {
        public bool IsSet { get; set; }
        public string PrefabName { get; set; }
        public Placement Placement { get; set; }
        public float Growth { get; set; }
    }
}
        