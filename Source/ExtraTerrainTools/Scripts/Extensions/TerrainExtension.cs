using Timberborn.TerrainSystem;
using UnityEngine;

namespace TerrainTools
{
    internal static class TerrainExtensions
    {
        public static void AdjustTerrain(this ITerrainService service, Vector3Int coordinate3D, int adjustBy )
        {
            if (adjustBy == 0)
                return;

            try
            {
                if (adjustBy > 0)
                {
                    service.SetTerrain(coordinate3D, adjustBy);
                }
                else
                {
                    service.UnsetTerrain(coordinate3D, Mathf.Abs(adjustBy)+1);
                }
            }
            catch (System.Exception e)
            {
                Utils.Log("coord: {0}, adjustBy: {1}", coordinate3D, adjustBy);
                throw e;
            }
        }
    }
}