using UnityEngine;
namespace TerrainTools.NoiseGenerator
{
    public readonly struct NoiseParameters
    {
        /// <summary>
        /// Randomizer seed
        /// null = System.Time-based seed, string = Hash-based seed
        /// </summary>
        public string Seed { get; }
        /// <summary>
        /// Number of octaves of noise to generate, higher = more detail
        /// </summary>
        public int Octaves { get; }
        /// <summary>
        /// Initial amplitude of noise
        /// </summary>
        public float Amplitude { get;}
        /// <summary>
        /// Initial frequency of noise
        /// </summary>
        public float Frequency { get; }
        /// <summary>
        /// Periodicity of noise in X and Y axis
        /// </summary>
        public Vector2 Period { get; }
        /// <summary>
        /// Minimum allowed height, hard cutoff
        /// </summary>
        public int Floor { get; }        
        /// <summary>
        /// Maximum allowed height, hard cutoff
        /// </summary>
        public int Ceiling { get; }
        /// <summary>
        /// Mid point, hard cutoff
        /// </summary>
        public int Mid { get; }
        /// <summary>
        /// Base slope of noise curve
        /// </summary>
        public Easer.Function Base { get; }
        /// <summary>
        /// Crest of noise curve
        /// </summary>
        public Easer.Function Crest { get; }


        public NoiseParameters(
            string seed,
            int octaves,
            float amplitude,
            float frequency,
            float periodX,
            float periodY,
            int floor,
            int mid,
            int ceiling,
            Easer.Function baseCurve,
            Easer.Function crestCurve
            
        )
        {
            Seed        = seed;
            Octaves     = octaves;
            Amplitude   = amplitude;
            Frequency   = frequency;
            Period      = new(periodX, periodY);
            Floor       = floor;
            Mid         = mid;
            Ceiling     = ceiling;
            Base        = baseCurve;
            Crest       = crestCurve;
        }

        public override string ToString()
        {
            return string.Format(
                "Seed={0}, Oct={1}, Amp={2}, Freq={3}, Period={4}, Floor={5}, Mid={6}, Ceiling={7}, Base={8}, Crest={9}",
                Seed,
                Octaves,
                Amplitude,
                Frequency,
                Period,
                Floor,
                Mid,
                Ceiling,
                Base,
                Crest
            );
        }
        public string[] ToStrings()
        {
            return new string[] {
                "Seed=" + Seed,
				"Oct=" + Octaves,
				"Amp=" + Amplitude,
				"Freq=" + Frequency,
				"Period=" + Period,
				"Floor=" + Floor,
				"Mid=" + Mid,
				"Ceiling=" + Ceiling,
				"Base=" + Base,
				"Crest=" + Crest
            };
        }
    }
}