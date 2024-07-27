using System.Collections.Generic;
using System.Linq;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.EntitySystem;
using Timberborn.MapEditorSimulationSystem;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using Object = UnityEngine.Object;
using Timberborn.WaterSourceSystem;

namespace TerrainTools
{
    public class ResetService : ILoadableSingleton
    {
        private readonly BlockService _blockService;
        private readonly EntityRegistry _entityRegistry;
        private readonly EntityService _entityService;
        private readonly ITerrainService _terrainService;
        private readonly MapEditorSimulation _mapEditorSim;
        private VisualElement _mapEditorSimPanel = null;

        public ResetService(
            BlockService blockService,
            EntityRegistry entityRegistry,
            EntityService entityService,
            ITerrainService terrainService,
            MapEditorSimulation mapEditorSim

        ) {
            _blockService = blockService;
            _entityRegistry = entityRegistry;
            _entityService = entityService;
            _mapEditorSim = mapEditorSim;
            _terrainService = terrainService;
        }

        private int storedSimSpeed = -1;

        public void Load()
        {
        }        

        public void ClearEntities() {
            foreach (var e in GetEntityList())
            {
                _entityService.Delete(e);
            }
        }

        
        public void ClearWaterSources()
        {
            _mapEditorSim.ResetSimulation();

            foreach (var e in GetEntityList())
            {
                var entityBlockObject = e.GetComponentFast<BlockObject>();
                var entityIsWaterSource = e.GetComponentFast<WaterSource>() != null;
                var enityCanContaminate = e.GetComponentFast<WaterSourceContamination>() != null;

                if(entityIsWaterSource)
                    Utils.Log(entityBlockObject + " has water source.");
                if(enityCanContaminate)
                    Utils.Log(entityBlockObject + " can contaminate.");

                if( entityBlockObject != null && entityIsWaterSource && enityCanContaminate)
                {
                    Utils.Log(entityBlockObject + " is deleted.");
                    _entityService.Delete(entityBlockObject);
                }
            }
        }

        public void UpdateEntities() {
            // Scan entities            
            var offset = new Vector3Int(0, 0, 1);
            foreach (var e in GetEntityList())
            {
                var entityBlockObject = e.GetComponentFast<BlockObject>();
                if (entityBlockObject != null && !_blockService.AnyTopObjectAt(entityBlockObject.Coordinates - offset))
                {
                    var stacked = GetBlockObjectAndStacked( entityBlockObject, true );
                   
                    // All other objects are vertical so should allow moving them up or down
                    int deltaZ = _terrainService.CellHeight((Vector2Int)entityBlockObject.Coordinates) - entityBlockObject.CoordinatesAtBaseZ.z;
                    foreach (var blockObject in stacked)
                    {
                        var c = blockObject.Coordinates;
                        if( !AdjustBlockObjectHeight(blockObject, deltaZ) )
                        {
                            Utils.Log("Could not adjust {0} at {1} by {2}. Entity deleted.", blockObject, c, deltaZ);
                            _entityService.Delete(blockObject);
                        }
                    }
                }
            }
        }

        public bool PauseEditorSim()
        {
            if (storedSimSpeed >= 0)
                return false;

            storedSimSpeed = _mapEditorSim.SimulationSpeed;
            _mapEditorSim.ResetSimulation();
            _mapEditorSim.SetSimulationSpeed(0);
            ToggleSimPanel(false);
            return true;
        }

        public bool UnpauseEditorSim()
        {
            if (storedSimSpeed < 0)
                return false;

            _mapEditorSim.ResetSimulation();
            _mapEditorSim.SetSimulationSpeed(storedSimSpeed);
            storedSimSpeed = -1;
            ToggleSimPanel(true);

            return true;
        }

        private void ToggleSimPanel(bool state)
        {
            if( _mapEditorSimPanel == null ) 
            {
                // Super hacky search for the sim panel
                var uIDocuments = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
                foreach (var uiDoc in uIDocuments)
                {
                    var result = uiDoc.rootVisualElement.Q("MapEditorSimulationPanel");
                    if(result != null)
                    {
                        _mapEditorSimPanel = result;
                    }
                }
            }

            _mapEditorSimPanel?.SetEnabled(state);
        }
     
        private List<EntityComponent> GetEntityList()
        {
            return _entityRegistry.Entities.ToList();
        }

        private bool AdjustBlockObjectHeight(BlockObject blockObject, int heightAdjustment)
        {
            var coord = blockObject.Placement.Coordinates;
            coord.z += heightAdjustment;
            if (!_blockService.Contains(coord))
                return false;
            
            try
            {
                blockObject.Reposition( new(
                    coord, blockObject.Placement.Orientation, blockObject.Placement.FlipMode
                ));
            }
            catch(InvalidOperationException)
            {
                return false;
            }
            
            return true;
        }

        private IEnumerable<BlockObject> GetBlockObjectAndStacked(BlockObject startBlockObject, bool upward)
        {
            HashSet<BlockObject> _blockObjects = new();
            if (startBlockObject != null && IsValid(startBlockObject, startBlockObject.Coordinates.z))
            {
                AddBlockObjectsRecursively(_blockObjects, startBlockObject, upward, startBlockObject.CoordinatesAtBaseZ.z);
            }
            return _blockObjects.AsReadOnlyEnumerable();
        }

        private void AddBlockObjectsRecursively(HashSet<BlockObject> set, BlockObject blockObject, bool upward, int referenceZ)
        {
            if (set.Add(blockObject))
            {
                foreach (Block block in from block in blockObject.PositionedBlocks.GetAllBlocks()
                    where upward ? block.Stackable.IsStackable() : block.IsFoundationBlock
                    select block)
                {
                    int z = upward ? 1 : (-1);
                    Vector3Int coordinates = block.Coordinates + new Vector3Int(0, 0, z);
                    foreach (BlockObject otherBlockObject in _blockService.GetObjectsAt(coordinates))
                    {
                        if (IncludeNearBlock(otherBlockObject.PositionedBlocks.GetBlock(coordinates), upward))
                        {
                            AddBlockObjectsRecursively(set, otherBlockObject, upward, referenceZ);
                        }
                    }
                }
            }
        }

        private static bool IsValid(BlockObject blockObject, int referenceZ)
        {
            return blockObject.CoordinatesAtBaseZ.z == referenceZ;            
        }

        private static bool IncludeNearBlock(Block block, bool upward)
        {
            return upward ? block.IsFoundationBlock : block.Stackable.IsStackable();
        }
    }
}