using Timberborn.Common;
using Timberborn.BlockSystem;
using Timberborn.TerrainSystem;
using UnityEngine;
using Timberborn.SingletonSystem;
using Timberborn.BlockObjectTools;
using Timberborn.EntitySystem;
using Timberborn.PrefabSystem;
using Timberborn.WaterSourceSystem;
using Timberborn.Coordinates;
using Timberborn.Growing;
using System;
using Timberborn.BaseComponentSystem;

namespace TerrainTools
{
    public class TerrainToolsManipulationService : ILoadableSingleton
    {
        private readonly IBlockService _blockService;
        private readonly ITerrainService _terrainService;
        private readonly BlockObjectPlacerService _placerService;
        private readonly EntityService _entityService;
        private readonly PrefabNameMapper _prefabNameMapper;
        private readonly BlockValidator _blockValidator;

        public TerrainToolsManipulationService(
            IBlockService blockService, ITerrainService terrainService, BlockObjectPlacerService placerService,
            EntityService entityService, PrefabNameMapper prefabNameMapper, BlockValidator blockValidator)
        {
            _blockService = blockService;
            _terrainService = terrainService;
            _placerService = placerService;
            _entityService = entityService;
            _prefabNameMapper = prefabNameMapper;
            _blockValidator = blockValidator;
        }

        public void Load()
        {
            // No initialization needed
        }

        /////// Terrain manipulation ///////
        public void AdjustTerrain(Vector3Int coordinate3D, int adjustBy)
        {
            if (adjustBy == 0)
                return;

            try
            {
                if (adjustBy > 0)
                {
                    _terrainService.SetTerrain(coordinate3D, adjustBy);
                }
                else
                {
                    _terrainService.UnsetTerrain(coordinate3D, Mathf.Abs(adjustBy) + 1);
                }
            }
            catch (System.Exception e)
            {
                Utils.Log("coord: {0}, adjustBy: {1}", coordinate3D, adjustBy);
                throw e;
            }
        }

        public bool CanAdjustTerrain(Vector3Int startCoord, int adjustBy)
        {
            if (adjustBy > 0)
                return CanAddTerrain(startCoord, adjustBy);

            else if (adjustBy < 0)
                return CanRemoveTerrain(startCoord);

            return true; // No change = Ok
        }

        public bool CanAddTerrain(Vector3Int startCoord, int height)
        {
            if (height <= 0) return false;

            var noObjectAtStart = !_blockService.AnyObjectAt(startCoord);
            var belowStart = startCoord.Below();
            var startIsSupported = _terrainService.Underground(belowStart)
                                    ||
                                    _blockService.GetTopObjectAt(belowStart)?.PositionedBlocks.GetBlock(belowStart).Stackable == BlockStackable.BlockObject;

            for (var i = 1; i <= height; i++)
            {
                var coord = startCoord.Above();
                if (_blockService.AnyObjectAt(coord))
                {
                    return false;
                }
            }

            return noObjectAtStart && startIsSupported;
        }

        public bool CanRemoveTerrain(Vector3Int startCoord)
        {
            var aboveStart = startCoord.Above();
            return !_blockService.BlockNeedsGroundBelow(startCoord) && !_blockService.BlockNeedsGroundBelow(aboveStart);
        }

        /////// Object manipulation ///////
        public bool PlaceObject(string prefabName, Placement placement, float growth = -1)
        {
            var prefab = GetBlockObjectSpec(prefabName);
            var placer = _placerService.GetMatchingPlacer(prefab);
            if (!_blockValidator.BlocksValid(prefab, placement))
                return false;

            placer.Place(prefab, placement);

            var blockObject = GetFirstObjectAt(prefabName, placement.Coordinates);
            if (blockObject != null)
            {
                var growable = blockObject.GetComponentFast<Growable>();
                if (growable != null && growth >= 0)
                {
                    growable.IncreaseGrowthProgress(growth);
                }
            }

            return true;
        }

        public void DeleteObject(BlockObject obj)
        {
            var waterSource = obj.GetComponentFast<WaterSource>();
            var entity = obj.GetComponentFast<EntityComponent>();

            if (waterSource != null)
                waterSource.DeleteEntity();

            obj.DeleteEntity();

            _entityService.Delete(entity);
        }

        /////// Object queries ///////
        public BlockObject GetFirstObjectAt(string name, Vector3Int coord, Predicate<BlockObject> predicate = null)
        {
            foreach (var obj in _blockService.GetObjectsAt(coord))
            {
                if (obj.GetComponentFast<PrefabSpec>().PrefabName == name && (predicate == null || predicate(obj)))
                {
                    return obj;
                }
            }
            return null;
        }
        public PrefabSpec GetPrefabSpec(string prefabName)
        {
            return _prefabNameMapper.GetPrefab(prefabName);
        }
        public BlockObjectSpec GetBlockObjectSpec(string prefabName)
        {
            // TODO Doesn't fit purpose of class - investigate where it belongs
            GetPrefabSpec(prefabName).TryGetComponentFast<BlockObjectSpec>(out var spec);
            return spec;
        }

        public T GetPrefabComponent<T>(PrefabSpec prefab) where T : BaseComponent
        {
            if (prefab.TryGetComponentFast(out T component))
                return component;
            return default;
        }

        public T GetPrefabComponent<T>(string prefabName) where T : BaseComponent
        {
            return GetPrefabComponent<T>(GetPrefabSpec(prefabName));
        }
    }
}