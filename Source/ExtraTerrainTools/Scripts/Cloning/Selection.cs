using System;
using System.Collections.Generic;
using System.Linq;
using Timberborn.BlockSystem;
using Timberborn.Coordinates;
using Timberborn.Growing;
using Timberborn.PrefabSystem;
using Timberborn.TerrainSystem;
using UnityEngine;

namespace TerrainTools.Cloning
{
    public class Selection
    {
        public struct BlockObjectData
        {
            public string PrefabName { get; set; }
            public Vector3Int Coordinates { get; set; }
            public Orientation Orientation { get; set; }
            public bool Flippable { get; internal set; }
            public FlipMode FlipMode { get; set; }
            public float Growth { get; set; }


            public bool Equals(BlockObjectData other)
            {
                return PrefabName.Equals(other.PrefabName)
                    && Coordinates.Equals(other.Coordinates)
                    && Orientation.Equals(other.Orientation)
                    && Flippable.Equals(other.Flippable)
                    && FlipMode.Equals(other.FlipMode)
                    && Growth.Equals(other.Growth);
            }

            public override bool Equals(object obj)
            {
                if (obj is Placement other)
                {
                    return Equals(other);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(PrefabName, Coordinates, Orientation, FlipMode, Growth);
            }

            public static bool operator ==(BlockObjectData left, BlockObjectData right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(BlockObjectData left, BlockObjectData right)
            {
                return !left.Equals(right);
            }

            public override string ToString()
            {
                return $"{PrefabName}, {Coordinates}, {Orientation}, flippable: {Flippable}, isFlipped: {FlipMode.IsFlipped}, growth: {Growth}";
            }
        }

        public Vector3Int Size { get; private set; }
        public int ObjectCount { get => _objectsInternal.Count; }
        public int TerrainCount { get => _terrainInternal.Count; }

        private Orientation _orientation = Orientation.Cw0;
        private FlipMode _flipMode = new(false);
        private Vector3Int _center;

        // Coordinates are stored relative to the center of the selection in both internal buffers
        private readonly Dictionary<Vector3Int, bool> _terrainInternal = new();
        private readonly HashSet<BlockObjectData> _objectsInternal = new();

        public void Clear()
        {
            _terrainInternal.Clear();
            _objectsInternal.Clear();
            _orientation = Orientation.Cw0;
            _flipMode = new(false);
        }

        public void Scan(ITerrainService terrainService, IBlockService blockService, Vector3Int from, Vector3Int toInclusive, bool includeStacked, bool includeAir)
        {
            Clear();

            try
            {
                Size = new(
                    Mathf.Abs(toInclusive.x - from.x) + 1,
                    Mathf.Abs(toInclusive.y - from.y) + 1,
                    Mathf.Abs(toInclusive.z - from.z) + 1
                );
                var center = UpdatePosition(from, toInclusive);
                var checkedObjects = new HashSet<BlockObject>();

                foreach (var coord in AllCoordinates(from, toInclusive))
                {
                    if (!terrainService.Contains(coord))
                        continue;

                    var relative = coord - center;

                    if (includeAir)
                        _terrainInternal.Add(relative, terrainService.Underground(coord));
                    else if (terrainService.Underground(coord))
                        _terrainInternal.Add(relative, true);

                    var blockObjects = includeStacked switch
                    {
                        true => blockService.GetStackedObjectsAt(coord),
                        false => blockService.GetObjectsAt(coord)
                    };

                    foreach (var obj in blockObjects.Where(o => !checkedObjects.Contains(o)))
                    {
                        relative = obj.Placement.Coordinates - center;
                        var data = new BlockObjectData
                        {
                            PrefabName = obj.GetComponentFast<PrefabSpec>().PrefabName,
                            Coordinates = relative,
                            Orientation = obj.Placement.Orientation,
                            Flippable = obj.GetComponentFast<BlockObjectSpec>().Flippable,
                            FlipMode = obj.Placement.FlipMode,
                            Growth = obj.TryGetComponentFast(out Growable growable) ? (growable.IsGrown ? 1 : 0) : -1
                        };

                        // Utils.Log(data.ToString());

                        _objectsInternal.Add(data);
                        checkedObjects.Add(obj);
                    }
                }
            }
            catch (Exception) when (LogInternalBuffers()) { } // Log internals, no catch
        }

        public Vector3Int UpdatePosition(Vector3Int from, Vector3Int toInclusive)
        {
            return _center = (toInclusive + from) / 2;
        }

        public void Rotate(bool clockwise)
        {
            _orientation = clockwise ? NextOrientationClockwise(_orientation) : NextOrientationCounterClockwise(_orientation);
        }

        public Vector3Int Transform(Vector3Int absoluteGridCoord)
        {
            return _center + _orientation.Transform(_flipMode.Transform(absoluteGridCoord - _center, FlipWidth()));
        }

        private Orientation NextOrientationClockwise(Orientation orientation)
        {
            return orientation switch
            {
                Orientation.Cw0 => Orientation.Cw90,
                Orientation.Cw90 => Orientation.Cw180,
                Orientation.Cw180 => Orientation.Cw270,
                Orientation.Cw270 => Orientation.Cw0,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null)
            };
        }

        private Orientation NextOrientationCounterClockwise(Orientation orientation)
        {
            return orientation switch
            {
                Orientation.Cw0 => Orientation.Cw270,
                Orientation.Cw90 => Orientation.Cw0,
                Orientation.Cw180 => Orientation.Cw90,
                Orientation.Cw270 => Orientation.Cw180,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null)
            };
        }

        private Orientation Combine(Orientation a, Orientation b)
        {
            return (Orientation)(((int)a + (int)b) % 4);
        }

        private FlipMode Combine(FlipMode a, FlipMode b)
        {
            return new FlipMode(a.IsFlipped ^ b.IsFlipped); // XOR
        }

        private Orientation Reverse(Orientation orientation)
        {
            return Combine(orientation, Orientation.Cw180);
        }

        public void Flip()
        {
            _flipMode = _flipMode.Flip();
        }

        public IEnumerable<KeyValuePair<Vector3Int, bool>> GetTerrain()
        {
            // Utils.Log($"Selection.GetTerrain - center: {_center}");
            int width = FlipWidth();
            // TODO: Investigate caching the result to avoid allocating new keyvaluepairs on each call
            foreach (var (relativeCoord, isTerrain) in _terrainInternal)
            {
                // Utils.Log("Selection.GetTerrain iteration");
                // Utils.Log($"relativeCoord: {relativeCoord}");
                // Utils.Log($"isTerrain: {isTerrain}");
                // Utils.Log($"Size.x: {Size.x}");
                // Utils.Log($"FlipWidth: {width}");

                yield return new(
                    _orientation.Transform(_flipMode.Transform(relativeCoord, width)) + _center,
                    isTerrain
                );
            }
        }

        public IEnumerable<BlockObjectData> GetObjects()
        {
            // Utils.Log($"Selection.GetObjects - center: {_center}");
            int width = FlipWidth();
            foreach (var obj in _objectsInternal)
            {
                // TODO: Investigate caching the result to avoid allocating new keyvaluepairs on each call
                Orientation objOrientation;
                FlipMode objFlipMode;
                if (obj.Flippable)
                {
                    objFlipMode = Combine(_flipMode, obj.FlipMode);
                    objOrientation = Combine(obj.Orientation, _orientation);
                    if (objFlipMode.IsFlipped && (obj.Orientation == Orientation.Cw0 || obj.Orientation == Orientation.Cw180))
                    {
                        objOrientation = Reverse(objOrientation);
                    }
                }
                else
                {
                    objFlipMode = obj.FlipMode;
                    objOrientation = Combine(obj.Orientation, _orientation);
                    if (_flipMode.IsFlipped && (obj.Orientation == Orientation.Cw90 || obj.Orientation == Orientation.Cw270))
                    {
                        objOrientation = Reverse(objOrientation);
                    }
                }

                // Utils.Log("Selection.GetObjects iteration");
                // Utils.Log($"objOrientation: {objOrientation}");
                // Utils.Log($"objFlipMode: {objFlipMode}");
                // Utils.Log($"obj.Coordinates: {obj.Coordinates}");
                // Utils.Log($"Size.x: {Size.x}");
                // Utils.Log($"FlipWidth: {width}");

                yield return new BlockObjectData
                {
                    PrefabName = obj.PrefabName,
                    Coordinates = _center + _orientation.Transform(_flipMode.Transform(obj.Coordinates, width)), // obj.Coordinates contains relative coordinates internally
                    Orientation = objOrientation,
                    FlipMode = objFlipMode,
                    Growth = obj.Growth
                };
            }
        }


        private int FlipWidth()
        {
            return Size.x % 2 == 0 ? 2 : 1;
        }
        private IEnumerable<Vector3Int> AllCoordinates(Vector3Int from, Vector3Int to)
        {
            var a = Mathf.Abs(to.x - from.x);
            var b = Mathf.Abs(to.y - from.y);
            var c = Mathf.Abs(to.z - from.z);

            var minX = Math.Min(from.x, to.x);
            var minY = Math.Min(from.y, to.y);
            var minZ = Math.Min(from.z, to.z);

            // Less then or equal to, so we include both endpoints
            for (int z = 0; z <= c; z++)
            {
                for (int y = 0; y <= b; y++)
                {
                    for (int x = 0; x <= a; x++)
                    {
                        yield return new Vector3Int(minX + x, minY + y, minZ + z);
                    }
                }
            }
        }

        private bool LogInternalBuffers()
        {
            Utils.Log("--------------------------------------------------");
            Utils.Log("Selection.Scan failed");
            Utils.Log("--------------------------------------------------");
            Utils.Log($"Terrain buffer contents: {_terrainInternal.Count}");
            foreach (var (key, value) in _terrainInternal)
            {
                Utils.Log($"Key: {key}, Value: {value}");
            }
            Utils.Log("--------------------------------------------------");
            Utils.Log($"Object buffer contents: {_objectsInternal.Count}");
            foreach (var objData in _objectsInternal)
            {
                Utils.Log(objData.ToString());
            }
            Utils.Log("--------------------------------------------------");

            return false;
        }
    }
}