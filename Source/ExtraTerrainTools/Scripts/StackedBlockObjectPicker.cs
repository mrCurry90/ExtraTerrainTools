using System.Collections.Generic;
using System.Linq;
using Timberborn.BlockSystem;
using UnityEngine;
using Timberborn.Common;
namespace TerrainTools
{
    internal class StackedBlockObjectPicker
    {
        internal enum BlockObjectPickDirection
        {
            Upwards,
            Downwards
        }

        internal readonly struct BlockObjectPickerFilter
        {
            private readonly int _referenceZ;

            private readonly bool _ignoreTopBlockObjectsOnBaseZ;

            private readonly bool _ignoreBottomBlockObjectsOnBaseZ;

            public BlockObjectPickerFilter(int referenceZ, bool ignoreTopBlockObjectsOnBaseZ, bool ignoreBottomBlockObjectsOnBaseZ)
            {
                _referenceZ = referenceZ;
                _ignoreTopBlockObjectsOnBaseZ = ignoreTopBlockObjectsOnBaseZ;
                _ignoreBottomBlockObjectsOnBaseZ = ignoreBottomBlockObjectsOnBaseZ;
            }

            public bool IsValid(BlockObject blockObject)
            {
                if (blockObject.CoordinatesAtBaseZ.z == _referenceZ && (!_ignoreTopBlockObjectsOnBaseZ || !HasTopOccupationOnBaseZ(blockObject, _referenceZ)))
                {
                    if (_ignoreBottomBlockObjectsOnBaseZ)
                    {
                        return !HasBottomOccupationOnBaseZ(blockObject, _referenceZ);
                    }
                    return true;
                }
                return false;
            }

            private static bool HasBottomOccupationOnBaseZ(BlockObject blockObject, int baseZ)
            {
                return (from block in blockObject.PositionedBlocks.GetAllBlocks()
                    where block.Coordinates.z == baseZ
                    select block).Any((Block block) => block.Occupation.IsBottomOrFloorOrBoth());
            }

            private static bool HasTopOccupationOnBaseZ(BlockObject blockObject, int baseZ)
            {
                return (from block in blockObject.PositionedBlocks.GetAllBlocks()
                    where block.Coordinates.z == baseZ
                    select block).Any((Block block) => block.Occupation.IsTopOrCornersOrBoth());
            }
        }

        private readonly HashSet<BlockObject> _blockObjects = new HashSet<BlockObject>();

        private readonly AreaIterator _areaIterator;

        private readonly BlockService _blockService;

        public StackedBlockObjectPicker(AreaIterator areaIterator, BlockService blockService)
        {
            _areaIterator = areaIterator;
            _blockService = blockService;
        }

        internal IEnumerable<BlockObject> GetBlockObjectAndStacked(BlockObject startBlockObject, BlockObjectPickDirection pickDirection, BlockObjectPickerFilter selectionFilter)
        {
            _blockObjects.Clear();
            if (startBlockObject != null && selectionFilter.IsValid(startBlockObject))
            {
                AddBlockObjectsRecursively(startBlockObject, pickDirection);
            }
            return _blockObjects.AsReadOnlyEnumerable();
        }

        internal IEnumerable<BlockObject> GetBlockObjectsInAreaAndStacked(Vector3Int start, Vector3Int end, BlockObject startBlockObject, BlockObjectPickDirection pickDirection, BlockObjectPickerFilter selectionFilter)
        {
            _blockObjects.Clear();
            if (startBlockObject != null && selectionFilter.IsValid(startBlockObject))
            {
                AddBlockObjectsRecursively(startBlockObject, pickDirection);
            }
            foreach (BlockObject item in GetBlockObjectsInCuboid(start, end).Where(selectionFilter.IsValid))
            {
                AddBlockObjectsRecursively(item, pickDirection);
            }
            return _blockObjects.AsReadOnlyEnumerable();
        }

        private IEnumerable<BlockObject> GetBlockObjectsInCuboid(Vector3Int start, Vector3Int end)
        {
            return (from coords in _areaIterator.GetCuboid(start, end)
                where _blockService.AnyObjectAt(coords)
                select coords).SelectMany((Vector3Int coords) => _blockService.GetObjectsAt(coords)).Distinct();
        }

        private void AddBlockObjectsRecursively(BlockObject blockObject, BlockObjectPickDirection pickDirection)
        {
            if (_blockObjects.Add(blockObject))
            {
                AddConnectedBlockObjects(blockObject, pickDirection);
            }
        }

        private void AddConnectedBlockObjects(BlockObject blockObject, BlockObjectPickDirection pickDirection)
        {
            foreach (Block item in from block in blockObject.PositionedBlocks.GetAllBlocks()
                where (pickDirection != BlockObjectPickDirection.Downwards) ? block.Stackable.IsStackable() : block.IsFoundationBlock
                select block)
            {
                AddValidBlockObjectsStackedWithBlock(item, pickDirection);
            }
        }

        private void AddValidBlockObjectsStackedWithBlock(Block block, BlockObjectPickDirection pickDirection)
        {
            int z = (pickDirection == BlockObjectPickDirection.Upwards) ? 1 : (-1);
            Vector3Int coordinates = block.Coordinates + new Vector3Int(0, 0, z);
            foreach (BlockObject item in _blockService.GetObjectsAt(coordinates))
            {
                if (ShouldIncludeNearBlock(item.PositionedBlocks.GetBlock(coordinates), pickDirection))
                {
                    AddBlockObjectsRecursively(item, pickDirection);
                }
            }
        }

        private static bool ShouldIncludeNearBlock(Block block, BlockObjectPickDirection direction)
        {
            if (direction != 0)
            {
                return block.Stackable.IsStackable();
            }
            return block.IsFoundationBlock;
        }
    }
}