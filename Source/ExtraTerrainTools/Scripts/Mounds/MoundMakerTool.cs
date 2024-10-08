
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Timberborn.BlockSystem;
using Timberborn.CameraSystem;
using Timberborn.Common;
using Timberborn.GridTraversing;
using Timberborn.InputSystem;
using Timberborn.MapStateSystem;
using Timberborn.Rendering;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
using Timberborn.ToolSystem;
using UnityEngine;

namespace TerrainTools.MoundMaker
{
    public class MoundMakerTool : ITerrainTool, IInputProcessor, ILoadableSingleton
    {
        public static readonly string ToolTitle = "Mound Maker";
        private static readonly string ToolDescriptionText = "Make a Mound";

        public static readonly string ReseedKey = "CycleMode"; // Tab
        public static readonly string CancelKey = "RotateClockwise"; // R
        public static readonly string FlipModeKey = "InverseBrushDirection"; // Ctrl

        private static readonly float MarkerYOffset = 0.02f;
        // private static readonly Color NeutralTileColor = new(0.8f, 0.8f, 0.8f, 0.5f);

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
        private readonly BlockService _blockService;
        private readonly TerrainPicker _terrainPicker;
        private readonly CameraComponent _cameraComponent;
        private readonly MarkerDrawerFactory _markerDrawerFactory;

        private MeshDrawer _meshDrawer;
        private ToolDescription _toolDescription;
        private bool _dragging = false;
        private Vector3Int _selectionCenter;
        private Vector3Int _selectionEnd;
        private string _seed = null;
        public event EventHandler<string> SeedUpdated;        

        private int _maxTerrainHeight = GetMaxHeight();

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

        public MoundMakerTool(InputService inputService, ITerrainService terrainService, BlockService blockService, TerrainPicker terrainPicker, CameraComponent cameraComponent, MarkerDrawerFactory markerDrawerFactory)
        {
            _inputService = inputService;
            _terrainService = terrainService;
            _blockService = blockService;
            _terrainPicker = terrainPicker;
            _cameraComponent = cameraComponent;
            _markerDrawerFactory = markerDrawerFactory;

            PeakHeight = _maxTerrainHeight;
            MaxAdjust = _maxTerrainHeight;
        }

        public void Load()
        {
            var _builder = new ToolDescription.Builder(ToolTitle);
            //_builder.AddPrioritizedSection(ToolDescriptionText);
            // _builder.AddSection("<color=#FFA500>...</color>");
            _toolDescription = _builder.Build();

            _meshDrawer = _markerDrawerFactory.CreateTileDrawer();
        }

        public override ToolDescription Description()
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
        }

        public bool ProcessInput()
        {
            Raise = !_inputService.IsKeyHeld(FlipModeKey);

            if(_inputService.IsKeyDown(ReseedKey))
            {
                Reseed();
            }

            Ray ray = _cameraComponent.ScreenPointToRayInGridSpace(_inputService.MousePosition);
            if( !_dragging )
            {
                if( _inputService.MainMouseButtonDown && !_inputService.MouseOverUI )
                {
                    if( HasRayHitTerrain(ray, out _selectionCenter) )
                    {
                        _dragging = true;
                    }
                }
                else if( !_inputService.MouseOverUI )
                {
                    if (HasRayHitTerrain(ray, out Vector3Int hoverCoord))
                    {
                        DrawHoverTile(hoverCoord);
                    }
                }
            }
            else if( _inputService.IsKeyDown(CancelKey) )
            {
                _dragging = false;
                ResetSelection();
                return true;
            }
            else if( !_inputService.MainMouseButtonHeld )
            {
                _dragging = false;
                
                if( (_selectionEnd - _selectionCenter).sqrMagnitude > 1 )
                {
                    ApplySelection();
                    if(AutoReseed)
                    {
                        Reseed();
                    }
                }
            }
            else if( HasRayHitPlane(ray, _selectionCenter.z, out _selectionEnd) )
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

        private bool HasRayHitPlane(Ray ray, int referenceHeight, out Vector3Int where)
        {
            Vector3Int? coordinates = _terrainPicker.FindCoordinatesOnLevelInMap(ray, referenceHeight);
            if (coordinates.HasValue)
            {
                where = coordinates.GetValueOrDefault();
                return true;
            }

            where = Vector3Int.zero;
            return false;
        }

        private void DrawHoverTile(Vector3Int coordinates)
        {
            int num = _terrainService.CellHeight( coordinates.XY() );
            _meshDrawer.DrawAtCoordinates( new Vector3Int(coordinates.x, coordinates.y, num), MarkerYOffset, HoverTileColor );
        }

        private void ApplySelection()
        {
            var adjustments = GetSelectedCoordinates();
            // Utils.Log("peak: {0}", new Vector3Int(_selectionCenter.x, _selectionCenter.y, PeakHeight));
            // Utils.Log("center: {0}", _selectionCenter.XY());
            // Utils.Log("adjustments.Count(): {0}", adjustments.Count());
            // Utils.Log("Applying selection, center: {0}, end: {1}, affected: {2}", _selectionCenter, _selectionEnd, adjustments.Count());
            foreach (var adjusted in adjustments)
            {
                var coord = adjusted.XY();
                int z = adjusted.z;
                var height = _terrainService.CellHeight(coord);
                if (_blockService.AnyObjectAtColumn(coord, height, _terrainService.Size.z))
                    continue;

                if (Mathf.Abs(z) > MaxAdjust)
                    z = MaxAdjust * Math.Sign(z);

                if( Raise )
                {
                    height += z;
                    if (height > _maxTerrainHeight)
                        height = _maxTerrainHeight;
                
                    _terrainService.SetHeight( coord, height );
                }
                else
                {
                    height -= z;
                    if (height < 1)
                        height = 1;
                
                    _terrainService.SetHeight( coord, height );
                }
            }
        }

        private IEnumerable<Vector3Int> GetSelectedCoordinates()
        {
            int radius = Mathf.RoundToInt((_selectionEnd - _selectionCenter).magnitude);

            Mound mound = new( _selectionCenter.XY(), radius, PeakHeight, _seed, MinWidthScale, 1f, RadialNoiseFreqency, RadialNoiseOctaves, VertNoiseOctaves, VertNoiseAmp, VertNoiseFreq );
            return mound.Make().Where( coord => _terrainService.Contains(coord.XY()) );
        }

        private void DrawPreviewTiles()
        {
            Vector3Int adjusted;
            Vector3Int drawAt;
            Color drawColor;
            foreach (Vector3Int coordinates in GetSelectedCoordinates())
            {
                Vector2Int coord = coordinates.XY();
                int height = _terrainService.CellHeight(coord);
                bool hasObjects = _blockService.AnyObjectAtColumn(coord, height, _terrainService.Size.z);

                adjusted = coordinates;

                if (Mathf.Abs(adjusted.z) > MaxAdjust)
                    adjusted.z = MaxAdjust * Math.Sign(adjusted.z);

                drawAt = coordinates;
                if (hasObjects)
                {
                    adjusted.z = height;
                    drawAt.z = height;
                    drawColor = BlockedTileColor;
                }
                else if (Raise)
                {
                    adjusted.z = Mathf.Clamp(height + adjusted.z, 1, _maxTerrainHeight);
                    drawAt.z = adjusted.z;
                    drawColor = HeightToColor(raiseHeightColors, adjusted.z, height, _maxTerrainHeight);
                }
                else
                {
                    adjusted.z = Mathf.Clamp(height - adjusted.z, 1, _maxTerrainHeight);
                    drawAt.z = height;
                    drawColor = HeightToColor(lowerHeightColors, adjusted.z, 1, height);
                }

                if (adjusted.z > _maxTerrainHeight)
                    adjusted.z = _maxTerrainHeight;
                else if (adjusted.z < 1)
                    adjusted.z = 1;

                
                _meshDrawer.DrawAtCoordinates(drawAt, MarkerYOffset, drawColor);
            }
        }

        public static int GetMaxHeight()
        {
            FieldInfo maxHeightField = typeof(MapSize).GetField("MaxMapEditorTerrainHeight", BindingFlags.Static | BindingFlags.Public);

			return (int)maxHeightField.GetValue(null);
        }

        private static Color HeightToColor( Color[] colors, int sample, int min, int max)
        {
            if (min >= max || sample <= min)
                return colors[0];

            if( sample >= max )
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