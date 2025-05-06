using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace TerrainTools
{
    internal static class SplineExtensions
    {
        public static BezierCurve[] GetCurves(this Spline spline)
        {
            Utils.Log("spline.Count: {0}", spline.Count);

            int curveCount = spline.Closed ? spline.Count : spline.Count - 1;
            BezierCurve[] curves = new BezierCurve[curveCount > 0 ? curveCount : 0];

            Utils.Log("curveCount: {0}", curveCount);
            Utils.Log("curves.Count(): {0}", curves.Count());
            
            for (int i = 0; i < curveCount; i++)
            {
                Utils.Log("i: {0}", i);
                var curve = spline.GetCurve(i);
                Utils.Log("curve: {0}", curve);
                Utils.Log("curve p0: {0}", curve.P0);
                Utils.Log("curve p1: {0}", curve.P1);
                Utils.Log("curve p2: {0}", curve.P2);
                Utils.Log("curve p3: {0}", curve.P3);
                curves[i] = curve;
            }

            return curves;
        }

        public static void GetBounds<T>(this T spline, out Vector3Int min, out Vector3Int max) where T : ISpline
        {
            Bounds bounds = spline.GetBounds();
            min = new(
                Mathf.FloorToInt(bounds.min.x),
                Mathf.FloorToInt(bounds.min.y),
                Mathf.FloorToInt(bounds.min.z)
            );
            max = new(
                Mathf.CeilToInt(bounds.max.x),
                Mathf.CeilToInt(bounds.max.y),
                Mathf.CeilToInt(bounds.max.z)
            );
        }
    }
}