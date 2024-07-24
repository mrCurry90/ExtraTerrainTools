using System;

namespace TerrainTools.NoiseGenerator
{
    public class NoiseParameterLimits
    {
        public class Limit<T>
        {
            public T Min { get; }
            public T Max { get; }

            private Limit() {}

            public Limit(T min, T max) {
                Min = min;
                Max = max;
            }
        }

        public readonly Limit<int> Octaves;
        public readonly Limit<float> Amplitude;
        public readonly Limit<float> Frequency;
        public readonly Limit<float> PeriodX;
        public readonly Limit<float> PeriodY;
        public readonly Limit<int> Floor;
        public readonly Limit<int> Mid;
        public readonly Limit<int> Ceiling;
        public readonly Limit<Easer.Function> Base;
        public readonly Limit<Easer.Function> Crest;
        

        private NoiseParameterLimits() { }

        public NoiseParameterLimits(
            int octavesMin, int octavesMax,
            float amplitudeMin,float  amplitudeMax,
            float frequencyMin, float frequencyMax,
            float periodXMin, float periodXMax,
            float periodYMin, float periodYMax,
            int floorMin, int floorMax,
            int midMin, int midMax,
            int ceilingMin, int ceilingMax       
        ) {
            Octaves     = new(octavesMin, octavesMax);
            Amplitude   = new(amplitudeMin, amplitudeMax);
            Frequency   = new(frequencyMin, frequencyMax);
            PeriodX     = new(periodXMin, periodXMax);
            PeriodY     = new(periodYMin, periodYMax);
            Floor       = new(floorMin, floorMax);
            Mid         = new(midMin, midMax);
            Ceiling     = new(ceilingMin, ceilingMax);
        }
    }
};