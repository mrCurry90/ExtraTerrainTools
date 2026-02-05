using System;
using System.Collections.Generic;
using System.Linq;
using Timberborn.BlockSystem;
using Timberborn.Coordinates;
using Timberborn.Growing;
using Timberborn.InventorySystem;
using Timberborn.NaturalResourcesModelSystem;
using Timberborn.Stockpiles;
using Timberborn.TemplateSystem;
using Timberborn.TerrainSystem;
using UnityEngine;

namespace TerrainTools.Cloning
{
    public class Selection
    {
        public struct BlockObjectData
        {
            public string TemplateName { get; set; }
            public Vector3Int Coordinates { get; set; }
            public Orientation Orientation { get; set; }
            public FlipMode FlipMode { get; set; }
            public float Growth { get; set; }
            public string GoodId { get; set; }
            public int GoodAmount { get; set; }


            public bool Equals(BlockObjectData other)
            {
                return TemplateName.Equals(other.TemplateName)
                    && Coordinates.Equals(other.Coordinates)
                    && Orientation.Equals(other.Orientation)
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
                return HashCode.Combine(TemplateName, Coordinates, Orientation, FlipMode, Growth);
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
                return $"{TemplateName}, {Coordinates}, {Orientation}, isFlipped: {FlipMode.IsFlipped}, growth: {Growth}, goodId: {GoodId}, goodAmount: {GoodAmount}";
            }
        }

        public Vector3Int Start { get { return TransformWorld(_start); } }
        public Vector3Int End { get { return TransformWorld(_end); } }
        public Vector3Int Size { get; private set; }
        public int ObjectCount { get => _objectsInternal.Count; }
        public int TerrainCount { get => _terrainInternal.Count; }

        private Vector3Int _start;
        private Vector3Int _end;
        private Vector3Int _origin;
        private Orientation _orientation = Orientation.Cw0;
        private FlipMode _flipMode = new(false);

        // Coordinates are stored relative to the center of the selection in both internal buffers
        private readonly Dictionary<Vector3Int, bool> _terrainInternal = new();
        private readonly HashSet<BlockObjectData> _objectsInternal = new();

        private readonly ITerrainService _terrainService;
        private readonly IBlockService _blockService;
        private readonly TerrainToolsManipulationService _manipulationService;

        public Selection(ITerrainService terrainService, IBlockService blockService, TerrainToolsManipulationService manipulationService)
        {
            _terrainService = terrainService;
            _blockService = blockService;
            _manipulationService = manipulationService;
        }

        public void Clear()
        {
            _terrainInternal.Clear();
            _objectsInternal.Clear();
            _orientation = Orientation.Cw0;
            _flipMode = new(false);
        }

        public void UpdatePosition(Vector3Int from, Vector3Int toInclusive)
        {
            _start = from;
            _end = toInclusive;

            _origin = new(
                Mathf.Min(_start.x, _end.x),
                Mathf.Min(_start.y, _end.y),
                Mathf.Min(_start.z, _end.z)
            );
        }

        private void UpdateDimensions(Vector3Int from, Vector3Int toInclusive)
        {
            UpdatePosition(from, toInclusive);
            Size = new(
                Mathf.Abs(toInclusive.x - from.x) + 1,
                Mathf.Abs(toInclusive.y - from.y) + 1,
                Mathf.Abs(toInclusive.z - from.z) + 1
            );
        }

        public void Scan(Vector3Int from, Vector3Int toInclusive, bool includeStacked, bool includeAir)
        {
            Clear();
            UpdateDimensions(from, toInclusive);

            try
            {

                var checkedObjects = new HashSet<BlockObject>();

                foreach (var coord in AllCoordinates(from, toInclusive))
                {
                    if (!_terrainService.Contains(coord))
                        continue;

                    var relative = coord - _origin;

                    if (includeAir)
                        _terrainInternal.Add(relative, _terrainService.Underground(coord));
                    else if (_terrainService.Underground(coord))
                        _terrainInternal.Add(relative, true);

                    var blockObjects = includeStacked switch
                    {
                        true => _blockService.GetStackedObjectsAt(coord),
                        false => _blockService.GetObjectsAt(coord)
                    };

                    foreach (var obj in blockObjects.Where(o => !checkedObjects.Contains(o)))
                    {
                        relative = obj.Coordinates - _origin;
                        var data = new BlockObjectData
                        {
                            TemplateName = obj.GetComponent<TemplateSpec>().TemplateName,
                            Coordinates = relative,
                            Orientation = obj.Orientation,
                            FlipMode = obj.FlipMode,
                            Growth = obj.TryGetComponent(out Growable growable) ? (growable.IsGrown ? 1 : 0) : -1,
                            GoodId = obj.TryGetComponent(out SingleGoodAllower goodAllower) && goodAllower.HasAllowedGood ? goodAllower.AllowedGood : "",
                            GoodAmount = obj.TryGetComponent(out Stockpile stockpile) ? stockpile.Inventory.TotalAmountInStock : 0
                        };

                        _objectsInternal.Add(data);
                        checkedObjects.Add(obj);
                    }
                }
            }
            catch (Exception) when (LogInternalBuffers()) { } // Log buffers, no catch
        }

        public void Rotate(bool clockwise)
        {
            _orientation = clockwise ? _orientation.RotateClockwise() : _orientation.RotateCounterclockwise();
        }

        public Vector3Int TransformLocal(Vector3Int coordinates)
        {
            return _orientation.Transform(_flipMode.Transform(coordinates, Size.x));
        }

        public Vector3Int TransformWorld(Vector3Int coordinates)
        {
            return _origin + _orientation.Transform(_flipMode.Transform(coordinates - _origin, Size.x));
        }

        private Orientation Add(Orientation a, Orientation b)
        {
            return (Orientation)(((int)a + (int)b) % 4);
        }

        private FlipMode XOR(FlipMode a, FlipMode b)
        {
            return new FlipMode(a.IsFlipped ^ b.IsFlipped);
        }

        private Orientation Flip(Orientation orientation)
        {
            return Add(orientation, Orientation.Cw180);
        }

        public void Flip()
        {
            _flipMode = _flipMode.Flip();
        }

        public IEnumerable<KeyValuePair<Vector3Int, bool>> GetTerrain()
        {
            // TODO: Investigate caching the result to avoid allocating new keyvaluepairs on each call
            foreach (var (coordinates, isTerrain) in _terrainInternal)
            {
                yield return new(
                    _origin + TransformLocal(coordinates),
                    isTerrain
                );
            }
        }

        public IEnumerable<BlockObjectData> GetObjects()
        {
            // TODO: Investigate caching the result to avoid allocating new keyvaluepairs on each call

            // TODO: Flipping is broken for objects, find a fix.
            foreach (var obj in _objectsInternal)
            {
                var templateSpec = _manipulationService.GetTemplateSpec(obj.TemplateName);
                var blockObjectSpec = templateSpec.GetSpec<BlockObjectSpec>();
                var placeableSpec = templateSpec.GetSpec<PlaceableBlockObjectSpec>();
                var modelRandomized = templateSpec.HasSpec<NaturalResourceModelRandomizerSpec>();
                var blocks = blockObjectSpec.GetBlocks();

                Vector3Int localCoordinates = obj.Coordinates;
                Orientation objOrientation = obj.Orientation;
                FlipMode objFlipMode = obj.FlipMode;
                Vector3Int pivotOffset = new();

                if (!modelRandomized)
                {
                    if (blockObjectSpec.Flippable && _flipMode.IsFlipped)
                    {
                        switch (obj.Orientation)
                        {
                            case Orientation.Cw0:
                                objFlipMode = obj.FlipMode.Flip();
                                pivotOffset.x = blocks.Size.x - 1;
                                break;
                            case Orientation.Cw90:
                                objFlipMode = obj.FlipMode.Flip();
                                objOrientation = Flip(objOrientation);
                                pivotOffset.x = 1 - blocks.Size.y + 1;
                                pivotOffset.y = 1 - blocks.Size.x;
                                break;
                            case Orientation.Cw180:
                                objFlipMode = obj.FlipMode.Flip();
                                pivotOffset.x = 1 - blocks.Size.x;
                                break;
                            case Orientation.Cw270:
                                objFlipMode = obj.FlipMode.Flip();
                                objOrientation = Flip(objOrientation);
                                pivotOffset.x = blocks.Size.y - 1 - 1;
                                pivotOffset.y = blocks.Size.x - 1;
                                break;
                        }
                    }
                    else if (!blockObjectSpec.Flippable && _flipMode.IsFlipped)
                    {
                        switch (obj.Orientation)
                        {
                            case Orientation.Cw0:
                                pivotOffset.x = blocks.Size.x - 1;
                                break;
                            case Orientation.Cw90:
                                objOrientation = Flip(objOrientation);
                                pivotOffset.x = 1 - blocks.Size.y + 1;
                                pivotOffset.y = 1 - blocks.Size.x;
                                break;
                            case Orientation.Cw180:
                                pivotOffset.x = 1 - blocks.Size.x;
                                break;
                            case Orientation.Cw270:
                                objOrientation = Flip(objOrientation);
                                pivotOffset.x = blocks.Size.y - 1 - 1;
                                pivotOffset.y = blocks.Size.x - 1;
                                break;
                        }
                    }
                    objOrientation = Add(objOrientation, _orientation);
                }
                // Vector3Int offset = new();
                // if (blockObjectSpec.Flippable)
                // {
                //     objFlipMode = XOR(_flipMode, obj.FlipMode);
                //     objOrientation = Add(obj.Orientation, _orientation);
                //     if (_flipMode.IsFlipped)
                //         offset = new(obj.Orientation < Orientation.Cw180 ? 1 - blocks.Size.x : blocks.Size.x - 1, 0, 0);
                //     if (obj.Orientation == Orientation.Cw90 || obj.Orientation == Orientation.Cw270)
                //     {
                //         objOrientation = Flip(objOrientation);
                //     }
                // }
                // else
                // {
                //     objFlipMode = obj.FlipMode;
                //     objOrientation = Add(obj.Orientation, _orientation);
                //     if (_flipMode.IsFlipped)
                //     {
                //         if (placeableSpec != null && placeableSpec.CustomPivot.HasCustomPivot) // Assumption: Only natural overhangs as of 1.0
                //         {
                //             objOrientation = Flip(objOrientation);
                //         }
                //         else
                //         {
                //             offset = new(1 - blocks.Size.x, 0, 0);
                //         }
                //     }
                // }

                yield return new BlockObjectData
                {
                    TemplateName = obj.TemplateName,
                    Coordinates = _origin + TransformLocal(localCoordinates + pivotOffset),
                    Orientation = objOrientation,
                    FlipMode = objFlipMode,
                    Growth = obj.Growth,
                    GoodId = obj.GoodId,
                    GoodAmount = obj.GoodAmount
                };
            }
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

        private bool LogInternalBuffers([System.Runtime.CompilerServices.CallerMemberName] string memberName = "UnknownMember")
        {
            Utils.Log("--------------------------------------------------");
            Utils.Log($"Selection.{memberName} failed");
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