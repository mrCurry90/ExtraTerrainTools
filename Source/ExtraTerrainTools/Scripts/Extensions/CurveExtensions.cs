using UnityEngine;
using UnityEngine.Splines;

namespace TerrainTools
{
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
    }
}