using Timberborn.BlueprintSystem;
using UnityEngine;


namespace TerrainTools.PathPainter
{
    internal record SplineDrawerSpec : ComponentSpec
    {
        [Serialize]
        public AssetRef<GameObject> LinePointPrefab { get; init; }
        [Serialize]
        public AssetRef<LineRenderer> DashedLinePrefab { get; init; }
        [Serialize]
        public AssetRef<LineRenderer> SolidLinePrefab { get; init; }

        [Serialize]
        public Color BaseColor { get; init; }
        [Serialize]
        public Color HoverColor { get; init; }
        [Serialize]
        public Color HighlightColor { get; init; }
    }
}