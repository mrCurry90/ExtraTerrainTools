using Timberborn.TerrainSystem;
using UnityEngine;

namespace TerrainTools
{
    internal static class TerrainExtensions
    {
        public static void AdjustTerrain(this ITerrainService service, Vector3Int coordinate3D, int adjustBy)
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
                    service.UnsetTerrain(coordinate3D, Mathf.Abs(adjustBy) + 1);
                }
            }
            catch (System.Exception e)
            {
                Utils.Log("coord: {0}, adjustBy: {1}", coordinate3D, adjustBy);
                throw e;
            }
        }

        public static int CellToIndex(this ITerrainService service, Vector2Int coordinates)
        {
            // Source: Timberborn.MapIndexSystem.MapIndexService.Load()
            int stride = service.Size.x + 2;                    

            // Source: Timberborn.MapIndexSystem.MapIndexService.CellToIndex()
            return (coordinates.y + 1) * stride + coordinates.x + 1;
        }

        /// <summary>
        /// Get the highest terrain in a given terrain column. Warning: Providate coordinates must be within terrain bounds
        /// </summary>
        /// <param name="service">The ITerrainService</param>
        /// <param name="coordinates">Coordinates</param>
        /// <returns>absolut height</returns>
        public static int CellHeight(this ITerrainService service, Vector2Int coordinates)
        {
            return service.GetColumnCeiling(service.CellToIndex(coordinates));
        }
    }
}