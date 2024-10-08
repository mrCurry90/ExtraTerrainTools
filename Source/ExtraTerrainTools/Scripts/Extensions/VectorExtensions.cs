using Unity.Mathematics;
using UnityEngine;

namespace TerrainTools
{
    internal static class VectorExtensions
    {
        public static Vector3 DropOnto( this Vector3 a, Vector3 b)
        {
            if (a == Vector3.zero || b == Vector3.zero)
                return Vector3.zero;

            var p = Vector3.Project(b,a);
            return b * (a.magnitude / p.magnitude);
        }
        public static void RotateCcw90( this Vector2 a )
        {
            float x = a.x;
            a.x = -a.y;
            a.y = x;
        }

        public static Vector2 Ccw90( this Vector2 a )
        {
            return new(-a.y, a.x);
        }

        public static void RotateCw90( this Vector2 a )
        {
            float x = a.x;
            a.x = a.y;
            a.y = -x;
        }

        public static Vector2 Cw90( this Vector2 a )
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
    }
}