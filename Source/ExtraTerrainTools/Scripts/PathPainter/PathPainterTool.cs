using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using TerrainTools.EditorHistory;
using Timberborn.BlockSystem;
using Timberborn.CameraSystem;
using Timberborn.Common;
using Timberborn.GridTraversing;
using Timberborn.InputSystem;
using Timberborn.Rendering;
using Timberborn.SingletonSystem;
using Timberborn.TerrainQueryingSystem;
using Timberborn.TerrainSystem;
using Timberborn.ToolSystem;
using Unity.Mathematics;
using UnityEngine.Splines;
using UnityEngine;
using Timberborn.Localization;

namespace TerrainTools.PathPainter
{
    public class PathPainterTool : ITerrainTool, IInputProcessor, ILoadableSingleton
    {
        public override string Icon { get; } = "LineToolIcon";
        private readonly string _keyToolTitle = "TerrainTools.PathPainter.Tool.Title"; // Path Painter
        private readonly string _keyToolDescription = "TerrainTools.PathPainter.Tool.Description"; // Adjust terrain using lines

        private static readonly string _addMoveKeybind = "MouseLeft";
        private static readonly string _deleteKeybind = "DeleteObject";
        public static string AddMoveKeybind => _addMoveKeybind;
        public static string DeleteKeybind => _deleteKeybind;

        private static readonly float MarkerYOffset = 0.02f;
        
        private readonly InputService _inputService;
        private readonly ITerrainService _terrainService;
        private readonly IBlockService _blockService;
        private readonly TerrainPicker _terrainPicker;
        private readonly CameraService _cameraService;
        private readonly MarkerDrawerFactory _markerDrawerFactory;
        private readonly EditorHistoryService _historyService;
        private readonly SplineDrawer _splineDrawer;

        private Color _nearTileColor = new(0f, 1f, 1f, 1f);
        private Color _farTileColor = new(1f, 1f, 0f, 1f);

        public Color NearTileColor { get => _nearTileColor; }
        public Color FarTileColor { get => _farTileColor; }

        private MeshDrawer _markerDrawer;
        private ToolDescription _toolDescription;

        // private int _width;
        private int _radius;
        // private float _strokeDistance = 2;
        private float _strokeOffset = 0.5f;

        public int Width
        {
            get { return _radius * 2 + 1; }
        }

        public int Radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                Rebuild();
            }
        }

        public int LeftNearHeight = 0;
        public int LeftFarHeight = 16;
        public int RightNearHeight = 0;
        public int RightFarHeight = 16;

        private float StrokeDistance
        {
            get { return _radius + _strokeOffset; }
        }

        public SplineDrawerMode Mode
        {
            get { return _splineDrawer.Mode; }
            set
            {
                _splineDrawer.Mode = value;
                Rebuild();
            }
        }
        public bool SimpleCurves
        {
            get { return _splineDrawer.SingleCurveOnly; }
            set
            {
                _splineDrawer.SingleCurveOnly = value;
                Rebuild();
            }
        }

        private bool _drawEndCaps = true;
        public bool DrawEndCaps
        {
            get { return _drawEndCaps; }
            set
            {
                _drawEndCaps = value;
                Rebuild();
            }
        }

        private bool _curveDebug = false;
        public bool CurveDebug
        {
            get { return _curveDebug; }
            set
            {
                _curveDebug = value;
                Rebuild();
            }
        }

        private Slope _leftSlope = new();
        public Slope LeftSlope
        {
            get { return _leftSlope; }
            set { _leftSlope = value; }
        }

        public bool InvertLeftSlope = false;

        private Slope _rightSlope = new();
        public Slope RightSlope
        {
            get { return _rightSlope; }
            set { _rightSlope = value; }
        }

        public bool InvertRightSlope = false;

        private int _maxNodes = 100;
        private Vector3 _gridCenterOffset = new(0.5f, 0.5f, 0.5f);
        ConcurrentDictionary<Vector2Int, CurveCoordinateT> _distanceField = new();

        private float _removeHeldTimer = 0;
        private float _removeHeldDelay = 0.333f; // seconds
        private float _removeRateTimer = 0;
        private float _removeRate = 0.1f; // seconds

        private LinePoint _focusedPoint;
        private bool _movingPoint;

        private Color[] _debugColors;

        public PathPainterTool(
            InputService inputService, ITerrainService terrainService, IBlockService blockService, TerrainPicker terrainPicker, CameraService cameraService,
            MarkerDrawerFactory markerDrawerFactory, EditorHistoryService historyService, SplineDrawer splineDrawer, ILoc loc
        ) : base(loc)
        {
            _inputService = inputService;
            _terrainService = terrainService;
            _blockService = blockService;
            _terrainPicker = terrainPicker;
            _cameraService = cameraService;
            _markerDrawerFactory = markerDrawerFactory;
            _historyService = historyService;
            _splineDrawer = splineDrawer;
        }

        public void Load()
        {
            var builder = new ToolDescription.Builder(_loc.T(_keyToolTitle));
            builder.AddPrioritizedSection(_loc.T(_keyToolDescription));
            _toolDescription = builder.Build();

            _markerDrawer = _markerDrawerFactory.CreateTileDrawer();

            Color[] colors = {
                Color.red,
                Color.yellow,
                Color.green,
                Color.cyan,
                Color.blue,
                Color.magenta
            };

            _debugColors = new Color[2 * colors.Length];
            int j, k, l;
            for (int i = 0; i < colors.Length; i++)
            {
                j = (i + 1) % colors.Length;
                k = i * 2;
                l = k + 1;
                _debugColors[k] = colors[i];
                _debugColors[l] = Color.Lerp(colors[i], colors[j], 0.5f);
            }
        }

        // private void MakeRandom()
        // {  
        //     Vector3Int size = _terrainService.Size;
        //     int n = 6;

        //     List<Vector3> points = new();
        //     for (int i = 0; i < n; i++)
        //     {
        //         Vector2Int p1 = new(
        //             Random.Range(0, size.x),
        //             Random.Range(0, size.y)
        //         );

        //         Vector3 p2 = new(p1.x, _terrainService.CellHeight(p1), p1.y);
        //         p2 += _gridCenterOffset;

        //         points.Add(p2);

        //     }

        //     _splineDrawer.SetPoints(points);
        // }

        private void ToggleHelpers(bool active)
        {
            _splineDrawer.Visible = active;
            if (active)
            {
                _splineDrawer.SplineRebuilt += OnSplineRebuilt;
            }
            else
            {
                _splineDrawer.SplineRebuilt -= OnSplineRebuilt;
            }
        }

        // private List<GameObject> splineKnotMarkers = new();
        // private Material LitMaterial;
        // private void DrawSplineKnots(Spline spline)
        // {
        //     if (LitMaterial == null)
        //     {
        //         LitMaterial = new Material(
        //             Shader.Find("Universal Render Pipeline/Lit")
        //         );
        //         LitMaterial.color = Color.green;
        //     }

        //     var scale = new Vector3(0.25f, 0.25f, 0.25f);

        //     foreach (var go in splineKnotMarkers)
        //     {
        //         GameObject.Destroy(go);
        //     }

        //     splineKnotMarkers.Clear();
        //     foreach (var knot in spline)
        //     {
        //         var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //         cube.GetComponent<Collider>().enabled = false;
        //         cube.transform.localScale = scale;
        //         cube.transform.position = knot.Position;
        //         cube.transform.LookAt(knot.Position + knot.TangentOut);
        //         cube.GetComponent<MeshRenderer>().material = LitMaterial;
        //         splineKnotMarkers.Add(cube);
        //     }
        // }

        private void Rebuild()
        {
            OnSplineRebuilt(_splineDrawer.Spline);
        }

        private void OnSplineRebuilt(Spline spline)
        {
            // if(_curveDebug)
            //     DrawSplineKnots(spline);

            spline.GetBounds(out Vector3Int min, out Vector3Int max);
            Vector3Int size = _terrainService.Size;
            min.x = Mathf.Max(0, min.x - _radius - 1);
            min.y = Mathf.Max(0, min.y);
            min.z = Mathf.Max(0, min.z - _radius - 1);
            max.x = Mathf.Min(size.x, max.x + _radius + 1);
            max.y = Mathf.Min(size.z, max.y);
            max.z = Mathf.Min(size.y, max.z + _radius + 1);

            int area = (max.x - min.x) * (max.z - min.z);
            Vector3 gridOffset = new(0.5f, 0.5f, 0.5f);

            // Utils.Log("min: {0}", min);
            // Utils.Log("max: {0}", max);            

            ConcurrentDictionary<Vector2Int, CurveCoordinateT> distanceField = new(Environment.ProcessorCount, area);
            CurveCoordinateT fieldDefault = new() { distance = StrokeDistance };

            // Compute and store curve lengths on the main thread
            for (int i = 0; i < spline.GetCurveCount(); i++)
            {
                spline.GetCurveLength(i);
            }

            // Split the work
            int w = max.x - min.x;
            Parallel.For(0, area, delegate (int i)
            {
                int x = min.x + (i % w);
                int y = min.z + (i / w);  // y = z because we are converting from world space to grid space

                Vector2Int coord = new(x, y);
                Vector3 point = new(x, _terrainService.CellHeight(coord), y);
                point += gridOffset;
                SplineUtility.GetNearestPoint(spline, point, out float3 nearest, out float t);
                if (!DrawEndCaps && (t < 0 || t > 1))
                    return;

                // We're only interested in horizontal distance
                Vector2 p1 = point.XZ();
                Vector2 p2 = new(nearest.x, nearest.z);
                float distance = (p2 - p1).magnitude;

                if (distance < distanceField.GetValueOrDefault(coord, fieldDefault).distance)
                {
                    // Compute tangent
                    Vector3 tangent;
                    float dt, step = 0.1f;
                    if (t <= 0)
                    {
                        // Step forward from t = 0
                        tangent = spline.GetPointAtLinearDistance(0, step, out dt) - spline.EvaluatePosition(0);
                    }
                    else if (t > 1)
                    {
                        // Step backward from t = 1
                        tangent = spline.EvaluatePosition(1) - spline.GetPointAtLinearDistance(1, -0.1f, out dt);
                    }
                    else
                    {
                        // 0 < t < 1, EvaluateTangent will work
                        tangent = spline.EvaluateTangent(t);
                    }
                    CurveCoordinateT projection = new(nearest, t, distance, tangent.normalized);
                    distanceField.AddOrUpdate(coord, projection, (_, v) => projection.distance < v.distance ? projection : v);
                }
            });

            // for (float y = min.z; y < max.z; y++) // y = z because we are converting from world space to grid space
            // {
            //     for (float x = min.x; x < max.x; x++)
            //     {
            //         Vector2Int coord = new((int)x, (int)y);
            //         Vector3 point = new(x, _terrainService.CellHeight(coord), y);
            //         SplineUtility.GetNearestPoint(spline, point, out float3 nearest, out float t);

            //         // We're only interested in horizontal distance
            //         Vector2 p1 = point.XZ();

            //         Vector2 p2 = new(nearest.x, nearest.z);
            //         float distance = (p2 - p1).magnitude;

            //         if( distance < distanceField.GetValueOrDefault(coord, fieldDefault).distance )
            //         {
            //             CurveCoordinateT projection = new(nearest, t, distance);
            //             distanceField[coord] = projection;
            //         }
            //     }
            // }

            _distanceField = distanceField;
        }

        public void ResetSpline()
        {
            _splineDrawer.Clear();
        }

        public override ToolDescription Description()
        {
            return _toolDescription;
        }

        public override void Enter()
        {
            _inputService.AddInputProcessor(this);
            //Spline.Changed += OnSplineChanged; 
            ToggleHelpers(true);
        }

        public override void Exit()
        {
            _inputService.RemoveInputProcessor(this);
            //Spline.Changed -= OnSplineChanged;   
            ToggleHelpers(false);
        }

        public bool ProcessInput()
        {
            Ray ray;
            int numKnots = _splineDrawer.PointCount;
            float deltaTime = Time.unscaledDeltaTime;

            if (_inputService.Cancel)
            {
                return false;
                // returning true apparently works to block output now
                // TODO rethink inputs

                // _inputService.MouseCancel
                // _inputService.UICancel
            }
            if (_inputService.IsKeyDown(DeleteKeybind) && numKnots > 0 && !_movingPoint)
            {
                _removeHeldTimer = 0;
                _removeRateTimer = 0;
                // _splineDrawer.RemoveLastPoint();
                if (_focusedPoint != null)
                {
                    _splineDrawer.RemovePoint(_focusedPoint);
                    _focusedPoint = null;
                }
            }
            else if (_inputService.IsKeyHeld(DeleteKeybind) && numKnots > 0 && !_movingPoint)
            {
                _removeHeldTimer += deltaTime;
                if (_removeHeldTimer > _removeHeldDelay)
                {
                    _removeRateTimer += deltaTime;
                    if (_removeRateTimer >= _removeRate)
                    {
                        _removeRateTimer = 0;
                        _splineDrawer.RemoveLastPoint();
                    }
                }
            }
            else if (!_inputService.MouseOverUI && !_movingPoint)
            {
                ray = _cameraService.ScreenPointToRayInWorldSpace(_inputService.MousePosition);
                bool hit = false;
                // if( Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, Physics.AllLayers, QueryTriggerInteraction.Collide) )
                if (Physics.Raycast(ray, out RaycastHit hitInfo))
                {
                    if (hitInfo.collider != null)
                    {
                        LinePoint linePoint = hitInfo.collider.GetComponentInParent<LinePoint>();
                        hit = linePoint != null;
                        if (hit && _focusedPoint != linePoint)
                        {
                            if (_focusedPoint != null)
                                _focusedPoint.SetState(LinePoint.State.Idle);

                            _focusedPoint = linePoint;
                            _focusedPoint.SetState(LinePoint.State.Hovered);
                        }
                    }
                }

                if (!hit && _focusedPoint != null)
                {
                    _focusedPoint.SetState(LinePoint.State.Idle);
                    _focusedPoint = null;
                }

                if (_inputService.MainMouseButtonDown && _focusedPoint == null && numKnots <= _maxNodes)
                {
                    ray = _cameraService.ScreenPointToRayInGridSpace(_inputService.MousePosition);
                    if (HasRayHitTerrain(ray, out Vector3Int mouseCoord))
                    {
                        _splineDrawer.AddPoint(GridCenterToWorld(mouseCoord));
                    }
                }
                else if (_focusedPoint != null && _inputService.MainMouseButtonDown)
                {
                    _focusedPoint.SetState(LinePoint.State.Highlighted);
                    _movingPoint = true;
                }
            }
            else if (!_inputService.MouseOverUI)
            {
                if (_inputService.MainMouseButtonHeld)
                {
                    ray = _cameraService.ScreenPointToRayInGridSpace(_inputService.MousePosition);
                    if (HasRayHitTerrain(ray, out Vector3Int mouseCoord))
                    {
                        _splineDrawer.MovePoint(_focusedPoint, GridCenterToWorld(mouseCoord));
                    }
                }
                else
                {
                    _movingPoint = false;
                    if (_focusedPoint != null)
                        _focusedPoint.SetState(LinePoint.State.Hovered);
                }
            }

            DrawPreviewTiles(_nearTileColor, _farTileColor);

            return false;
        }

        private Vector3 GridCenterToWorld(Vector3Int coordinate)
        {
            Vector3 p = coordinate + _gridCenterOffset;
            return p.XZY();
        }

        public void Apply()
        {
            _historyService.BatchStart();

            Easer leftEaser = LeftNearHeight < LeftFarHeight ? _leftSlope.ToEaser() : _leftSlope.Inverse().ToEaser();
            Easer rightEaser = RightNearHeight < RightFarHeight ? _rightSlope.ToEaser() : _rightSlope.Inverse().ToEaser();

            Easer easer;
            bool invert;
            int nearHeight;
            int farHeight;

            foreach (var fieldPair in _distanceField)
            {
                var coordinates = fieldPair.Key;
                var curveData = fieldPair.Value;

                Vector3 worldCoord = GridCenterToWorld(coordinates.XYZ());
                Ray ray = new(
                    new(curveData.coord.x, worldCoord.y, curveData.coord.z),
                    curveData.tangent
                );

                float t = Mathf.Clamp01(curveData.distance / StrokeDistance);


                if (worldCoord.IsLeftOf(ray) < 0)
                {
                    easer = leftEaser;
                    invert = InvertLeftSlope;
                    nearHeight = LeftNearHeight;
                    farHeight = LeftFarHeight;
                }
                else
                {
                    easer = rightEaser;
                    invert = InvertRightSlope;
                    nearHeight = RightNearHeight;
                    farHeight = RightFarHeight;
                }

                int adjustBy = Mathf.RoundToInt(
                    Mathf.LerpUnclamped(nearHeight, farHeight, easer.Value(t))
                );

                _terrainService.AdjustTerrain(new(coordinates.x, coordinates.y, _terrainService.CellHeight(coordinates)), adjustBy);
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
            TraversedCoordinates? coordinates = _terrainPicker.FindCoordinatesOnLevelInMap(ray, referenceHeight);
            if (coordinates.HasValue)
            {
                where = coordinates.GetValueOrDefault().Coordinates;
                return true;
            }

            where = Vector3Int.zero;
            return false;
        }

        private void DrawPreviewTiles(Color nearColor, Color farColor)
        {
            if (_distanceField.Count > 0)
            {
                Color col;
                Vector3Int coord = new();
                if (!_curveDebug)
                {
                    foreach (var pair in _distanceField)
                    {
                        coord.x = pair.Key.x;
                        coord.y = pair.Key.y;
                        coord.z = _terrainService.CellHeight(pair.Key);
                        col = Color.Lerp(
                            nearColor, farColor,
                            Mathf.Abs(pair.Value.distance / StrokeDistance)
                        );
                        _markerDrawer.DrawAtCoordinates(coord, MarkerYOffset, col);
                    }
                }
                else
                {
                    foreach (var pair in _distanceField)
                    {
                        coord.x = pair.Key.x;
                        coord.y = pair.Key.y;
                        coord.z = _terrainService.CellHeight(pair.Key);

                        int colIndex = Mathf.FloorToInt(Mathf.Lerp(0, _debugColors.Length, pair.Value.t));
                        col = _debugColors[colIndex % _debugColors.Length];
                        _markerDrawer.DrawAtCoordinates(coord, MarkerYOffset, col);
                    }
                }
            }


            // Vector3Int coordinates;
            // float distance;

            // // Draw knots
            // List<Vector3Int> knotCoordinates = new(_maxNodes);
            // foreach (var knot in _spline)
            // {
            //     coordinates = GetTerrainCoordinateAt(knot.Position.ToVector3Int());
            //     coordinates.z = _terrainService.CellHeight(coordinates.XY());
            //     knotCoordinates.Add(coordinates);
            //     _markerDrawer.DrawAtCoordinates(coordinates, MarkerYOffset, knotColor);
            // }

            // // Draw spline
            // Dictionary<Vector2Int,float> coordDistToSpline = new();
            // for (int i = 0; i < _spline.Count; i++)
            // {

            // }

            // // Draw fill
            // foreach (var selected in GetSelectedCoordinates())
            // {
            //     coordinates = GetTerrainCoordinateAt(selected.Key);
            //     if( knotCoordinates.Contains(coordinates) )
            //         continue;

            //     distance = selected.Value;
            //     _markerDrawer.DrawAtCoordinates(coordinates, MarkerYOffset, fillColor);
            // }
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

    }
}