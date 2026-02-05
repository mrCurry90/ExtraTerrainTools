
using System.Collections.Generic;
using System.Linq;
using System;
using Timberborn.BlockSystem;
using Timberborn.CameraSystem;
using Timberborn.Common;
using Timberborn.GridTraversing;
using Timberborn.InputSystem;
using Timberborn.MapStateSystem;
using Timberborn.Rendering;
using Timberborn.SingletonSystem;
using Timberborn.TerrainQueryingSystem;
using Timberborn.TerrainSystem;
using Timberborn.BlockObjectPickingSystem;
using UnityEngine;
using Timberborn.Localization;
using Timberborn.UndoSystem;
using Timberborn.ToolSystemUI;

namespace TerrainTools.MoundMaker
{
    public class MoundMakerTool : TerrainTool, IInputProcessor, ILoadableSingleton
    {
        private struct Adjustment
        {
            public Vector3Int Coord;
            public int Size;

            public Adjustment(Vector3Int coordinate, int size)
            {
                Coord = coordinate;
                Size = size;
            }
        }

        public override string Icon { get; } = "MoundToolIcon";

        private readonly string _keyToolTitle = "TerrainTools.MoundMaker.Tool.Title"; // Mound Maker

        private static readonly string _reseedKeybind = "ExtraTerrainTools.CycleMode"; // Tab
        private static readonly string _flipModeKeybind = "InverseBrushDirection"; // Ctrl - TODO make keybinding
        public static string ReseedKeybind => _reseedKeybind;
        public static string FlipModeKeybind => _flipModeKeybind;

        private static readonly float MarkerYOffset = 0.02f;
        private static readonly Color[] raiseHeightColors = {
            new(0f, 1f, 0f, 0.7f), // Low = Green
            new(1f, 1f, 0f, 0.7f), // Mid = Yellow
            new(1f, 0f, 0f, 0.7f)  // High = Red
        };

        private static readonly Color[] lowerHeightColors = {
            new(0.5f, 0f, 0.5f, 0.7f), // Low = Purple
            new(0f, 0f, 1f, 0.7f),  // Mid = Blue
            new(0f, 1f, 1f, 0.7f) // High = Cyan
        };

        private static readonly Color BlockedTileColor = new(0.5f, 0.5f, 0.5f, 0.7f);
        private static readonly Color HoverTileColor = new(0f, 1f, 1f, 0.7f);

        private readonly InputService _inputService;
        private readonly ITerrainService _terrainService;
        private readonly IBlockService _blockService;
        private readonly TerrainPicker _terrainPicker;
        private readonly CameraService _cameraService;
        private readonly MarkerDrawerFactory _markerDrawerFactory;
        private readonly BlockObjectRaycaster _blockObjectRaycaster;
        private readonly IUndoRegistry _undoRegistry;
        private readonly TerrainToolsManipulationService _manipulationService;
        private readonly MapSize _mapSize;

        private MeshDrawer _meshDrawer;
        private ToolDescription _toolDescription;
        private bool _dragging = false;
        private Vector3Int _selectionCenter;
        private Vector3Int _selectionEnd;
        private string _seed = null;
        public event EventHandler<string> SeedUpdated;

        public int MaxTerrainHeight => _mapSize.MaxMapEditorTerrainHeight;

        public int PeakHeight { get; set; }
        public float MinWidthScale { get; set; }
        public int RadialNoiseOctaves { get; set; }
        public float RadialNoiseFreqency { get; set; }
        public int VertNoiseOctaves { get; set; }
        public float VertNoiseAmp { get; set; }
        public float VertNoiseFreq { get; set; }
        public int MaxAdjust { get; set; }
        public bool Raise { get; set; } = true;
        public bool AutoReseed { get; set; } = true;


        public MoundMakerTool(
            InputService inputService,
            ITerrainService terrainService,
            IBlockService blockService,
            TerrainPicker terrainPicker,
            CameraService cameraService,
            MarkerDrawerFactory markerDrawerFactory,
            BlockObjectRaycaster blockObjectRaycaster,
            IUndoRegistry undoRegistry,
            MapSize mapSize,
            TerrainToolsManipulationService manipulationService,
            ILoc loc
        ) : base(loc)
        {
            _inputService = inputService;
            _terrainService = terrainService;
            _blockService = blockService;
            _terrainPicker = terrainPicker;
            _cameraService = cameraService;
            _markerDrawerFactory = markerDrawerFactory;
            _blockObjectRaycaster = blockObjectRaycaster;
            _undoRegistry = undoRegistry;
            _manipulationService = manipulationService;
            _mapSize = mapSize;
        }

        public void Load()
        {
            PeakHeight = MaxTerrainHeight;
            MaxAdjust = MaxTerrainHeight;

            var _builder = new ToolDescription.Builder(_loc.T(_keyToolTitle));
            _toolDescription = _builder.Build();

            // _meshDrawer = _markerDrawerFactory.CreateTileDrawer();
            _meshDrawer = _markerDrawerFactory.CreateSmallBlockTileDrawer();
        }

        public override ToolDescription DescribeTool()
        {
            return _toolDescription;
        }

        public override void Enter()
        {
            Raise = true;
            Reseed();
            ResetSelection();
            _dragging = false;
            _inputService.AddInputProcessor(this);
        }

        public override void Exit()
        {
            ResetSelection();
            _dragging = false;
            _inputService.RemoveInputProcessor(this);
            _undoRegistry.CommitStack();
        }

        public bool ProcessInput()
        {
            Raise = !_inputService.IsKeyHeld(FlipModeKeybind);

            if (_inputService.IsKeyDown(ReseedKeybind))
            {
                Reseed();
            }

            Ray ray = _cameraService.ScreenPointToRayInGridSpace(_inputService.MousePosition);
            if (!_dragging)
            {
                if (_inputService.MainMouseButtonDown && !_inputService.MouseOverUI)
                {
                    if (HasRayHitTerrain(ray, out _selectionCenter))
                    {
                        _dragging = true;
                    }
                }
                else if (!_inputService.MouseOverUI)
                {
                    if (HasRayHitTerrain(ray, out Vector3Int hoverCoord))
                    {
                        DrawHoverTile(hoverCoord);
                    }
                }
            }
            // else if( _inputService.IsKeyDown(CancelKeybind) )
            else if (_inputService.Cancel)
            {
                _dragging = false;
                ResetSelection();
                return true;
            }
            else if (!_inputService.MainMouseButtonHeld)
            {
                _dragging = false;

                if ((_selectionEnd - _selectionCenter).sqrMagnitude > 1)
                {
                    ApplySelection();
                    if (AutoReseed)
                    {
                        Reseed();
                    }
                }
            }
            else if (HasRayHitPlane(ray, _selectionCenter.z, out _selectionEnd))
            {
                DrawPreviewTiles();
            }

            return false;
        }

        private void ResetSelection()
        {
            _selectionCenter = Vector3Int.zero;
            _selectionEnd = Vector3Int.zero;
        }

        private bool HasRayHitTerrain(Ray ray, out Vector3Int where)
        {
            if (_blockObjectRaycaster.TryHitBlockObject<BlockObject>(ray, out var blockObjectHit) && ValidateBlockObjectHit(blockObjectHit, out var hitCoord))
            {
                where = hitCoord.Above();
                return true;
            }

            TraversedCoordinates? traversedCoordinates = _terrainPicker.PickTerrainCoordinates(ray);
            if (traversedCoordinates.HasValue)
            {
                TraversedCoordinates valueOrDefault = traversedCoordinates.GetValueOrDefault();
                where = valueOrDefault.Coordinates + valueOrDefault.Face;
                return true;
            }

            where = Vector3Int.zero;
            return false;
        }

        private static bool ValidateBlockObjectHit(BlockObjectHit hit, out Vector3Int coordinates)
        {
            coordinates = hit.HitBlock.Coordinates;
            if (hit.BlockObject.PositionedBlocks.TryGetBlock(coordinates, out var result))
            {
                return result.Stackable == BlockStackable.BlockObject;
            }
            return false;
        }

        private bool HasRayHitPlane(Ray ray, int referenceHeight, out Vector3Int where)
        {
            TraversedCoordinates? coordinates = _terrainPicker.FindCoordinatesOnLevelInMap(ray, referenceHeight);
            if (coordinates.HasValue)
            {
                where = coordinates.GetValueOrDefault().Coordinates;
                return true;
            }

            where = Vector3Int.zero;
            return false;
        }

        private void DrawHoverTile(Vector3Int coordinates)
        {
            _meshDrawer.DrawAtCoordinates(coordinates, MarkerYOffset, HoverTileColor);
        }

        private bool TryGetTerrainHeight(Vector3Int coord, out int relativeHeight)
        {
            if (!_terrainService.TryGetRelativeHeight(coord, out relativeHeight))
                return false;

            // Utils.Log("TryGetTerrainHeight - coord: {0}", coord);

            if (_terrainService.OnGround(coord) || _terrainService.Underground(coord))
                return true; // If we're on or under ground the return from TryGetRelativeHeight will be correct

            // Utils.Log("TryGetRelativeHeight - relativeHeight: {0}", relativeHeight);

            TryGetRelativeHeightOfObjectBelow(coord, Mathf.Abs(relativeHeight), out relativeHeight);

            // Utils.Log("TryGetDistanceToObjectBelow - relativeHeight: {0}", relativeHeight);


            return true;
        }

        private bool TryGetRelativeHeightOfObjectBelow(Vector3Int coord, int maxDistance, out int distance)
        {
            var objectsInColumn = _blockService.GetObjectsAtColumn(coord.XY(), coord.z - maxDistance, coord.z);
            if (objectsInColumn.Count() == 0)
            {
                distance = -maxDistance;
                return false;
            }

            coord.z -= 1;

            var occupiedCoordsInColumn = objectsInColumn.Last().PositionedBlocks.GetOccupiedCoordinates().Where(c => c.x == coord.x && c.y == coord.y);
            foreach (var coord2 in occupiedCoordsInColumn)
            {
                int dist = coord.z - coord2.z;
                if (dist < maxDistance)
                {
                    maxDistance = dist;
                }
            }

            distance = -maxDistance;
            return true;
        }

        private bool TryGetDistanceToObjectAbove(Vector3Int coord, int maxDistance, out int distance)
        {
            var objectsInColumn = _blockService.GetObjectsAtColumn(coord.XY(), coord.z, coord.z + maxDistance);
            if (objectsInColumn.Count() == 0)
            {
                distance = maxDistance;
                return false;
            }

            var occupiedCoordsInColumn = objectsInColumn.First().PositionedBlocks.GetOccupiedCoordinates().Where(c => c.x == coord.x && c.y == coord.y);
            foreach (var coord2 in occupiedCoordsInColumn)
            {
                int dist = coord2.z - coord.z;
                if (dist < maxDistance)
                {
                    maxDistance = dist;
                }
            }

            distance = maxDistance;
            return true;
        }

        private bool IsEmptyAndStackable(Vector3Int coord)
        {
            var below = coord.Below();

            // Utils.Log("IsEmptyAndStackable - coord: {0}", coord);
            // Utils.Log("IsEmptyAndStackable - below: {0}", below);

            // Utils.Log("IsEmptyAndStackable - !AnyObjectAt(coord): {0}", !_blockService.AnyObjectAt(coord));
            // Utils.Log("IsEmptyAndStackable - Underground(below): {0}", _terrainService.Underground(below));
            // Utils.Log("IsEmptyAndStackable - Topobject(below): {0}", _blockService.GetTopObjectComponentAt<BlockObject>(below));
            // Utils.Log("IsEmptyAndStackable - Stackable: {0}", _blockService.GetTopObjectAt(below)?.PositionedBlocks.GetBlock(below).Stackable);

            return !_blockService.AnyObjectAt(coord) && (
                _terrainService.Underground(below)
                ||
                // _blockService.GetTopObjectAt(below)?.PositionedBlocks?.GetBlock(below).Stackable == BlockStackable.BlockObject
                _blockService.GetTopObjectComponentAt<BlockObject>(below)?.PositionedBlocks?.GetBlock(below).Stackable == BlockStackable.BlockObject
            );
        }

        private void ApplySelection()
        {
            // Utils.Log("peak: {0}", new Vector3Int(_selectionCenter.x, _selectionCenter.y, PeakHeight));
            // Utils.Log("center: {0}", _selectionCenter.XY());
            // Utils.Log("adjustments.Count(): {0}", adjustments.Count());
            // Utils.Log("Applying selection, center: {0}, end: {1}, affected: {2}", _selectionCenter, _selectionEnd, adjustments.Count());

            Vector3Int coord3;
            foreach (var adjustment in GetTerrainAdjustments())
            {
                coord3 = adjustment.Coord;
                int z = adjustment.Size;

                if (!TryGetTerrainHeight(coord3, out int terrainHeight))
                    continue;

                coord3.z += terrainHeight;

                if (Mathf.Abs(z) > MaxAdjust)
                    z = MaxAdjust * Math.Sign(z);

                if (Raise)
                {
                    // Limit within map editor threshold
                    if (coord3.z + z > MaxTerrainHeight)
                        z = MaxTerrainHeight - coord3.z;

                    // Cut off below objects
                    if (IsEmptyAndStackable(coord3))
                    {
                        TryGetDistanceToObjectAbove(coord3, z, out z);
                        _terrainService.SetTerrain(coord3, z);
                    }
                }
                else
                {
                    // UnsetTerrain limits the z to be above min value
                    coord3.z -= 1;
                    if (_manipulationService.CanRemoveTerrain(coord3))
                    {
                        _terrainService.UnsetTerrain(coord3, z);
                    }
                }
            }

            _undoRegistry.CommitStack();
        }

        private IEnumerable<Adjustment> GetTerrainAdjustments()
        {
            int radius = Mathf.RoundToInt((_selectionEnd - _selectionCenter).magnitude);

            return from adjustment in new Mound(_selectionCenter.XY(), radius, PeakHeight, _seed, MinWidthScale, 1f, RadialNoiseFreqency, RadialNoiseOctaves, VertNoiseOctaves, VertNoiseAmp, VertNoiseFreq).Make()
                   where _terrainService.Contains(adjustment.XY())
                   select new Adjustment(new Vector3Int(adjustment.x, adjustment.y, _selectionCenter.z), adjustment.z);
        }

        private void DrawPreviewTiles()
        {
            Vector3Int coordinates, terrainCoord;
            Vector3Int drawAt;
            Color drawColor;
            bool canApply;
            foreach (var adjustment in GetTerrainAdjustments())
            {
                int adjustZ = adjustment.Size;

                coordinates = adjustment.Coord;
                if (!TryGetTerrainHeight(coordinates, out int height))
                    continue;

                coordinates.z += height;
                terrainCoord = coordinates;

                if (Mathf.Abs(adjustment.Size) > MaxAdjust)
                    adjustZ = MaxAdjust * Math.Sign(adjustment.Size);

                if (adjustZ == 0)
                    continue;

                if (Raise)
                {
                    TryGetDistanceToObjectAbove(terrainCoord, adjustZ, out adjustZ);

                    coordinates.z = Mathf.Clamp(coordinates.z + adjustZ, 0, MaxTerrainHeight);

                    canApply = IsEmptyAndStackable(terrainCoord);
                    drawColor = HeightToColor(raiseHeightColors, coordinates.z, terrainCoord.z, MaxTerrainHeight);
                    drawAt = coordinates;
                }
                else
                {
                    coordinates.z = Mathf.Clamp(terrainCoord.z - adjustZ, 0, MaxTerrainHeight);
                    drawColor = HeightToColor(lowerHeightColors, coordinates.z, 0, terrainCoord.z);
                    drawAt = new(coordinates.x, coordinates.y, terrainCoord.z);
                    canApply = _manipulationService.CanRemoveTerrain(drawAt);
                }

                if (!canApply)
                {
                    drawAt.z = terrainCoord.z;
                    drawColor = BlockedTileColor;
                }

                drawAt.z -= 1;

                if (drawAt.z > MaxTerrainHeight)
                    drawAt.z = MaxTerrainHeight;
                else if (drawAt.z < 0)
                    drawAt.z = 0;


                _meshDrawer.DrawAtCoordinates(drawAt, MarkerYOffset, drawColor);
            }
        }

        private static Color HeightToColor(Color[] colors, int sample, int min, int max)
        {
            if (min >= max || sample <= min)
                return colors[0];

            if (sample >= max)
                return colors.Last();

            float d = (colors.Count() - 1) * (sample - min) / (float)(max - min);
            int i = Mathf.FloorToInt(d);
            d -= i;
            return Color.Lerp(colors[i], colors[i + 1], d);
        }

        private void Reseed()
        {
            _seed = Time.realtimeSinceStartupAsDouble.GetHashCode().ToString();
            SeedUpdated.Invoke(this, _seed);
        }


    }
}