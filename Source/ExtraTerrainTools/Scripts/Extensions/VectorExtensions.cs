using Unity.Mathematics;
using UnityEngine;

namespace TerrainTools
{
    internal static class VectorExtensions
    {
        // Constants
        private const float _IsLeftEpsilon = 0.0002f;
        // Should support vector component ratios of 5000:1 units lines as per https://www.desmos.com/calculator/ypvs2qudwm
        // Yes I actually did the math... It's probably wrong.

        // Extension methods
        public static Vector3 DropOnto(this Vector3 a, Vector3 b)
        {
            if (a == Vector3.zero || b == Vector3.zero)
                return Vector3.zero;

            var p = Vector3.Project(b, a);
            return b * (a.magnitude / p.magnitude);
        }
        public static void RotateCcw90(this Vector2 a)
        {
            float x = a.x;
            a.x = -a.y;
            a.y = x;
        }
        public static Vector2 Ccw90(this Vector2 a)
        {
            return new(-a.y, a.x);
        }
        public static void RotateCw90(this Vector2 a)
        {
            float x = a.x;
            a.x = a.y;
            a.y = -x;
        }
        public static Vector2 Cw90(this Vector2 a)
        {
            return new(a.y, -a.x);
        }
        public static float3 ToFloat3(this Vector3Int v)
        {
            return new float3(v.x, v.y, v.z);
        }
        public static float3 ToFloat3(this Vector3 v)
        {
            return new float3(v.x, v.y, v.z);
        }
        public static Vector3Int ToVector3Int(this float3 f)
        {
            return new Vector3Int(
                Mathf.FloorToInt(f.x),
                Mathf.FloorToInt(f.y),
                Mathf.FloorToInt(f.z)
            );
        }
        public static Vector3 ToVector3(this float3 f)
        {
            return new Vector3(f.x, f.y, f.z);
        }
        public static Vector3 XZY(this Vector3 v)
        {
            return new(v.x, v.z, v.y);
        }
        public static int IsLeftOf(this Vector3 point, Ray ray)
        {
            return point.IsLeftOf(ray, Vector3.up);
        }
        public static int IsLeftOf(this Vector3 point, Ray ray, Vector3 upAxis)
        {
            upAxis.Normalize();

            // Utils.Log("point = {0}", point);
            // Utils.Log("ray = {0}", ray);
            // Utils.Log("upAxis = {0}", upAxis);

            if (point == ray.origin)
                return 0;

            if (ray.direction == Vector3.zero)
                return 0;

            if (upAxis == Vector3.zero)
                return 0;

            Vector3 normal = Vector3.Cross(ray.direction, upAxis).normalized;
            float dot = Vector3.Dot(normal, (point - ray.origin).normalized);

            // Utils.Log("point - ray.origin = {0}", point - ray.origin);
            // Utils.Log("normal = {0}", normal);
            // Utils.Log("dot = {0}", dot);

            return Mathf.Abs(dot) < _IsLeftEpsilon ? 0 : dot > 0 ? -1 : 1;
        }
        public static int IsLeftOf(this Vector3Int point, Ray ray)
        {
            return ((Vector3)point).IsLeftOf(ray);
        }
        public static int IsLeftOf(this Vector3Int point, Ray ray, Vector3 upAxis)
        {
            return ((Vector3)point).IsLeftOf(ray, upAxis);
        }
    }
}