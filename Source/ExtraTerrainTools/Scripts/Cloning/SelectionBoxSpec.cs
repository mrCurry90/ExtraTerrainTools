using Timberborn.BlueprintSystem;
using UnityEngine;

namespace TerrainTools.Cloning
{
    internal record SelectionBoxSpec : ComponentSpec
    {
        [Serialize]
        public AssetRef<GameObject> BoxPrefab { get; init; }

        [Serialize]
        public Color BaseColor { get; init; }
    }
}