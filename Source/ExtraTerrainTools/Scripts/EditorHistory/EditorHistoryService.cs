using System.Collections.Generic;
using System;
using Timberborn.BlockSystem;
using Timberborn.InputSystem;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
using UnityEngine;
using Timberborn.Common;
using System.Collections;

namespace TerrainTools.EditorHistory
{
    public class EditorHistoryService : ILoadableSingleton, IPostLoadableSingleton, IInputProcessor
    {
        public static HistoryLog LogLevel = HistoryLog.None;

        private readonly static int FastStep = 5;
        private readonly static int NormalStep = 1;
        private readonly ITerrainService _terrainService;
        private readonly InputService _inputService;
        private readonly EventBus _eventBus;
        private readonly TerrainToolsManipulationService _manipulationService;
        private HistoryCollection<List<HistoryEntry>> _history;
        private List<HistoryEntry> _batch = null;
        private bool _recordMouse = true;
        private bool _handleEvents = true;

        public int HistoryLength { get { return _history.Count; } }

        public EditorHistoryService(
            ITerrainService terrainService,
            InputService inputService,
            EventBus eventBus,
            TerrainToolsManipulationService manipulationService
        )
        {
            _terrainService = terrainService;
            _inputService = inputService;
            _eventBus = eventBus;
            _manipulationService = manipulationService;
        }

        public void Load()
        {
            _history = new();
            _batch = new();
            _terrainService.TerrainHeightChanged += OnTerrainChanged;
            _inputService.AddInputProcessor(this);
        }

        public void PostLoad()
        {
            _eventBus.Register(this);
        }

        private void OnTerrainChanged(object sender, TerrainHeightChangeEventArgs evt)
        {
            if (!_handleEvents)
                return;

            _batch.Add(new(evt));
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
                    failed.Sort(OrderByObjectZ);
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
                    var obj = _manipulationService.GetFirstObjectAt(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement.Coordinates);
                    if (obj != null)
                    {
                        _manipulationService.DeleteObject(obj);
                    }
                }
                else
                {
                    // entry.ObjectEvent.Object = PlaceObject(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement);
                    return _manipulationService.PlaceObject(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement, entry.ObjectEvent.Growth);
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
                    failed.Sort(OrderByObjectZ);
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
                    return _manipulationService.PlaceObject(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement, entry.ObjectEvent.Growth);
                }
                else
                {
                    // DeleteObject(entry.ObjectEvent.Object);
                    var obj = _manipulationService.GetFirstObjectAt(entry.ObjectEvent.PrefabName, entry.ObjectEvent.Placement.Coordinates);
                    if (obj != null)
                    {
                        _manipulationService.DeleteObject(obj);
                    }
                }
            }
            return true;
        }

        public bool ProcessInput()
        {
            _handleEvents = true;

            // Hack to handle brush tools without using injection >>>
            if (_recordMouse && _batch?.Count > 0 && !_inputService.MainMouseButtonHeld)
            {
                if (LogLevel > HistoryLog.None)
                    Utils.Log("Input: MainMouseButtonUp");
                PrintHistoryToLog();
                FinishAndStartNewBatch();
                PrintHistoryToLog();
            }
            // <<< 
            else if (_inputService.IsKeyDown("ExtraTerrainTools.UndoTerrainFast"))
            {
                if (LogLevel > HistoryLog.None)
                    Utils.Log("Input: UndoTerrainFast");
                PrintHistoryToLog();
                Undo(FastStep);
                PrintHistoryToLog();
                return true;
            }
            else if (_inputService.IsKeyDown("ExtraTerrainTools.UndoTerrain"))
            {
                if (LogLevel > HistoryLog.None)
                    Utils.Log("Input: UndoTerrain");
                PrintHistoryToLog();
                Undo(NormalStep);
                PrintHistoryToLog();
                return true;
            }
            else if (_inputService.IsKeyDown("ExtraTerrainTools.RedoTerrainFast"))
            {
                if (LogLevel > HistoryLog.None)
                    Utils.Log("Input: RedoTerrainFast");
                PrintHistoryToLog();
                Redo(FastStep);
                PrintHistoryToLog();
                return true;
            }
            else if (_inputService.IsKeyDown("ExtraTerrainTools.RedoTerrain"))
            {
                if (LogLevel > HistoryLog.None)
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
                if (prevOcc.ContainsKey(tuple))
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

            foreach (var batch in _history)
            {
                Utils.LogIndented(1, "index: {0}, count: {1}", i, batch.Count);

                if (LogLevel == HistoryLog.Item)
                    PrintBatchToLog(batch, 2);

                i++;
            }
        }

        private void PrintBatchToLog(List<HistoryEntry> batch, int indents = 0)
        {
            int j = 0;

            foreach (var item in batch)
            {
                if (item.IsTerrainEvent)
                {
                    Utils.LogIndented(indents, "Terrain - ind: {0}, coord: {1}, from: {2}, to: {3}, set: {4}",
                        j,
                        item.TerrainEvent.Change.Coordinates,
                        item.TerrainEvent.Change.From,
                        item.TerrainEvent.Change.To,
                        item.TerrainEvent.Change.SetTerrain
                    );
                }
                if (item.IsObjectEvent)
                {
                    Utils.LogIndented(indents, "Object - ind: {0}, set: {1}, prefab: {2}, at: {3} rot: {4} flipped: {5} growth: {6}",
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

        /* Moved to TerrainToolsManipulationService
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

                private void DeleteObject(BlockObject obj)
                {
                    var waterSource = obj.GetComponentFast<WaterSource>();
                    var entity = obj.GetComponentFast<EntityComponent>();

                    if (waterSource != null)
                        waterSource.DeleteEntity();

                    obj.DeleteEntity();

                    _entityService.Delete(entity);
                }

                private BlockObject GetObjectAt(string name, Vector3Int coord)
                {
                    foreach (var obj in _blockService.GetObjectsAt(coord))
                    {
                        if (obj.GetComponentFast<PrefabSpec>().PrefabName == name)
                        {
                            return obj;
                        }
                    }
                    return null;
                }
        */
        private List<HistoryEntry> SimplifyChanges(List<HistoryEntry> batch)
        {
            try
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
                try
                {
                    foreach (var entry in batch)
                    {
                        if (entry.IsTerrainEvent)
                        {
                            var change = entry.TerrainEvent.Change;
                            coord.x = change.Coordinates.x;
                            coord.y = change.Coordinates.y;

                            try
                            {
                                if (!changeColumnFloor.ContainsKey(change.Coordinates) || change.From < changeColumnFloor[change.Coordinates])
                                {
                                    changeColumnFloor[change.Coordinates] = change.From;
                                }
                            }
                            catch (KeyNotFoundException e)
                            {
                                e.Data.Add("Dictionary", "changeColumnFloor");
                                throw e;
                            }

                            try
                            {
                                if (!changeColumnTop.ContainsKey(change.Coordinates) || change.To > changeColumnTop[change.Coordinates])
                                {
                                    changeColumnTop[change.Coordinates] = change.To;
                                }
                            }
                            catch (KeyNotFoundException e)
                            {
                                e.Data.Add("Dictionary", "changeColumnTop");
                                throw e;
                            }

                            // Brute method, could it be more performant?
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
                }
                catch (KeyNotFoundException e)
                {
                    e.Data.Add("Context", "While building pre/post change state maps");
                    throw e;
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

                try
                {
                    foreach (var entry in batch)
                    {
                        if (entry.IsTerrainEvent)
                        {
                            // Check if terrain change event should be kept, we only care about the state before and after
                            var change = entry.TerrainEvent.Change;
                            if (!visited.Contains(change.Coordinates))
                            {
                                // change.Coordinates represents a new terrain column, parse the entire column at once and once only
                                visited.Add(change.Coordinates);
                                try
                                {
                                    for (int z = changeColumnFloor[change.Coordinates]; z <= changeColumnTop[change.Coordinates]; z++)
                                    {
                                        coord.x = change.Coordinates.x;
                                        coord.y = change.Coordinates.y;
                                        coord.z = z;

                                        try
                                        {
                                            if (!preChangeStateMap.ContainsKey(coord) || !postChangeStateMap.ContainsKey(coord) || preChangeStateMap[coord] == postChangeStateMap[coord])
                                                continue;
                                        }
                                        catch (KeyNotFoundException e)
                                        {
                                            if (!e.Data.Contains("Dictionary"))
                                            {
                                                if (preChangeStateMap.ContainsKey(coord))
                                                    e.Data.Add("Dictionary", "preChangeStateMap");
                                                else
                                                    e.Data.Add("Dictionary", "postChangeStateMap");
                                            }

                                            e.Data.Add("Context 2", "While comparing pre/post change state maps");
                                            throw e;
                                        }

                                        bool isSet = postChangeStateMap[coord];
                                        int to = z;
                                        try
                                        {
                                            while (to <= changeColumnTop[change.Coordinates] && preChangeStateMap.ContainsKey(coord) && postChangeStateMap.ContainsKey(coord) && preChangeStateMap[coord] != postChangeStateMap[coord])
                                            {
                                                to++;
                                                coord.z = to;
                                            }
                                        }
                                        catch (KeyNotFoundException e)
                                        {
                                            if (!e.Data.Contains("Dictionary"))
                                            {
                                                if (!changeColumnTop.ContainsKey(change.Coordinates))
                                                    e.Data.Add("Dictionary", "changeColumnTop");
                                                else
                                                {
                                                    e.Data.Add("changeColumnFloor[change.Coordinates]", changeColumnFloor[change.Coordinates]);
                                                    e.Data.Add("changeColumnTop[change.Coordinates]", changeColumnTop[change.Coordinates]);
                                                    if (preChangeStateMap.ContainsKey(coord))
                                                        e.Data.Add("Dictionary", "postChangeStateMap");
                                                    else
                                                        e.Data.Add("Dictionary", "preChangeStateMap");
                                                }

                                                e.Data.Add("to", to);
                                                e.Data.Add("z", z);
                                                e.Data.Add("Context 2", "While find column upper limit");
                                            }

                                            throw e;
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
                                catch (KeyNotFoundException e)
                                {
                                    e.Data.Add("change.Coordinates", change.Coordinates);
                                    e.Data.Add("change.From", change.From);
                                    e.Data.Add("change.To", change.To);
                                    e.Data.Add("change.SetTerrain", change.SetTerrain);
                                    if (!e.Data.Contains("Dictionary"))
                                    {
                                        if (changeColumnFloor.ContainsKey(change.Coordinates))
                                            e.Data.Add("Dictionary", "changeColumnTop");
                                        else
                                            e.Data.Add("Dictionary", "changeColumnFloor");
                                    }

                                    throw e;
                                }
                            }
                        }
                        else
                        {
                            // Object events are simply added back in
                            simplified.Add(entry);
                        }
                    }
                }
                catch (KeyNotFoundException e)
                {
                    e.Data.Add("Context", "While building simplified batch");
                    e.Data.Add("Result", simplified);
                    throw e;
                }

                if (LogLevel == HistoryLog.Simplification)
                {
                    Utils.Log("SimplifyChanges - Post-simplify - batchsize: {0}", simplified.Count);
                    PrintBatchToLog(simplified, 1);
                }

                return simplified;
            }
            catch (KeyNotFoundException e)
            {
                Utils.LogError("KeyNotFoundException in EditorHistoryService.SimplifyChanges");
                Utils.LogError("Exception Data:");
                foreach (DictionaryEntry data in e.Data)
                {
                    Utils.LogError("{0} = {1}", data.Key, data.Value);
                }

                Utils.LogError("Source batch:");
                PrintBatchToLog(batch, 1);
                if (e.Data.Contains("Result"))
                {
                    Utils.LogError("Simplified batch:");
                    PrintBatchToLog((List<HistoryEntry>)e.Data["Result"], 1);
                }

                throw e;
            }
        }
    }
}
