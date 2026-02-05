using System.Collections.Generic;
using System.Linq;
using Timberborn.Common;
using Unity.Mathematics;
using UnityEngine.Splines;
using UnityEngine;
using System.IO;
using Timberborn.SingletonSystem;
using System;
using Object = UnityEngine.Object;

namespace TerrainTools.PathPainter
{
    public enum SplineDrawerMode
    {
        Linear,
        Quadratic,
        Cubic,
        Continuous
    }

    public class SplineDrawer : ILoadableSingleton, IPostLoadableSingleton
    {
        private const float TWO_THIRDS = 2f / 3f;
        private static readonly string prefabSubpath = "Line";
        private static readonly string ctrlPointPrefabName = "Point";
        private static readonly string ctrlLinePrefabName = "Dashed";
        private static readonly string splineLinePrefabName = "Solid";
        private static LinePoint controlPointPrefab = null;
        private static LineRenderer controlLinePrefab = null;
        private static LineRenderer splineLinePrefab = null;
        private LineRenderer _splineLine;
        private List<LinePoint> _controlPoints = new();
        private List<LineRenderer> _controlLines = new();
        private TerrainToolsAssetService _assetService;

        private Spline _spline = new();
        public Spline Spline { get { return _spline; } }
        private SplineDrawerMode _mode;
        public SplineDrawerMode Mode
        {
            get { return _mode; }
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    ApplyDrawMode();
                }
            }
        }
        private int _lastControlLineIndex = -1;
        private float _tension = 0.5f;

        private int _maxControlPoints = int.MaxValue;

        private bool _visible = false;
        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; UpdateVisibility(); }
        }
        public int PointCount
        {
            get { return _controlPoints.Count; }
        }

        private bool _singleCurveOnly = true;
        public bool SingleCurveOnly
        {
            get { return _singleCurveOnly; }
            set
            {
                _singleCurveOnly = value;
                ApplyDrawMode();
            }
        }

        public event Action<Spline> SplineRebuilt;
        public ReadOnlyList<LinePoint> ControlPoints { get { return _controlPoints.AsReadOnlyList(); } }

        public SplineDrawer(TerrainToolsAssetService assetService)
        {
            _assetService = assetService;
        }

        public void Load()
        {
            TerrainToolsAssetService.Folder prefabsFolder = TerrainToolsAssetService.Folder.Prefabs;
            string path;

            if (controlPointPrefab == null)
            {
                path = Path.Combine(prefabSubpath, ctrlPointPrefabName);
                controlPointPrefab = _assetService.Fetch<LinePoint>(path, prefabsFolder);
            }
            if (controlLinePrefab == null)
            {
                path = Path.Combine(prefabSubpath, ctrlLinePrefabName);
                controlLinePrefab = _assetService.Fetch<LineRenderer>(path, prefabsFolder);
            }
            if (splineLinePrefab == null)
            {
                path = Path.Combine(prefabSubpath, splineLinePrefabName);
                splineLinePrefab = _assetService.Fetch<LineRenderer>(path, prefabsFolder);
            }
        }

        public void PostLoad()
        {
            _splineLine = Instantiate(splineLinePrefab).GetComponent<LineRenderer>();
            _splineLine.enabled = false;

            ApplyDrawMode();
        }

        private void AddPoint(Vector3 point, bool rebuild)
        {
            if (_controlPoints.Count >= _maxControlPoints)
                return;

            LinePoint linePoint = Instantiate(controlPointPrefab).GetComponent<LinePoint>();
            linePoint.Position = point;
            linePoint.SetState(LinePoint.State.Idle);

            _controlPoints.Add(linePoint);

            if (rebuild) Rebuild();
        }

        public void AddPoint(Vector3 point)
        {
            AddPoint(point, true);
        }

        public void SetPoints(IEnumerable<Vector3> points)
        {
            Clear(false);

            foreach (Vector3 point in points)
            {
                AddPoint(point, false);
            }

            Rebuild();
        }

        private void Clear(bool rebuild)
        {
            foreach (LinePoint point in _controlPoints)
            {
                DestroyGameObject(point);
            }
            _controlPoints.Clear();

            if (rebuild)
                Rebuild();
        }

        public void Clear()
        {
            Clear(true);
        }

        private void RemoveLastPoint(bool rebuild)
        {
            DestroyGameObject(_controlPoints.Last());
            _controlPoints.RemoveLast();

            if (rebuild)
                Rebuild();
        }

        public void RemoveLastPoint()
        {
            RemoveLastPoint(true);
        }

        public bool RemovePoint(LinePoint linePoint)
        {
            int i = _controlPoints.FindIndex(p => p == linePoint);
            if (i >= 0)
            {
                DestroyGameObject(_controlPoints[i]);
                _controlPoints.RemoveAt(i);

                Rebuild();
                return true;
            }

            return false;
        }

        public void MovePoint(LinePoint linePoint, Vector3 newPosition)
        {
            linePoint.Position = newPosition;
            Rebuild();
        }

        private void Rebuild()
        {
            // Clear up existing control lines
            foreach (var line in _controlLines)
            {
                line.enabled = false;
            }

            // Compute spline
            if (_controlPoints.Count <= 1) // Single point = No line
            {
                // Utils.Log("SplineDrawer - Rebuilding with single point");
                _spline.Clear();
                _splineLine.enabled = false;
            }
            else if (_controlPoints.Count == 2) // Two points = Straight line
            {
                // Utils.Log("SplineDrawer - Rebuilding with two points");
                _lastControlLineIndex = 0;
                RenderLine(_splineLine, _controlPoints[0].Position, _controlPoints[1].Position);
                DrawSplineAsLine(_controlPoints[0].Position, _controlPoints[1].Position);

                _splineLine.enabled = true;
            }
            else // Three or more points = Curvy!
            {
                // Utils.Log("SplineDrawer - Rebuilding with {0} points", _controlPoints.Count);
                // Utils.Log("_spline.Count: {0}", _spline.Count);
                RedrawSplineLine();
                // Utils.Log("_spline.Count: {0}", _spline.Count);
                _splineLine.enabled = true;

                // Utils.Log("Redrawing control lines");
                RedrawControlLines();
                _lastControlLineIndex = _controlPoints.Count - 2;
                // Utils.Log("_spline.Count: {0}", _spline.Count);
            }

            AlignControlPoints();

            // Destroy unused control lines
            // Utils.Log("Destroying unused control lines");
            for (int i = _controlLines.Count - 1; i > _lastControlLineIndex; i--)
            {
                DestroyGameObject(_controlLines[i]);
                _controlLines.RemoveAt(i);
            }
            // Utils.Log("_spline.Count: {0}", _spline.Count);

            SplineRebuilt?.Invoke(_spline);
        }

        private void RedrawControlLines()
        {
            int count = _controlPoints.Count;
            int controlLineCount = _controlLines.Count;
            for (int cur = 0, next = 1; next < count; cur++, next++)
            {
                LineRenderer line;
                if (cur < controlLineCount)
                {
                    line = _controlLines[cur];
                    line.enabled = true;
                }
                else
                {
                    line = Instantiate(controlLinePrefab).GetComponent<LineRenderer>();
                    _controlLines.Add(line);
                }
                RenderLine(line, _controlPoints[cur], _controlPoints[next]);
            }
        }

        private void ConstructLinear()
        {
            _spline.Clear();

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                _spline.Add(new BezierKnot((float3)_controlPoints[i].Position), TangentMode.Linear);
            }
        }
        private void ConstructQuadatric()
        {
            if (_controlPoints.Count < 3)
            {
                ConstructLinear();
                return;
            }
            ;

            _spline.Clear();

            float3 pKnot, pPrev, pNext;
            int knotIndex = 0, prev, next;
            for (int i = 0; i < _controlPoints.Count; i += 2)
            {
                prev = PreviousIndex(i);
                next = NextIndex(i);

                pPrev = _controlPoints[prev].Position;
                pKnot = _controlPoints[i].Position;
                pNext = _controlPoints[next].Position;

                _spline.Add(new BezierKnot(pKnot, (pPrev - pKnot) * TWO_THIRDS, (pNext - pKnot) * TWO_THIRDS), TangentMode.Broken);
                knotIndex = i;
            }

            // Check for remaining control points to handle
            int pointsRemaining = _controlPoints.Count - (knotIndex + 1);
            switch (pointsRemaining)
            {
                case 0: break; // We're done            
                case 1:
                    _spline.Add(new BezierKnot(_controlPoints[knotIndex + 1].Position), TangentMode.Linear);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("pointsRemaining", "More than 1 point remaining should not be possible!");
            }
        }

        private void ConstructCubic()
        {
            if (_controlPoints.Count < 4)
            {
                ConstructQuadatric();
                return;
            }
            ;

            _spline.Clear();

            float3 pKnot, pPrev, pNext;
            int knotIndex = 0, prev, next;
            for (int i = 0; i < _controlPoints.Count; i += 3)
            {
                prev = PreviousIndex(i);
                next = NextIndex(i);

                pPrev = _controlPoints[prev].Position;
                pKnot = _controlPoints[i].Position;
                pNext = _controlPoints[next].Position;

                _spline.Add(new BezierKnot(pKnot, pPrev - pKnot, pNext - pKnot), TangentMode.Broken);
                knotIndex = i;
            }

            // Check for remaining control points to handle
            int pointsRemaining = _controlPoints.Count - (knotIndex + 1);
            switch (pointsRemaining)
            {
                case 0: break; // We're done
                case 1:
                    _spline.Add(new BezierKnot(_controlPoints[knotIndex + 1].Position), TangentMode.Linear);
                    break;
                case 2:
                    knotIndex += 2;
                    prev = PreviousIndex(knotIndex);
                    next = NextIndex(knotIndex);

                    pPrev = _controlPoints[prev].Position;
                    pKnot = _controlPoints[knotIndex].Position;
                    pNext = _controlPoints[next].Position;

                    _spline.Add(new BezierKnot(pKnot, (pPrev - pKnot) * TWO_THIRDS, (pNext - pKnot) * TWO_THIRDS), TangentMode.Broken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("pointsRemaining", "More than 2 points remaining should not be possible!");
            }
        }
        private void ConstructContinuous()
        {
            _spline.Clear();

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                float3 position = _controlPoints[i].Position;
                int next = NextIndex(i);
                int prev = PreviousIndex(i);
                float3 tangentOut = SplineUtility.GetAutoSmoothTangent(_controlPoints[prev].Position, _controlPoints[i].Position, _controlPoints[next].Position, _tension);
                float3 tangentIn = -tangentOut;
                _spline.Add(new BezierKnot(position, tangentIn, tangentOut), TangentMode.AutoSmooth);
            }
        }

        private void RedrawSplineLine()
        {
            // Utils.Log("RedrawSplineLine()");
            // Construct spline
            switch (_mode)
            {
                case SplineDrawerMode.Linear: ConstructLinear(); break;
                case SplineDrawerMode.Quadratic: ConstructQuadatric(); break;
                case SplineDrawerMode.Cubic: ConstructCubic(); break;
                case SplineDrawerMode.Continuous: ConstructContinuous(); break;
            }

            // Evalute
            float length = _spline.CalculateLength(Matrix4x4.identity);
            int nPoints = Mathf.CeilToInt(length);
            List<Vector3> linePositions = new(nPoints);
            for (float i = 0; i < nPoints; i++)
            {
                float t = i / nPoints;
                linePositions.Add(_spline.EvaluatePosition(t));
            }

            // Tidy up end point
            Vector3 lastPoint = _spline.EvaluatePosition(1);
            Vector3 lastStep = linePositions.Last() - lastPoint;
            if (lastStep.magnitude > 0.5f)
                linePositions.Add(lastPoint);
            else
                linePositions[^1] = lastPoint;

            _splineLine.positionCount = linePositions.Count;
            _splineLine.SetPositions(linePositions.ToArray());

            // Utils.Log("_spline.Count: {0}", _spline.Count);
        }

        private void RenderLine(LineRenderer line, Vector3 start, Vector3 end)
        {
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }
        private void DrawSplineAsLine(Vector3 start, Vector3 end)
        {
            _spline.Clear();
            _spline.Add(new BezierKnot(start), TangentMode.Linear);
            _spline.Add(new BezierKnot(end), TangentMode.Linear);
        }

        private int PreviousIndex(int index)
        {
            return math.max(index - 1, 0);
        }

        private int NextIndex(int index)
        {
            return math.min(index + 1, _controlPoints.Count - 1);
        }

        private void UpdateVisibility()
        {
            foreach (var point in _controlPoints)
            {
                point.gameObject.SetActive(Visible);
            }

            foreach (var renderer in _controlLines)
            {
                renderer.gameObject.SetActive(Visible);
            }

            _splineLine.gameObject.SetActive(Visible);
        }

        private void ApplyDrawMode()
        {
            if (SingleCurveOnly)
            {
                switch (_mode)
                {
                    case SplineDrawerMode.Linear:
                        _maxControlPoints = 2;
                        break;
                    case SplineDrawerMode.Quadratic:
                        _maxControlPoints = 3;
                        // _maxControlPoints = 3;
                        break;
                    case SplineDrawerMode.Cubic:
                        _maxControlPoints = 4;
                        // _maxControlPoints = 4;
                        break;
                    case SplineDrawerMode.Continuous:
                        _maxControlPoints = int.MaxValue;
                        break;
                }

                while (_controlPoints.Count > _maxControlPoints)
                {
                    RemoveLastPoint(rebuild: false);
                }
            }
            else
            {
                _maxControlPoints = int.MaxValue;
            }
            Rebuild();
        }

        private void AlignControlPoints()
        {
            switch (_controlPoints.Count)
            {
                case 0:
                    return;
                case 1:
                    _controlPoints[0].transform.rotation = Quaternion.identity;
                    return;
                default:
                    LinePoint previous = _controlPoints.First();
                    for (int i = 0; i < _controlPoints.Count - 1; i++)
                    {
                        previous = _controlPoints[i];
                        previous.transform.LookAt(_controlPoints[i + 1].transform);
                    }

                    LinePoint last = _controlPoints.Last();
                    Vector3 point = last.transform.position + (last.transform.position - previous.transform.position);
                    last.transform.LookAt(point);
                    return;
            }
        }

        // Gameobject wrappers for cleaner code
        public static T Instantiate<T>(T original) where T : Object
        {
            return Object.Instantiate<T>(original);
        }

        public static void DestroyGameObject(Component obj)
        {
            Object.Destroy(obj.gameObject);
        }
    }
}