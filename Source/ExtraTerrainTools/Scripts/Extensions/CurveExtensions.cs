using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace TerrainTools
{
    internal struct CurveCoordinateT
    {
        public Vector3 coord;
        public float t;
        public float distance;
        public int curveIndex;
        public Vector3 tangent;

        public CurveCoordinateT(Vector3 coord, float t)
        {
            this.coord = coord;
            this.t = t;
            this.distance = 0;
            this.curveIndex = -1;
            this.tangent = Vector3.zero;
        }
        public CurveCoordinateT(Vector3 coord, float t, float distance)
        {
            this.coord = coord;
            this.t = t;
            this.distance = distance;
            this.curveIndex = -1;
            this.tangent = Vector3.zero;
        }
        public CurveCoordinateT(Vector3 coord, float t, float distance, Vector3 tangent)
        {
            this.coord = coord;
            this.t = t;
            this.distance = distance;
            this.curveIndex = -1;
            this.tangent = tangent;
        }
        public CurveCoordinateT(Vector3 coord, float t, float distance, int curveIndex)
        {
            this.coord = coord;
            this.t = t;
            this.distance = distance;
            this.curveIndex = curveIndex;
            this.tangent = Vector3.zero;
        }

        public override readonly string ToString()
        {
            return string.Format("coord: {0}, t: {1}, dist: {2}, index: {3}, tangent: {4}", coord, t, distance, curveIndex, tangent);
        }
    }

    internal static class CurveExtensions
    {
        public static Vector3 EvaluatePosition(this BezierCurve curve, float t)
        {
            return CurveUtility.EvaluatePosition(curve, t);
        }

        public static Vector3 EvaluateTangent(this BezierCurve curve, float t)
        {
            return CurveUtility.EvaluateTangent(curve, t);
        }
        public static Vector3 EvaluateAcceleration(this BezierCurve curve, float t)
        {
            return CurveUtility.EvaluateAcceleration(curve, t);
        }

        public static float EvaluateCurvature(this BezierCurve curve, float t)
        {
            return CurveUtility.EvaluateCurvature(curve, t);
        }

        public static void GetBounds(this BezierCurve curve, out Vector3 min, out Vector3 max)
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
                if (p.x > max.x) max.x = p.x;
                if (p.x < min.x) min.x = p.x;

                if (p.y > max.y) max.y = p.y;
                if (p.y < min.y) min.y = p.y;

                if (p.z > max.z) max.z = p.z;
                if (p.z < min.z) min.z = p.z;
            }

            Utils.Log("Bounds: {0} - {1}", min, max);
        }

        private static List<float> GetQuadraticRoots(float a, float b, float c)
        {
            List<float> roots = new();

            float sqrt = Mathf.Sqrt(b * b - 4 * a * c),
                  twoA = 2 * a;

            roots.Add((-b + sqrt) / twoA);
            roots.Add((-b - sqrt) / twoA);

            return roots;
        }

        public static void ComputeCurvePositions(this BezierCurve curve, CurveCoordinateT[] lookupTable)
        {
            int length = lookupTable.Length;
            if (length < 2)
                throw new ArgumentOutOfRangeException("lookupTable.Length", "Must be at least length of 2");

            lookupTable[0] = new(EvaluatePosition(curve, 0), 0);
            for (int i = 1; i < length - 1; i++)
            {
                float t = i / length;
                Vector3 p1 = EvaluatePosition(curve, t);
                lookupTable[i] = new(p1, t);
            }
            lookupTable[length - 1] = new(EvaluatePosition(curve, 1), 1);
        }

        public static CurveCoordinateT ClosestPoint(this BezierCurve curve, Vector3 point, int initialSamples = 4)
        {
            if (initialSamples < 2)
                initialSamples = Mathf.Max(Mathf.CeilToInt(CurveUtility.ApproximateLength(curve)), 2);

            var lookupTable = new CurveCoordinateT[initialSamples];
            curve.ComputeCurvePositions(lookupTable);
            return curve.ClosestPoint(point, lookupTable);
        }

        public static CurveCoordinateT ClosestPoint(this BezierCurve curve, Vector3 point, CurveCoordinateT[] lookupTable)
        {
            int i = ClosestPointInLUT(point, lookupTable),
                iLast = lookupTable.Length - 1;

            CurveCoordinateT result = lookupTable[i];
            float dist = float.MaxValue;
            float step = float.MaxValue;
            int c = 0;
            while (step > 0.001f)
            {
                // Utils.Log("c = {0}", c);
                // Utils.Log("dist = {0}", dist);
                int i1 = i == 0 ? 0 : i - 1;
                int i2 = i == iLast ? iLast : i + 1;

                float t1 = lookupTable[i1].t;
                float t2 = lookupTable[i2].t;
                step = (t2 - t1) / 4;

                // Utils.Log("t1 = {0}", t1);
                // Utils.Log("t2 = {0}", t2);
                // Utils.Log("step = {0}", step);

                var lut = new CurveCoordinateT[5];
                lut[0] = lookupTable[i1];
                for (int j = 1; j <= 3; j++)
                {
                    float t = t1 + j * step;
                    CurveCoordinateT n = new(curve.EvaluatePosition(t), t);
                    n.distance = (point - n.coord).sqrMagnitude;
                    if (n.distance < dist)
                    {
                        result = n;
                        dist = n.distance;
                        i = j;
                    }
                    lut[j] = n;
                }
                lut[4] = lookupTable[i2];

                lookupTable = lut;
                c++;
            }
            ;

            result.distance = Mathf.Sqrt(result.distance);

            return result;
        }

        private static int ClosestPointInLUT(Vector3 point, CurveCoordinateT[] lookupTable)
        {
            float min = float.MaxValue;
            int index = 0;
            for (int i = 0; i < lookupTable.Length; i++)
            {
                float d = (point - lookupTable[i].coord).sqrMagnitude;
                if (d < min)
                {
                    min = d;
                    index = i;
                }
            }
            return index;
        }
    }
}