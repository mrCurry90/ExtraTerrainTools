using System.Collections.Generic;
using System;
using Timberborn.BlockObjectTools;
using Timberborn.BlockSystem;
using Timberborn.Coordinates;
using Timberborn.EntitySystem;
using Timberborn.Growing;
using Timberborn.InputSystem;
using Timberborn.NaturalResources;
using Timberborn.PrefabSystem;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
using Timberborn.WaterSourceSystem;
using UnityEngine;
using Timberborn.MapIndexSystem;
using Timberborn.Common;
using Timberborn.NaturalResourcesModelSystem;

namespace TerrainTools.EditorHistory
{
    public class EditorHistoryService : ILoadableSingleton, IInputProcessor
    {
        public static HistoryLog LogLevel = HistoryLog.None;

        private readonly static int FastStep = 5;
        private readonly static int NormalStep = 1;
        private readonly ITerrainService _terrainService;
        private readonly InputService _inputService;
        private readonly EventBus _eventBus;
        private readonly BlockObjectPlacerService _placerService;
        private readonly EntityService _entityService;
        private readonly PrefabNameMapper _prefabNameMapper;
        private readonly IBlockService _blockService;
        private readonly MapIndexService _mapIndexService;
        private readonly BlockValidator _blockValidator;
        private HistoryCollection<List<HistoryEntry>> _history;
        private List<HistoryEntry> _batch = null;
        private bool _recordMouse = true;
        private bool _handleEvents = true;

        public int HistoryLength { get { return _history.Count; } }

        public EditorHistoryService(
            ITerrainService terrainService,
            InputService inputService,
            EventBus eventBus,
            BlockObjectPlacerService placerService,
            EntityService entityService,
            PrefabNameMapper prefabNameMapper,
            IBlockService blockService,
            MapIndexService mapIndexService,
            BlockValidator blockValidator
        ) {
            _terrainService = terrainService;
            _inputService = inputService;
            _eventBus = eventBus;
            _placerService = placerService;
            _entityService = entityService;
            _prefabNameMapper = prefabNameMapper;
            _blockService = blockService;
            _blockValidator = blockValidator;
            _mapIndexService = mapIndexService;
        }

        public void Load()
        {
            _history = new();
            _batch = new();
            _terrainService.TerrainHeightChanged += OnTerrainChanged;
            _inputService.AddInputProcessor(this);
            _eventBus.Register(this);
        }

        private void OnTerrainChanged( object sender, TerrainHeightChangeEventArgs evt)
        {
            if( !_handleEvents )
                return;

            _batch.Add( new(evt) );
        }

        [OnEvent]
        public void OnBlockObjectSet(BlockObjectSetEvent setEvent)
        {
            if (!_handleEvents)
                return;

            _batch.Add(new(setEvent));

            // var growable = setEvent.BlockObject.GetComponentFast<Growable>();            

            // Utils.Log("growable: {0}", growable);
            // Utils.Log("growable.IsGrown: {0}", growable.IsGrown);
            // Utils.Log("growable.GrowthProgress: {0}", growable.GrowthProgress);

            CleanDuplicatesInBatch();            
        }


        [OnEvent]
        public void OnBlockObjectUnset(BlockObjectUnsetEvent unsetEvent)
        {
            if (!_handleEvents)
                return;

            _batch.Add(new(unsetEvent));

            CleanDuplicatesInBatch();
        }

        /// <summary>
        /// Start a batch change
        /// </summary>
        public void BatchStart()
        {
            _recordMouse = false;
            FinishAndStartNewBatch();
        }
        /// <summary>
        /// Finalize a batch started by BatchStart()
        /// </summary>
        public void BatchStop()
        {
            _recordMouse = true;
            FinishAndStartNewBatch();
        }

        private void Undo(int steps)
        {
            _handleEvents = false;

            var past = _history.Undo(Mathf.Abs(steps));
            foreach (var batch in past)
            {
                if (LogLevel >= HistoryLog.Batch)
                    Utils.Log("Undoing batch with {0} items.", batch.Count);

                List<HistoryEntry> failed = new();
                for (int i = batch.Count - 1; i >= 0; i--)
                {
                    if (!UndoSingle(batch[i]))
                    {
                        failed.Add(batch[i]);
                    }
                }

                if (failed.Count > 0)
                {
                    // Try redoing in Z-order ascending
                    failed.Sort( OrderByObjectZ );
                    foreach (var entry in failed)
                    {
                        if (!UndoSingle(entry))
                            Utils.LogError("Failed to undo {0}", entry);
                    }
                }
            }

            //_handleEvents = true;
        }

        private bool UndoSingle(HistoryEntry entry)
        {
            if (LogLevel >= HistoryLog.Item)
                Utils.Log("Undoing {0}", entry);

            if (entry.IsTerrainEvent)
            {
                // TerrainHeightChange(coordinates2D, intFrom, intTo, boolSet);
                TerrainHeightChange change = entry.TerrainEvent.Change;

                Vector3Int coord = change.Coordinates.XYZ();
                int height = change.To + 1 - change.From;
                if (change.SetTerrain)
                {
                    // Undo set = Unset
                    coord.z = change.To;
                    _terrainService.UnsetTerrain(coord, height);
                }
                else
                {
                    // Undo unset = Set
                    coord.z = change.From;
                    _terrainService.SetTerrain(coord, height);
                }
            }
            else
            {
                if (entry.ObjectEvent.IsSet)
                {
                    // DeleteObject(entry.ObjectEvent.Object);
                    var obj = GetObjectAt(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement.Coordinates);
                    if (obj != null)
                    {
                        DeleteObject(obj);
                    }
                }
                else
                {
                    // entry.ObjectEvent.Object = PlaceObject(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement);
                    return PlaceObject(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement, entry.ObjectEvent.Growth);
                }
            }
            return true;
        }
        private void Redo(int steps)
        {
            _handleEvents = false;

            var future = _history.Redo(Mathf.Abs(steps));

            foreach (var batch in future)
            {
                List<HistoryEntry> failed = new();
                for (int i = 0; i < batch.Count; i++)
                {
                    if (!RedoSingle(batch[i]))
                    {
                        failed.Add(batch[i]);
                    }
                }

                if (failed.Count > 0)
                {
                    // Try redoing in Z-order ascending
                    failed.Sort( OrderByObjectZ );
                    foreach (var entry in failed)
                    {
                        if (!RedoSingle(entry))
                            Utils.LogError("Failed to redo {0}", entry);
                    }
                }
            }
            


           //_handleEvents = true;
        }
        private bool RedoSingle(HistoryEntry entry)
        {
            if (LogLevel >= HistoryLog.Item)
                Utils.Log("Redoing {0}", entry);

            if (entry.IsTerrainEvent)
            {
                // TerrainHeightChange(coordinates2D, intFrom, intTo, boolSet);
                TerrainHeightChange change = entry.TerrainEvent.Change;

                Vector3Int coord = change.Coordinates.XYZ();
                int height = change.To + 1 - change.From;
                if (change.SetTerrain)
                {
                    // Redo set = Set
                    coord.z = change.From;
                    _terrainService.SetTerrain(coord, height);

                }
                else
                {
                    // Redo unset = Unset
                    coord.z = change.To;
                    _terrainService.UnsetTerrain(coord, height);
                }
            }
            else
            {
                if (entry.ObjectEvent.IsSet)
                {
                    // entry.ObjectEvent.Object = PlaceObject(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement);
                    return PlaceObject(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement, entry.ObjectEvent.Growth);
                }
                else
                {
                    // DeleteObject(entry.ObjectEvent.Object);
                    var obj = GetObjectAt(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement.Coordinates);
                    if (obj != null)
                    {
                        DeleteObject(obj);
                    }
                }
            }
            return true;
        }

        public bool ProcessInput()
        {
            _handleEvents = true;

            // Hack to handle brush tools without using injection >>>
            if ( _recordMouse && _batch?.Count > 0 && !_inputService.MainMouseButtonHeld )
            {
                if( LogLevel > HistoryLog.None) 
                    Utils.Log("Input: MainMouseButtonUp");
                PrintHistoryToLog();
                FinishAndStartNewBatch();
                PrintHistoryToLog();
            }
            // <<< 
            else if (_inputService.IsKeyDown("UndoTerrainFast"))
            {
                if( LogLevel > HistoryLog.None) 
                    Utils.Log("Input: UndoTerrainFast");
                PrintHistoryToLog();
                Undo(FastStep);
                PrintHistoryToLog();
                return true;
            }
            else if (_inputService.IsKeyDown("UndoTerrain"))
            {
                if( LogLevel > HistoryLog.None) 
                    Utils.Log("Input: UndoTerrain");
                PrintHistoryToLog();
                Undo(NormalStep);
                PrintHistoryToLog();
                return true;
            }
            else if (_inputService.IsKeyDown("RedoTerrainFast"))
            {
                if( LogLevel > HistoryLog.None) 
                    Utils.Log("Input: RedoTerrainFast");
                PrintHistoryToLog();
                Redo(FastStep);
                PrintHistoryToLog();
                return true;
            }
            else if (_inputService.IsKeyDown("RedoTerrain"))
            {
                if( LogLevel > HistoryLog.None) 
                    Utils.Log("Input: RedoTerrain");
                PrintHistoryToLog();
                Redo(NormalStep);
                PrintHistoryToLog();
                return true;
            }
            
            return false;
        }

        private void CleanDuplicatesInBatch()
        {
            if (_batch.Count == 0)
                return;

            // Conditional copy to clean up duplicate ObjectChangeEvents
            List<HistoryEntry> cleaned = new(_batch.Count);
            Dictionary<Tuple<string, Vector3Int>, int> prevOcc = new();
            Tuple<string, Vector3Int> tuple;

            for (int i = 0; i < _batch.Count; i++)
            {
                var elem = _batch[i];
                if (elem.ObjectEvent == null)
                {
                    cleaned.Add(elem);
                    continue;
                }

                tuple = new(elem.ObjectEvent.PrefabName, elem.ObjectEvent.Placement.Coordinates);
                if( prevOcc.ContainsKey(tuple) )
                {
                    cleaned[prevOcc[tuple]] = elem;
                }
                else
                {
                    prevOcc[tuple] = i;
                    cleaned.Add(elem);
                }
            }
            _batch = cleaned;
        }

        private int OrderByObjectZ(HistoryEntry a, HistoryEntry b)
        {
            return a.IsObjectEvent ? a.ObjectEvent.Placement.Coordinates.z - b.ObjectEvent.Placement.Coordinates.z : 0;
        }

        private void FinishBatch()
        {
            // If batch still valid store it
            if (_batch.Count > 0)
            {
                // Only store changes that reflect the state at the start and the end of the batch
                _history.Insert(SimplifyChanges(_batch));
            }
        }

        private void FinishAndStartNewBatch()
        {
            FinishBatch();
            
            _batch = new();
        }

        private void PrintHistoryToLog()
        {
            if (LogLevel == HistoryLog.None)
                return;

            Utils.Log("History: {0} Now: {1}", _history.Count, _history.Now);
            int i = 0;
            
            if (LogLevel == HistoryLog.History)
                return;

            foreach( var batch in _history )
            {   
                Utils.LogIndented(1, "index: {0}, count: {1}", i, batch.Count);
                
                if (LogLevel == HistoryLog.Item)
                    PrintBatchToLog(batch, 2);
                    
                i++;
            }
        }

        private void PrintBatchToLog( List<HistoryEntry> batch, int indents = 0 )
        {
            int j = 0;
                
            foreach (var item in batch)
            {
                if (item.IsTerrainEvent)
                {
                    Utils.LogIndented( indents, "Terrain - ind: {0}, coord: {1}, from: {2}, to: {3}, set: {4}",
                        j,
                        item.TerrainEvent.Change.Coordinates,
                        item.TerrainEvent.Change.From,
                        item.TerrainEvent.Change.To,
                        item.TerrainEvent.Change.SetTerrain
                    );
                }
                if (item.IsObjectEvent)
                {
                    Utils.LogIndented( indents, "Object - ind: {0}, set: {1}, prefab: {2}, at: {3} rot: {4} flipped: {5} growth: {6}", 
                        j, 
                        item.ObjectEvent.IsSet,
                        item.ObjectEvent.PrefabName, 
                        item.ObjectEvent.Placement.Coordinates,
                        item.ObjectEvent.Placement.Orientation,
                        item.ObjectEvent.Placement.FlipMode.IsFlipped,
                        item.ObjectEvent.Growth
                    );
                }
                                    
                j++;
            }
        }

        private bool PlaceObject(string prefabName, Placement placement, float growth)
        {
            var prefab = _prefabNameMapper.GetPrefab(prefabName).GetComponentFast<BlockObjectSpec>();
            var placer = _placerService.GetMatchingPlacer(prefab);
            if (!_blockValidator.BlocksValid(prefab, placement))
                return false;

            placer.Place(prefab, placement);

            var blockObject = GetObjectAt(prefabName, placement.Coordinates);
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

        private void DeleteObject( BlockObject obj )
        {
            var waterSource = obj.GetComponentFast<WaterSource>();
            var entity = obj.GetComponentFast<EntityComponent>();

            if( waterSource != null)
                waterSource.DeleteEntity();
            
            obj.DeleteEntity();        

            _entityService.Delete(entity);
        }

        private BlockObject GetObjectAt(string name, Vector3Int coord)
        {
            foreach (var obj in _blockService.GetObjectsAt(coord))
            {
                if( obj.GetComponentFast<PrefabSpec>().PrefabName == name ) 
                {
                    return obj;
                }
            }
            return null;
        }

        private List<HistoryEntry> SimplifyChanges( List<HistoryEntry> batch )
        {
            // New voxel terrain code - Update 7+

            // Find the state of the terrain prior to and after the batch change
            Dictionary<Vector3Int, bool> preChangeStateMap = new();
            Dictionary<Vector3Int, bool> postChangeStateMap = new();
            Dictionary<Vector2Int, int> changeColumnFloor = new();
            Dictionary<Vector2Int, int> changeColumnTop = new();

            if (LogLevel == HistoryLog.Simplification)
            {        
                Utils.Log("SimplifyChanges - Pre-simplify - batchsize: {0}", batch.Count);
                PrintBatchToLog(batch, 1);
            }

            Vector3Int coord = new();
            foreach (var entry in batch)
            {
                if (entry.IsTerrainEvent)
                {
                    var change = entry.TerrainEvent.Change;
                    coord.x = change.Coordinates.x;
                    coord.y = change.Coordinates.y;

                    if (!changeColumnFloor.ContainsKey(change.Coordinates) || change.From < changeColumnFloor[change.Coordinates])
                    {
                        changeColumnFloor[change.Coordinates] = change.From;
                    }

                    if (!changeColumnTop.ContainsKey(change.Coordinates) || change.To > changeColumnTop[change.Coordinates])
                    {
                        changeColumnTop[change.Coordinates] = change.To;
                    }

                    // Brute method, could be more performant?
                    for (int i = change.From; i <= change.To; i++)
                    {
                        coord.z = i;
                        if (!preChangeStateMap.ContainsKey(coord))
                        {
                            preChangeStateMap[coord] = !change.SetTerrain;
                        }

                        postChangeStateMap[coord] = change.SetTerrain;
                    }
                }
            }

            if (LogLevel == HistoryLog.Simplification)
            {
                Utils.Log("SimplifyChanges - Mid-simplify");
                Utils.Log("preChangeStateMap = ");
                foreach (var item in preChangeStateMap)
                {
                    Utils.LogIndented(1, "Key: {0},{1},{2} Value: {3}", item.Key.x, item.Key.y, item.Key.z, item.Value);
                }

                Utils.Log("postChangeStateMap = ");
                foreach (var item in postChangeStateMap)
                {
                    Utils.LogIndented(1, "Key: {0},{1},{2} Value: {3}", item.Key.x, item.Key.y, item.Key.z, item.Value);
                }

                Utils.Log("changeColumnFloor = ");
                foreach (var item in changeColumnFloor)
                {
                    Utils.LogIndented(1, "Key: {0},{1} Value: {2}", item.Key.x, item.Key.y, item.Value);
                }

                Utils.Log("changeColumnTop = ");
                foreach (var item in changeColumnTop)
                {
                    Utils.LogIndented(1, "Key: {0},{1} Value: {2}", item.Key.x, item.Key.y, item.Value);
                }
            }

            // Filter out entries and replace them with contigious terrain columns that have changed
            // Object type changes are left in place
            HashSet<Vector2Int> visited = new();
            List<HistoryEntry> simplified = new();
            foreach (var entry in batch)
            {
                if (entry.IsTerrainEvent)
                {
                    // Check if terrain change event should be kept, we only care about the state before and after
                    var change = entry.TerrainEvent.Change;
                    if (!visited.Contains(change.Coordinates))
                    {
                        // This is a new column, parse this column once only
                        visited.Add(change.Coordinates);

                        for (int z = changeColumnFloor[change.Coordinates]; z <= changeColumnTop[change.Coordinates]; z++)
                        {
                            coord.x = change.Coordinates.x;
                            coord.y = change.Coordinates.y;
                            coord.z = z;

                            if (preChangeStateMap[coord] == postChangeStateMap[coord])
                                continue;

                            bool isSet = postChangeStateMap[coord];
                            int to = z;
                            while (to <= changeColumnTop[change.Coordinates] && preChangeStateMap[coord] != postChangeStateMap[coord])
                            {
                                to++;
                                coord.z = to;
                            }

                            simplified.Add(
                                new HistoryEntry(
                                    new TerrainHeightChangeEventArgs(
                                        new TerrainHeightChange(change.Coordinates, z, to - 1, isSet)
                                    )
                                )
                            );

                            z = to;
                        }
                    }
                }
                else
                {
                    // Object events are simply added back in
                    simplified.Add(entry);
                }
            }

            if (LogLevel == HistoryLog.Simplification)
            {        
                Utils.Log("SimplifyChanges - Post-simplify - batchsize: {0}", simplified.Count);
                PrintBatchToLog(simplified, 1);
            }

            /* Old 2.5D terrain code - Up to update 6
            Dictionary<Vector2Int, int> oldest = new();
            Dictionary<Vector2Int, int> newest = new();

            TerrainColumnChangedEventArgs evt;
            foreach (var item in batch)
            {
                if (item.IsTerrainEvent)
                {
                    evt = item.TerrainEvent;
                    if (!oldest.ContainsKey(evt.Change.Coordinates))
                    {
                        oldest[evt.Change.Coordinates] = evt.Change.From;
                    }
                    newest[evt.Change.Coordinates] = evt.Change.To;
                }
            }

            Dictionary<Vector2Int, bool> visited = new();

            // Order in batch is important, hence cannot rely on Dictionary.ToList() to filter
            List<HistoryEntry> simplified = new();
            foreach (var item in batch)
            {
                if (item.IsTerrainEvent)
                {
                    // Check if terrain event should be kept, only oldest height and newest height value for each point is kept
                    evt = item.TerrainEvent;
                    if( !visited.ContainsKey(evt.Change.Coordinates) )
                    {
                        visited[evt.Change.Coordinates] = true;
                        simplified.Add(
                            new HistoryEntry(
                                new TerrainColumnChangedEventArgs(
                                    new TerrainHeightChange( evt.Change.Coordinates, oldest[evt.Change.Coordinates], newest[evt.Change.Coordinates], evt.Change.SetTerrain )
                                )
                            )
                        );
                    }
                }
                else 
                {
                    // Object events are simply added back in
                    simplified.Add(item);
                }
            }
            */

            return simplified;
        }
    }
}

