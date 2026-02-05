using Timberborn.Common;
using Timberborn.BlockSystem;
using Timberborn.TerrainSystem;
using UnityEngine;
using Timberborn.SingletonSystem;
using Timberborn.BlockObjectTools;
using Timberborn.EntitySystem;
using Timberborn.WaterSourceSystem;
using Timberborn.Coordinates;
using System;
using Timberborn.TemplateSystem;
using Timberborn.BlueprintSystem;
using Timberborn.InventorySystem;
using Timberborn.BaseComponentSystem;
using Timberborn.Stockpiles;
using Timberborn.Goods;
using System.Collections.Immutable;

namespace TerrainTools
{
    public class TerrainToolsManipulationService : ILoadableSingleton
    {
        private readonly IBlockService _blockService;
        private readonly ITerrainService _terrainService;
        private readonly BlockObjectPlacerService _placerService;
        private readonly EntityService _entityService;
        private readonly TemplateNameMapper _templateNameMapper;
        private readonly BlockValidator _blockValidator;

        public TerrainToolsManipulationService(
            IBlockService blockService, ITerrainService terrainService, BlockObjectPlacerService placerService,
            EntityService entityService, TemplateNameMapper templateNameMapper, BlockValidator blockValidator)
        {
            _blockService = blockService;
            _terrainService = terrainService;
            _placerService = placerService;
            _entityService = entityService;
            _templateNameMapper = templateNameMapper;
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
                                    _blockService.GetTopObjectComponentAt<BlockObject>(belowStart)?.PositionedBlocks.GetBlock(belowStart).Stackable == BlockStackable.BlockObject;

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
        public bool PlaceObject(string prefabName, Placement placement, Action<BaseComponent> placedCallback)
        {
            var prefab = GetBlockObjectSpec(prefabName);
            var placer = _placerService.GetMatchingPlacer(prefab);
            if (!_blockValidator.BlocksValid(prefab, placement))
                return false;

            placer.Place(prefab, placement, placedCallback);
            return true;
        }

        public void DeleteObject(BlockObject obj)
        {
            var waterSource = obj.GetComponent<WaterSource>();
            var entity = obj.GetComponent<EntityComponent>();

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
                if (obj.GetComponent<TemplateSpec>().TemplateName == name && (predicate == null || predicate(obj)))
                {
                    return obj;
                }
            }
            return null;
        }

        public TemplateSpec GetTemplateSpec(string templateName)
        {
            return _templateNameMapper.GetTemplate(templateName);
        }
        public BlockObjectSpec GetBlockObjectSpec(string templateName)
        {
            var spec = GetTemplateSpec(templateName)?.Blueprint.GetSpec<BlockObjectSpec>();
            return spec;
        }

        public T GetSpec<T>(TemplateSpec template) where T : ComponentSpec
        {
            return template.Blueprint.GetSpec<T>();
        }

        public T GetSpec<T>(string prefabName) where T : ComponentSpec
        {
            return GetSpec<T>(GetTemplateSpec(prefabName));
        }


        public void Set(Stockpile stockpile, string goodId, int amount)
        {
            // Clear existing
            ImmutableArray<GoodAmount>.Enumerator enumerator = stockpile.Inventory.Stock.ToImmutableArray().GetEnumerator();
            while (enumerator.MoveNext())
            {
                GoodAmount current = enumerator.Current;
                stockpile.Inventory.Take(current);
            }

            // Set new good
            stockpile.GetComponent<SingleGoodAllower>().Allow(goodId);
            stockpile.GetComponent<FixedStockpile>().SetFixedGood(goodId);

            // Give amount
            if (amount > 0)
            {
                stockpile.Inventory.Give(new GoodAmount(goodId, amount));
            }
        }
    }
}