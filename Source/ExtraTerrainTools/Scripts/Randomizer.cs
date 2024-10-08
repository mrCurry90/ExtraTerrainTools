using System.Security.Cryptography;
using System.Text;
using System;

namespace TerrainTools
{
    public class Randomizer {
        private Random rng = null;

        public Randomizer() {
            Seed();
        }
        public Randomizer(string seed) {
            Seed(seed);
        }

        /// <summary>
        /// Return random integer in range min (inclusive)  to max (exlusive)
        /// </summary>
        public int GetInt(int min, int max)
        {          
            return rng.Next(min, max);
        }

        public int GetInt(int max) { 
            return rng.Next(max);
        }

        public int GetInt() { 
            return rng.Next();
        }

        public float GetFloat(float min, float max)
        {
            double  a = min,
                    b = max;
            
            return (float)(a + (b - a) * rng.NextDouble());
        }

        public float GetFloat(float max) { 
            double a = max;
            return (float)(a * rng.NextDouble());
        }
        
        public float GetFloat() {
            return (float)rng.NextDouble();
        }

        public void Seed(string seed = null)
        {
            if( seed == null )
            {
                rng = new();
                return;
            }

            var hash = BitConverter.ToInt32( SHA1.Create().ComputeHash( Encoding.UTF8.GetBytes(seed) ) );
            rng = new(hash);
        }
    }    
}