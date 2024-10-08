using System.Collections.Generic;
using System.IO;
using System.Linq;
using TerrainTools.EditorHistory;
using Timberborn.AssetSystem;
using Timberborn.BlockSystem;
using Timberborn.CameraSystem;
using Timberborn.Common;
using Timberborn.GridTraversing;
using Timberborn.InputSystem;
using Timberborn.Rendering;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
using Timberborn.ToolSystem;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace TerrainTools.PathPainterTool
{
    public class PathPainterTool : ITerrainTool, IInputProcessor, ILoadableSingleton
    {

        public static readonly string ToolTitle = "Path Painter";
        private static readonly string ToolDescriptionText = "Draw paths";

        public static readonly string CancelKey = "RotateClockwise"; // R
        private static readonly float MarkerYOffset = 0.02f;
        private static readonly Color PositiveTileColor = new(0f, 1f, 0f, 0.5f);
        private static readonly Color BlockedTileColor = new(0.5f, 0.5f, 0.5f, 0.7f);
        private static readonly Color HoverTileColor = new(0f, 1f, 1f, 0.7f);
        private static readonly Color LineTileColor = new(0.8f, 0.8f, 0.8f, 0.7f);

        private readonly InputService _inputService;
        private readonly ITerrainService _terrainService;
        private readonly BlockService _blockService;
        private readonly TerrainPicker _terrainPicker;
        private readonly CameraComponent _cameraComponent;
        private readonly MarkerDrawerFactory _markerDrawerFactory;
        private readonly EditorHistoryService _historyService;
        private readonly MeshDrawerFactory _meshDrawerFactory;
        private readonly IAssetLoader _assetLoader;

        private MeshDrawer _markerDrawer;
        private MeshDrawer _splineDrawer;
        private ToolDescription _toolDescription;

        // TODO List of points, Investigate Spline Runtime implementation of Unity
        //private Stack<Vector3Int> _nodes;
        private Vector3Int _mouseCoord;
        private int _maxNodes = 4;
        private Spline _spline;
        private int _pathWidth = 1;

        private float _splineMeshRadius = 1f;
        private Mesh _splineMesh;
        private Material _splineMaterial;
        private Color _splineColor = Color.red;

        public PathPainterTool(
            InputService inputService, ITerrainService terrainService, BlockService blockService, TerrainPicker terrainPicker, CameraComponent cameraComponent, 
            MarkerDrawerFactory markerDrawerFactory, EditorHistoryService historyService, MeshDrawerFactory meshDrawerFactory, IAssetLoader assetLoader
        )
        {
            _inputService = inputService;
            _terrainService = terrainService;
            _blockService = blockService;
            _terrainPicker = terrainPicker;
            _cameraComponent = cameraComponent;
            _markerDrawerFactory = markerDrawerFactory;
            _historyService = historyService;
            _meshDrawerFactory = meshDrawerFactory;
            _assetLoader = assetLoader;
        }

        public void Load()
        {
            var _builder = new ToolDescription.Builder(ToolTitle);
            _builder.AddPrioritizedSection(ToolDescriptionText);
            // _builder.AddSection("<color=#FFA500>...</color>");
            _toolDescription = _builder.Build();

            _splineMaterial = _assetLoader.Load<Material>(
                Path.Combine("Materials/SplineEditor", "SplineMaterial")
            );

            _markerDrawer = _markerDrawerFactory.CreateTileDrawer();
            _splineDrawer = _meshDrawerFactory.Create(_splineMesh, _splineMaterial, _splineColor);

            ResetSpline();
        }

        public void ResetSpline()
        {
            _spline = new();
        }

        public override ToolDescription Description()
        {
            return _toolDescription;
        }

        public override void Enter()
        {
            _inputService.AddInputProcessor(this);

            Spline.Changed += OnSplineChanged;
        }        

        public override void Exit()
        {
            _inputService.RemoveInputProcessor(this);
            Spline.Changed -= OnSplineChanged;
        }

        private void OnSplineChanged(Spline spline, int index, SplineModification modification)
        {
            SplineMesh.Extrude(_spline, _splineMesh, _splineMeshRadius, 3, 24 * _spline.Count(), true);
        }

        public bool ProcessInput()
        {
            Ray ray = _cameraComponent.ScreenPointToRayInGridSpace(_inputService.MousePosition);
            bool mouseOverTerrain = HasRayHitTerrain(ray, out Vector3Int _mouseCoord);
            int numKnots = _spline.Count();

            if(_inputService.Cancel)
            {
                
            }
            else if(!_inputService.MouseOverUI && _inputService.MainMouseButtonDown && numKnots <= _maxNodes)
            {
                if(mouseOverTerrain)
                {
                    Utils.Log("Adding new knot at {0}", _mouseCoord);
                    _spline.Add( BezierKnotFrom( _mouseCoord ) );
                    
                }
            }
            else if(!_inputService.MouseOverUI && _inputService.MainMouseButtonDown)
            {
                Utils.Log("Applying brush", _mouseCoord);
                Apply();
            }
            else if(_inputService.IsKeyDown(CancelKey))
            {
                if (!_spline.IsEmpty())
                {
                    _spline.RemoveAt(_spline.Count - 1);
                }
            }

            DrawSpline();

            numKnots = _spline.Count();

            if( numKnots == 0 )
            {
                DrawPreviewTile( _mouseCoord, HoverTileColor );
            }
            else if( numKnots == 1 )
            {
                //DrawPreviewTiles( HoverTileColor );
            }
            else if( numKnots >= _maxNodes )
            {
                //MakeSplineFromNodes( endAtMouse: false );
                //DrawPreviewTiles( PositiveTileColor );
            }
            else if( numKnots > 1 )
            {
                //MakeSplineFromNodes( endAtMouse: true );
                //DrawPreviewTiles( LineTileColor );
            }
            else 
            {
                
            }

            return false;
        }

        private BezierKnot BezierKnotFrom( Vector3Int coord )
        {
            return default;
        }

        private void MakeSplineFromNodes( bool endAtMouse )
        {
            // List<float3> col = new();
            // foreach (Vector3Int coord in _nodes)
            // {
            //     col.Add( XYOf(coord) );
            // }

            // if( endAtMouse )
            // {
            //     col.Add( XYOf(_mouseCoord) );
            // }

            // _spline = SplineFactory.CreateCatmullRom(col);
        }

        private float3 XYOf( Vector3Int coord )
        {
            float3 tmp = coord.ToFloat3();
            tmp.z = 0;

            return tmp;
        }

        private void Apply()
        {
            _historyService.BatchStart();

            Vector3Int coordinates;
            float distance;
            foreach (var selected in GetSelectedCoordinates())
            {
                coordinates = selected.Key;
                distance = selected.Value;


                _terrainService.SetHeight(coordinates.XY(), coordinates.z);
            }

            _historyService.BatchStop();
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

        private Dictionary<Vector3Int, float> GetSelectedCoordinates()
        {
            float length = _spline.GetLength();
            float3 p1, p2;

            Dictionary<Vector3Int, float> selected = new();
            for (int i = 0; i < length; i++)
            {
                _spline.Evaluate(i / length, out float3 position, out float3 forward, out float3 upVector);
                float3 right = Vector3.Cross(forward, upVector);
                p1 = position + (right * _pathWidth);
                p2 = position + (-right * _pathWidth);

                Vector2Int min = new(
                    Mathf.FloorToInt( Mathf.Min(p1.x, p2.x) ),
                    Mathf.FloorToInt( Mathf.Min(p1.y, p2.y) )
                );
                Vector2Int max = new(
                    Mathf.CeilToInt( Mathf.Max(p1.x, p2.x) ),
                    Mathf.CeilToInt( Mathf.Max(p1.y, p2.y) )
                );

                float3 point;
                Vector3Int grid;
                float dist;
                for (int y = min.y; y < max.y; y++)
                {
                    for (int x = min.x; y < max.x; x++)
                    {
                        point = new(x, y, 0);
                        grid = point.ToVector3Int();
                        dist = SplineUtility.GetNearestPoint(_spline, point, out float3 nearest, out float t);

                        if( selected.ContainsKey(grid) )
                        {
                            if( dist <= selected[grid] )
                            {
                                selected[grid] = dist;    
                            }
                        }
                        else if( dist <= _pathWidth )
                        {
                            selected[grid] = dist;
                        }
                    }
                }


            }
            return selected;
        }

        private void DrawPreviewTile( Vector3Int coord, Color color )
        {
            _markerDrawer.DrawAtCoordinates(coord, MarkerYOffset, color);
        }

        private void DrawPreviewTiles( Color knotColor, Color splineColor, Color fillColor )
        {
            Vector3Int coordinates;
            float distance;

            // Draw knots
            List<Vector3Int> knotCoordinates = new(_maxNodes);
            foreach (var knot in _spline)
            {
                coordinates = GetTerrainCoordinateAt(knot.Position.ToVector3Int());
                coordinates.z = _terrainService.CellHeight(coordinates.XY());
                knotCoordinates.Add(coordinates);
                _markerDrawer.DrawAtCoordinates(coordinates, MarkerYOffset, knotColor);
            }

            // Draw spline
            Dictionary<Vector2Int,float> coordDistToSpline = new();
            for (int i = 0; i < _spline.Count; i++)
            {

            }

            // Draw fill
            foreach (var selected in GetSelectedCoordinates())
            {
                coordinates = GetTerrainCoordinateAt(selected.Key);
                if( knotCoordinates.Contains(coordinates) )
                    continue;

                distance = selected.Value;
                _markerDrawer.DrawAtCoordinates(coordinates, MarkerYOffset, fillColor);
            }
        }

        private Vector3Int GetTerrainCoordinateAt(Vector3Int refCoord)
        {
            refCoord.z = _terrainService.CellHeight(refCoord.XY());
            return refCoord;
        }

        private Vector3Int GetTerrainCoordinateAt(Vector2Int coord)
        {
            Vector3Int tmp = coord.XYZ();
            tmp.z = _terrainService.CellHeight(coord);
            return tmp;
        }

        private static void GetCurveAABB(BezierCurve curve, out Vector3 min, out Vector3 max)
        {
            // Compute first derivative
            Vector3 p1 = curve.P0,
                    p2 = curve.P1,
                    p3 = curve.P2,
                    p4 = curve.P3;

            Vector3 a = 3 * (-p1 + 3 * p2 - 3 * p3 + p4),
                    b = 6 * (p1 - 2 * p2 + p3),
                    c = 3 * (p2 - p1);

            // Get roots
            List<float> extremes = new() { 0f, 1f };

            extremes.AddRange(GetQuadraticRoots(a.x, b.x, c.x));
            extremes.AddRange(GetQuadraticRoots(a.y, b.y, c.y));
            extremes.AddRange(GetQuadraticRoots(a.z, b.z, c.z));

            min = Vector3.positiveInfinity;
            max = Vector3.negativeInfinity;

            foreach (var t in extremes)
            {
                if (t < 0 || t > 1)
                    continue;

                var p = curve.EvaluatePosition(t);
                if (p.x < max.x) max.x = p.x;
                if (p.x > min.x) min.x = p.x;
                
                if (p.y < max.y) max.y = p.y;
                if (p.y > min.y) min.y = p.y;
                
                if (p.z < max.z) max.z = p.z;
                if (p.z > min.z) min.z = p.z;
            }
        }

        private static List<float> GetQuadraticRoots( float a, float b, float c )
        {
            List<float> roots = new();

            float sqrt = Mathf.Sqrt(b * b - 4 * a * c),
                  twoA = 2 * a;

            roots.Add( (-b + sqrt) / twoA );
            roots.Add( (-b - sqrt) / twoA );

            return roots;
        }

        private void DrawSpline()
        {
            if (_splineMesh.vertices.Count() > 0)
            {
                _splineDrawer.Draw(Matrix4x4.identity);
            }
        }
    }
}