using System.Collections.Generic;
using System;
using TerrainTools.EditorHistory;
using Timberborn.BlockSystem;
using Timberborn.CameraSystem;
using Timberborn.Common;
using Timberborn.GridTraversing;
using Timberborn.InputSystem;
using Timberborn.Localization;
using Timberborn.Rendering;
using Timberborn.SingletonSystem;
using Timberborn.TerrainQueryingSystem;
using Timberborn.TerrainSystem;
using Timberborn.ToolSystem;
using UnityEngine;

namespace TerrainTools.SmoothingBrush
{
    public class SmoothingBrushTool : ITerrainTool, IInputProcessor, ILoadableSingleton
    {
        public override string Icon { get; } = "SmoothingToolIcon";

        private readonly string _keyToolTitle = "TerrainTools.Smoothing.Tool.Title"; // Smoothing Brush

        private static readonly float MarkerYOffset = 0.02f;
        private static readonly Color PositiveTileColor = new(0f, 1f, 0f, 0.7f);
        private static readonly Color BlockedTileColor = new(0.5f, 0.5f, 0.5f, 0.7f);

        private readonly InputService _inputService;
        private readonly ITerrainService _terrainService;
        private readonly IBlockService _blockService;
        private readonly TerrainPicker _terrainPicker;
        private readonly CameraService _cameraService;
        private readonly MarkerDrawerFactory _markerDrawerFactory;
        private readonly EditorHistoryService _historyService;

        private MeshDrawer _meshDrawer;
        private ToolDescription _toolDescription;
        private Vector2Int _center;

        public int Size { get; set; }
        public int SampleSize { get; set; } = 1;
        public float Strength { get; set; } = 0.5f;
        public bool Circular { get; set; } = false;
        public bool UseWeightedSampling { get; set; } = false;
        public bool UseCircularSampling { get; set; } = false;
        public float Force { get; set; } = 1;
        public int Radial { get; set; } = 1;

        private float _tickRate = 1f / 8f;
        private float _timer;

        public SmoothingBrushTool(
            InputService inputService, ITerrainService terrainService, IBlockService blockService,
            TerrainPicker terrainPicker, CameraService cameraService, MarkerDrawerFactory markerDrawerFactory,
            EditorHistoryService historyService, ILoc loc
        ) : base(loc)
        {
            _inputService = inputService;
            _terrainService = terrainService;
            _blockService = blockService;
            _terrainPicker = terrainPicker;
            _cameraService = cameraService;
            _markerDrawerFactory = markerDrawerFactory;
            _historyService = historyService;

            Size = 3;
        }

        public void Load()
        {
            var _builder = new ToolDescription.Builder(_loc.T(_keyToolTitle));
            _toolDescription = _builder.Build();

            _meshDrawer = _markerDrawerFactory.CreateTileDrawer();

        }

        public override ToolDescription Description()
        {
            return _toolDescription;
        }

        public override void Enter()
        {
            _inputService.AddInputProcessor(this);
        }

        public override void Exit()
        {
            _inputService.RemoveInputProcessor(this);
        }

        public bool ProcessInput()
        {
            Ray ray = _cameraService.ScreenPointToRayInGridSpace(_inputService.MousePosition);
            if (_inputService.MainMouseButtonHeld && !_inputService.MouseOverUI)
            {
                if (_inputService.MainMouseButtonDown)
                {
                    _historyService.BatchStart();
                }

                if (HasRayHitTerrain(ray, out Vector3Int center))
                {
                    _center = center.XY();

                    if (_timer == 0)
                    {
                        ApplyBrush();
                    }
                    DrawPreviewTiles();
                }

                _timer += Time.unscaledDeltaTime;
                if (_timer >= _tickRate) _timer = 0;
            }
            else
            {
                _timer = 0;
                if (!_inputService.MouseOverUI)
                {
                    if (_inputService.MainMouseButtonUp)
                    {
                        _historyService.BatchStop();
                    }

                    if (HasRayHitTerrain(ray, out Vector3Int center))
                    {
                        _center = center.XY();
                        DrawPreviewTiles();
                    }
                }
            }
            return false;
        }

        private void ApplyBrush()
        {

            int height;
            float newHeight;
            List<Tuple<Vector3Int, int>> updated = new();

            foreach (Vector2Int coord in GetNeighbors(_center))
            {
                height = _terrainService.CellHeight(coord);
                if (_blockService.AnyObjectAtColumn(coord, height, _terrainService.Size.z))
                    continue;

                newHeight = SampleHeight(coord);
                newHeight = height + Force * (newHeight - height);

                updated.Add(
                    new(
                        new(coord.x, coord.y, height),
                        Mathf.RoundToInt(newHeight) - height
                    )
                );
            }

            foreach (var tuple in updated)
            {
                _terrainService.AdjustTerrain(tuple.Item1, tuple.Item2);
            }
        }

        private bool HasRayHitTerrain(Ray ray, out Vector3Int where)
        {
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

        private void DrawPreviewTiles()
        {
            Vector3Int drawCoord;
            foreach (Vector2Int coord in GetNeighbors(_center))
            {
                drawCoord = coord.XYZ();
                drawCoord.z = _terrainService.CellHeight(coord);
                bool hasBlocks = _blockService.AnyObjectAtColumn(coord, drawCoord.z, _terrainService.Size.z);

                _meshDrawer.DrawAtCoordinates(drawCoord, MarkerYOffset, hasBlocks ? BlockedTileColor : PositiveTileColor);
            }
        }

        private List<Vector2Int> GetNeighbors(Vector2Int coord)
        {
            List<Vector2Int> selected = new();

            Vector2Int start = new(coord.x - Size, coord.y - Size),
                        end = new(coord.x + Size + 1, coord.y + Size + 1);

            for (int y = start.y; y < end.y; y++)
            {
                for (int x = start.x; x < end.x; x++)
                {
                    Vector2Int p = new(x, y);
                    if (Circular && (p - coord).magnitude > Size)
                        continue;

                    if (_terrainService.Contains(p))
                        selected.Add(p);
                }
            }

            return selected;
        }

        private Heightmap BuildHeightmap()
        {
            return new(_terrainService);
        }

        private float GetBrushStrength2(Vector2 point)
        {
            float x = Mathf.Abs(point.x - _center.x) / Size;
            float y = Mathf.Abs(point.y - _center.y) / Size;

            x *= x;
            y *= y;

            return Mathf.Max(0, 1 - Mathf.Pow(x + y, Force));
        }

        private float SampleHeight(Vector2Int coord)
        {
            Vector2Int start = new(coord.x - SampleSize, coord.y - SampleSize),
                        end = new(coord.x + SampleSize + 1, coord.y + SampleSize + 1),
                        pos = new();

            float sample = 0;
            int n = 0;
            for (pos.y = start.y; pos.y < end.y; pos.y++)
            {
                for (pos.x = start.x; pos.x < end.x; pos.x++)
                {
                    if (_terrainService.Contains(pos))
                    {
                        sample += _terrainService.CellHeight(pos);
                        n++;
                    }
                }
            }

            return sample / n;
        }

        private void HeightmapToLog(Heightmap heightmap)
        {
            string[] rows = heightmap.RowsToString();
            if (rows.Length < 1)
                return;

            string columns = "Col:";
            for (int i = 0; i < heightmap.Size.x; i++)
            {
                columns += i.ToString().PadLeft(4, ' ');
            }

            Utils.Log(columns);

            string rowIndex;
            for (int i = 0; i < rows.Length; i++)
            {
                rowIndex = i.ToString().PadLeft(3, ' ');

                Utils.Log("{0}:{1}", rowIndex, rows[i]);
            }
        }
    }
}