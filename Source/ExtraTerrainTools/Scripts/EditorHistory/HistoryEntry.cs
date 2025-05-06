using System;
using Timberborn.BlockSystem;
using Timberborn.Growing;
using Timberborn.PrefabSystem;
using Timberborn.TerrainSystem;
using UnityEngine;

namespace TerrainTools.EditorHistory
{
    internal struct HistoryEntry
    {
        public static HistoryLog LogLevel = HistoryLog.None;
        public TerrainHeightChangeEventArgs TerrainEvent { get; }
        public ObjectChangedEvent ObjectEvent { get; private set; }

        public readonly bool IsTerrainEvent { get { return TerrainEvent != null; } }
        public readonly bool IsObjectEvent { get { return ObjectEvent != null; } }

        public HistoryEntry( TerrainHeightChangeEventArgs evt )
        {
            TerrainEvent = evt;
            ObjectEvent = default;
        }

        public override readonly string ToString()
        {
            string changeType;
            string content;
            if (IsTerrainEvent)
            {
                changeType = "Terrain";
                content = string.Format(
                    "{0} col {1} from {2} to {3}",
                    TerrainEvent.Change.SetTerrain ? "set" : "unset",
                    TerrainEvent.Change.Coordinates,
                    TerrainEvent.Change.From,
                    TerrainEvent.Change.To
                );
                
            }
            else if (IsObjectEvent)
            {
                changeType = "Object";
                content = string.Format(
                    "{0} {1} at {2} with growth {3}",
                    ObjectEvent.IsSet ? "set" : "unset",
                    ObjectEvent.PrefabName,
                    ObjectEvent.Placement.Coordinates,
                    ObjectEvent.Growth
                );
            }
            else
            {
                changeType = "Unknown";
                content = "NULL";
            }

            return string.Format( "{0}: {1} => {2}", GetType().Name, changeType, content );
        }

        public HistoryEntry(BlockObjectSetEvent evt)
        {
            LogLevel = HistoryLog.None;
            TerrainEvent = null;
            ObjectEvent = new()
            {
                IsSet = true,
                PrefabName = evt.BlockObject.GetComponentFast<PrefabSpec>().PrefabName,
                Placement = evt.BlockObject.Placement,
                Growth = evt.BlockObject.TryGetComponentFast(out Growable growable) ? growable.GrowthProgress : -1
            };

            // Update Growth if HasGrown event is called (workaround for trees being updated later)
            // EventRemover is to make sure the event gets removed on destruction, preventing mem leaking.
            if (growable != null)
            {
                var objectEvent = ObjectEvent;
                growable.GameObjectFast.AddComponent<EventRemover>().SetHasGrownHandler(growable,
                    delegate
                    {
                        if (objectEvent != null)
                            objectEvent.Growth = 1f;
                        else if (LogLevel > HistoryLog.None)
                            Utils.Log("HistoryEntry objectEvent was null");
                    }
                );
                // var objectEvent = ObjectEvent;                    
                // growable.HasGrown += delegate
                // {
                //     if (objectEvent != null)
                //         objectEvent.Growth = 1f;
                //     else if( LogLevel > HistoryLog.None )
                //         Utils.HistoryLog("HistoryEntry objectEvent was null");
                // };
            }

            if (LogLevel > HistoryLog.None)
                Utils.Log("New object set: {0}", evt.BlockObject, evt.BlockObject.GetHashCode());
        }

        public HistoryEntry( BlockObjectUnsetEvent evt)
        {
            TerrainEvent = null;
            ObjectEvent = new()
            {
                IsSet = false,
                PrefabName = evt.BlockObject.GetComponentFast<PrefabSpec>().PrefabName,
                Placement = evt.BlockObject.Placement,
                Growth = evt.BlockObject.TryGetComponentFast( out Growable growable ) ? (growable.IsGrown ? 1 : 0) : -1
            };

            if( LogLevel > HistoryLog.None )
                Utils.Log("Object unset: {0} {1}", evt.BlockObject, evt.BlockObject.GetHashCode() );
        }

        /// <summary>
        /// Helper MonoBehaviour to remove event handler when object is destroyed.
        /// </summary>
        private class EventRemover : MonoBehaviour
        {
            private EventHandler eventHandler;
            private Growable growable;

            public void SetHasGrownHandler( Growable growable, EventHandler eventHandler)
            {
                this.eventHandler = eventHandler;
                this.growable = growable;
                growable.HasGrown += eventHandler;
            }
            private void OnDestroy()
            {
                growable.HasGrown -= eventHandler;
            }
        }
    }
}