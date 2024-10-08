using UnityEngine;
using Timberborn.Common;
using System.Collections.Generic;
using Generator = TerrainTools.NoiseGenerator.NoiseGenerator;
using Unity.Mathematics;

namespace TerrainTools.MoundMaker
{
    public class Mound
    {
        private readonly int _height;
        private readonly Vector2Int _center;
        private readonly int _radius;
        private readonly string _seed;
        private readonly float _radialNoiseScaleMax;
        private readonly float _radialNoiseScaleMin;
        private readonly float _radialNoiseFrequency;
        private readonly int _radialNoiseOctaves;

        private readonly int _vertNoiseOctaves;
        private readonly float _vertNoiseAmp;
        private readonly float _vertNoiseFreq;


        private Mound() {}

        public Mound(Vector2Int center, int radius, int height, string seed, float radialNoiseScaleMin, float radialNoiseScaleMax, float radialNoiseFrequency, int radialNoiseOctaves, int vertNoiseOctaves, float vertNoiseAmp, float vertNoiseFreq)
        {
            _height = height;
            _center = center;
            _radius = radius;
            _seed = seed;
            _radialNoiseScaleMin = radialNoiseScaleMin;
            _radialNoiseScaleMax = radialNoiseScaleMax;
            _radialNoiseFrequency = radialNoiseFrequency;
            _radialNoiseOctaves = radialNoiseOctaves;
            _vertNoiseOctaves = vertNoiseOctaves;
            _vertNoiseAmp = vertNoiseAmp;
            _vertNoiseFreq = vertNoiseFreq;
        }

        public List<Vector3Int> Make()
        {    

            Vector3Int peakPoint = new(_center.x, _center.y, _height);

            Peak peak;
            Vector2 randOffset;
            if( _seed == null )
            {
                peak = new(peakPoint, _radius);
                randOffset = Vector2.zero;
            }
            else 
            {
                peak = new(peakPoint, _radius, _seed, _radialNoiseScaleMin, _radialNoiseScaleMax, _radialNoiseOctaves, _radialNoiseFrequency );
                Randomizer rng = new(_seed);
                randOffset = new(rng.GetFloat(), rng.GetFloat());
            }
            

            // Iterate over area and check against peak
            List<Vector3Int> heightmap = new();
            Vector2Int  start   = new(_center.x - _radius, _center.y - _radius),
                        end     = new(_center.x + _radius, _center.y + _radius);

            int xMin = end.x > start.x ? start.x : end.x,
                xMax = end.x > start.x ? end.x : start.x,
                yMin = end.y > start.y ? start.y : end.y,
                yMax = end.y > start.y ? end.y : start.y;

            Vector2 noisePeriod = new(_vertNoiseFreq, _vertNoiseFreq);

            // Utils.Log("Seed: {0}", _seed);
            // Utils.Log("Amplitude: {0}", _vertNoiseAmp);
            // Utils.Log("Apply vertical noise: {0}", _seed != null && _vertNoiseAmp > 0);

            Vector3Int point = new();
            Easer easeIn = new(Easer.Direction.In, Easer.Function.Cube);
            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    point.x = x;
                    point.y = y;
                    Vector2 peakToPoint = point.XY() - peak.Coordinate;
                    float sqrMag = peakToPoint.sqrMagnitude;
                    if ( sqrMag < peak.RadiusSqr )
                    {
                        float noise = 0;
                        if( _seed != null && _vertNoiseAmp > 0)
                        {
                            Vector2 noisePoint = new( 
                                (float)x / xMax + randOffset.x, 
                                (float)y / yMax + randOffset.y
                            );

                            noise = Generator.FBM(
                                noisePoint + randOffset,
                                noisePeriod,
                                0, _vertNoiseOctaves, _vertNoiseAmp, _vertNoiseFreq
                            );  
    
                            // if (y == peak.Coordinate.y && false)
                            // {
                            //     Utils.Log("x: {0}", x);
                            //     Utils.Log("noise: {0}", noise);    
                            //     Utils.Log("easin.Value(): {0}", easeIn.Value(1 - (sqrMag / peak.RadiusSqr)));
                            //     Utils.Log("noise: {0}", noise);
                            // }

                            noise *= easeIn.Value( 1 - (sqrMag / peak.RadiusSqr) );
                        }                        

                        point.z = Mathf.RoundToInt(peak.PointOnSlope(point.XY()).z + noise);
                        if (point.z > 0)
                        {
                            heightmap.Add(point);
                        }
                    }
                }
            }

            return heightmap;
        }



    }
}