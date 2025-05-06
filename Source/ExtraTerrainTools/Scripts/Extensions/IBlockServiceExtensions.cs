using System.Collections.Generic;
using System.Linq;
using Timberborn.BlockSystem;
using UnityEngine;

public static class IBlockServiceExtensions
{
    public static IEnumerable<BlockObject> GetObjectsAtColumn(this IBlockService blockService, Vector2Int coordinates, int startHeight, int endHeight)
    {
        int ceil = blockService.Size.z - 1;
        startHeight = Mathf.Clamp(startHeight, 0, ceil);
        endHeight = Mathf.Clamp(endHeight, 0, ceil);

        for (int z = startHeight; z < endHeight; z++)
        {
            foreach (var block in blockService.GetObjectsAt(new Vector3Int(coordinates.x, coordinates.y, z)))
            {
                yield return block;
            }
        }
    }

    public static bool AnyObjectAtColumn(this IBlockService blockService, Vector2Int coordinates, int startHeight, int endHeight)
    {
        return blockService.GetObjectsAtColumn(coordinates, startHeight, endHeight).Count() > 0;
    }
}