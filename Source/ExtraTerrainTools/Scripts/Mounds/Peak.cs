using UnityEngine;
using Timberborn.Common;
using Generator = TerrainTools.NoiseGenerator.NoiseGenerator;
namespace TerrainTools.MoundMaker
{
    public class Peak
    {
        private static readonly float TAU = 2 * Mathf.PI;
        
        public Vector2Int Coordinate { get { return vertex.XY(); } }
        public int Height { get { return vertex.z; } }
        public int Base { get { return Radius * 2; } }
        public int Radius { get; }
        public int RadiusSqr { get; }
        public int Radials { get; }
        public float RadiusMin { get; }
        public float RadiusMax { get; }
        public int RadiusOctaves { get; }
        public float RadiusFreq { get; }

        private Vector3Int vertex;
        private Vector3[] radialEnds;
        private readonly float arcStep;

        public Peak( Vector3Int vertex, int radius, int maxRadials = 64 )
        {
            this.vertex = vertex;
            Radius = radius;
            RadiusSqr = radius * radius;

            Radials = Mathf.Min( 
                Mathf.RoundToInt(TAU * radius), 
                maxRadials 
            );
            arcStep = TAU / Radials;

            ComputeCone();
        }

        public Peak(Vector3Int vertex, int radius, string seed, float radialNoiseScaleMin = 0.75f, float radialNoiseScaleMax = 1, int radialNoiseDetail = 3, float radialNoiseFrequency = 2, int maxRadials = 64 )
        {
            this.vertex = vertex;
            Radius = radius;
            RadiusSqr = radius * radius;
            RadiusMin = radialNoiseScaleMin;
            RadiusMax = radialNoiseScaleMax;
            RadiusFreq = radialNoiseFrequency;
            RadiusOctaves = radialNoiseDetail;

            Radials = Mathf.Min( 
                Mathf.RoundToInt(TAU * radius), 
                maxRadials 
            );
            arcStep = TAU / Radials;

            ComputeCone(seed);
        }

        private void ComputeCone()
        {
            radialEnds = new Vector3[Radials];

            float angle, x, y;
            for (int i = 0; i < Radials; i++)
            {
                angle = arcStep * i;
                x = Mathf.Cos(angle) * Radius;
                y = Mathf.Sin(angle) * Radius;
                radialEnds[i] = new( x, y, -vertex.z );
            }
        }

        private void ComputeCone(string seed)
        {
            Randomizer rng = seed == null ? new() : new(seed);
            radialEnds = new Vector3[Radials];
            
            int radialOffset = rng.GetInt(Radials);
            float freq = RadiusFreq;

            Vector2 coord = new(0, rng.GetFloat());
            Vector2 period = new(freq, freq);
            float   angle, x, y, n, r,
                    rMin = RadiusMin,
                    rDev = 1f - rMin;

            for (int i = 0, j; i < Radials; i++)
            {
                j = (radialOffset + i) % Radials;
                coord.x = freq * j / Radials;
                // n = ( Generator.Noise(coord, period) + 1 ) / 2;
                n = Mathf.Clamp01( (Generator.FBM(coord, period, 0, RadiusOctaves, 0.5f, freq) + 1) / 2 );
                r = rMin + rDev * n;
                angle = arcStep * i;
                x = Mathf.Cos(angle) * r * Radius;
                y = Mathf.Sin(angle) * r * Radius;
                radialEnds[i] = new( x, y, -vertex.z );
            }
        }    

        /// <summary>
        /// Project point onto slope of peak.
        /// </summary>
        /// <param name="point">Point on XY-plane in world space</param>
        /// <returns>Point on surface</returns>
        public Vector3 PointOnSlope( Vector2 point )
        {
            Vector3 local = (point - vertex.XY()).XYZ();

            // Utils.Log("local: {0}", local);

            float angle = (Mathf.Atan2(local.y, local.x) + TAU) % TAU;
            float steps = angle / arcStep;
            
            int index0 = Mathf.FloorToInt(steps);
            float subStep = steps % 1;

            Vector3 r0 = radialEnds[index0];

            // Utils.Log("r0: {0}", r0);
            if( subStep == 0 ) 
            {
                // Utils.Log("Projected: {0}", local.DropOnto(r0));
                // We hit a radial
                return vertex + local.DropOnto(r0);
            }

            // We hit between radials
            Vector3 r1 = radialEnds[(index0 + 1) % Radials];
            Vector3 r2 = Vector3.Slerp(r0, r1, subStep);

            // Utils.Log("r1: {0}", r1);
            // Utils.Log("r2: {0}", r2);
            // Utils.Log("Projected: {0}", local.DropOnto(r2));                    

            return vertex + local.DropOnto(r2);
        }


        // private bool LinesIntersect(Vector2 P1, Vector2 Q1, Vector2 P2, Vector2 Q2, out Vector2 atPoint)
        // {
        //     float d = (P1.x - Q1.x) * (P2.y - Q2.y) - (P1.y - Q1.y) * (P2.x - Q2.x);

        //     if (d == 0)
        //     {
        //         // Lines are parallel
        //         atPoint = Vector2.zero;
        //         return false;
        //     }

        //     // Factors for x and y
        //     float f1 = P1.x * Q1.y - P1.y * Q1.x;
        //     float f2 = P2.x * Q2.y - P2.y * Q2.x;

        //     // Compute intersection point
        //     atPoint = new Vector2(
        //         ( f1 * ( P2.x - Q2.x ) - f2 * ( P1.x - Q1.x ) ) / d,
        //         ( f1 * ( P2.y - Q2.y ) - f2 * ( P1.y - Q1.y ) ) / d
        //     );
        //     return true;
        // }
    }
}