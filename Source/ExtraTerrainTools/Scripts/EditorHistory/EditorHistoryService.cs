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
using System.Linq;

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
        private readonly BlockService _blockService;
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
            BlockService blockService
        ) {
            _terrainService = terrainService;
            _inputService = inputService;
            _eventBus = eventBus;
            _placerService = placerService;
            _entityService = entityService;
            _prefabNameMapper = prefabNameMapper;
            _blockService = blockService;
        }

        public void Load()
        {
            _history = new();
            _batch = new();
            _terrainService.TerrainHeightChanged += OnTerrainChanged;
            _inputService.AddInputProcessor(this);
            _eventBus.Register(this);
        }

        private void OnTerrainChanged( object sender, TerrainHeightChangedEventArgs evt)
        {
            if( !_handleEvents )
                return;

            _batch.Add( new(evt) );
        }

        [OnEvent]
        public void OnBlockObjectSet(BlockObjectSetEvent setEvent)
        {
            if( !_handleEvents )
                return;

            _batch.Add( new(setEvent) );

            // var growable = setEvent.BlockObject.GetComponentFast<Growable>();            
            
            // Utils.Log("growable: {0}", growable);
            // Utils.Log("growable.IsGrown: {0}", growable.IsGrown);
            // Utils.Log("growable.GrowthProgress: {0}", growable.GrowthProgress);

            CleanDuplicatesInBatch();
        }


        [OnEvent]
        public void OnBlockObjectUnset(BlockObjectUnsetEvent unsetEvent)
        {
            if( !_handleEvents )
                return;

            _batch.Add( new(unsetEvent) );

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

        private void Undo( int steps )
        {
            _handleEvents = false;

            var past = _history.Undo( Mathf.Abs(steps) );
            foreach (var batch in past)
            {
                foreach (var entry in batch)
                {
                    UndoSingle(entry);
                }
            }

            //_handleEvents = true;
        }
        private void UndoSingle( HistoryEntry entry )
        {
            if( entry.IsTerrainEvent ) 
            {
                _terrainService.SetHeight(entry.TerrainEvent.Coordinates, entry.TerrainEvent.OldHeight);
            }
            else
            {
                if( entry.ObjectEvent.IsSet )
                {
                    // DeleteObject(entry.ObjectEvent.Object);
                    var obj = GetObjectAt(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement.Coordinates);
                    if( obj != null )
                    {
                        DeleteObject(obj);
                    }
                }
                else 
                {
                    // entry.ObjectEvent.Object = PlaceObject(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement);
                    PlaceObject(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement, entry.ObjectEvent.Growth);
                }
            }
        }
        private void Redo( int steps ) 
        {
            _handleEvents = false;
            
            var future = _history.Redo( Mathf.Abs(steps) );
            
            foreach (var batch in future)
            {
                foreach (var entry in batch)
                {
                    RedoSingle(entry);
                }
            }

           //_handleEvents = true;
        }
        private void RedoSingle( HistoryEntry entry )
        {
            if( entry.IsTerrainEvent ) 
            {
                _terrainService.SetHeight(entry.TerrainEvent.Coordinates, entry.TerrainEvent.NewHeight);
            }
            else
            {

                if( entry.ObjectEvent.IsSet )
                {
                    // entry.ObjectEvent.Object = PlaceObject(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement);
                    PlaceObject(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement, entry.ObjectEvent.Growth);
                }
                else 
                {
                    // DeleteObject(entry.ObjectEvent.Object);
                    var obj = GetObjectAt(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement.Coordinates);
                    if( obj != null )
                    {
                        DeleteObject(obj);
                    }
                }
            }
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

        private void FinishBatch()
        {
            // If batch still valid store it
            if( _batch.Count > 0 )
            {
                // Only store changes that reflect the state at the start and the end of the batch
                _history.Insert( SimplifyChanges(_batch) );
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
                Utils.Log(" 	index: {0}, count: {1}", i, batch.Count);
                
                if (LogLevel == HistoryLog.Batch)
                    continue;

                int j = 0;
                
                foreach (var item in batch)
                {  
                    if( item.TerrainEvent != null )
                    {
                        Utils.Log(" 	    Terrain - ind: {0}, coord: {1}, old: {2}, new: {3}", 
                            j, 
                            item.TerrainEvent.Coordinates, 
                            item.TerrainEvent.OldHeight, 
                            item.TerrainEvent.NewHeight
                        );
                    }
                    if( item.ObjectEvent != null )
                    {
                        Utils.Log(" 	    Object - ind: {0}, set: {1}, prefab: {2}, at: {3} rot: {4} flipped: {5} growth: {6}", 
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
                i++;
            }
        }

        private void PlaceObject( string prefabName, Placement placement, float growth )
        {
            var prefab = _prefabNameMapper.GetPrefab(prefabName).GetComponentFast<BlockObject>();
            var placer = _placerService.GetMatchingPlacer(prefab);
            placer.Place(prefab, placement);

            var blockObject = GetObjectAt(prefabName, placement.Coordinates);
            if (blockObject != null) 
            {
                if ((bool)prefab.GetComponentFast<NaturalResource>())
                {
                    blockObject.GetComponentFast<IModelRandomizer>()?.RandomizeModel();
                    blockObject.GetComponentFast<CoordinatesOffseter>().SetOffset();
                }

                var growable = blockObject.GetComponentFast<Growable>();
                if (growable != null && growth >= 0)
                {
                    growable.IncreaseGrowthProgress(growth);
                }
            }
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
                if( obj.GetComponentFast<Prefab>().PrefabName == name ) 
                {
                    return obj;
                }
            }
            return null;
        }

        private List<HistoryEntry> SimplifyChanges( List<HistoryEntry> batch)
        {
            
            Dictionary<Vector2Int, int> oldest = new();
            Dictionary<Vector2Int, int> newest = new();

            TerrainHeightChangedEventArgs evt;
            foreach (var item in batch)
            {
                if (item.IsTerrainEvent)
                {
                    evt = item.TerrainEvent;
                    if (!oldest.ContainsKey(evt.Coordinates))
                    {
                        oldest[evt.Coordinates] = evt.OldHeight;
                    }
                    newest[evt.Coordinates] = evt.NewHeight;
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
                    if( !visited.ContainsKey(evt.Coordinates) )
                    {
                        visited[evt.Coordinates] = true;
                        simplified.Add(
                            new HistoryEntry(
                                new TerrainHeightChangedEventArgs(evt.Coordinates, oldest[evt.Coordinates], newest[evt.Coordinates])
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

            return simplified;
        }
    }
}

