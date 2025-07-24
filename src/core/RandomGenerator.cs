using System;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Implementation of IRandomGenerator using System.Random
    /// </summary>
    public class RandomGenerator : IRandomGenerator
    {
        private Random _random = null!;
        private int _seed;
        
        /// <summary>
        /// Creates a new random generator with a random seed
        /// </summary>
        public RandomGenerator()
        {
            _seed = Environment.TickCount;
            _random = new Random(_seed);
        }
        
        /// <summary>
        /// Creates a new random generator with the specified seed
        /// </summary>
        /// <param name="seed">Random seed value</param>
        public RandomGenerator(int seed)
        {
            SetSeed(seed);
        }
        
        /// <summary>
        /// Sets the seed for reproducible random generation
        /// </summary>
        /// <param name="seed">Random seed value</param>
        public void SetSeed(int seed)
        {
            _seed = seed;
            _random = new Random(seed);
        }
        
        /// <summary>
        /// Gets the current seed value
        /// </summary>
        /// <returns>Current seed</returns>
        public int GetSeed()
        {
            return _seed;
        }
        
        /// <summary>
        /// Generates a random integer between min (inclusive) and max (exclusive)
        /// </summary>
        /// <param name="min">Minimum value (inclusive)</param>
        /// <param name="max">Maximum value (exclusive)</param>
        /// <returns>Random integer</returns>
        public int Next(int min, int max)
        {
            return _random.Next(min, max);
        }
        
        /// <summary>
        /// Generates a random integer between 0 and max (exclusive)
        /// </summary>
        /// <param name="max">Maximum value (exclusive)</param>
        /// <returns>Random integer</returns>
        public int Next(int max)
        {
            return _random.Next(max);
        }
        
        /// <summary>
        /// Generates a random integer
        /// </summary>
        /// <returns>Random integer</returns>
        public int Next()
        {
            return _random.Next();
        }
        
        /// <summary>
        /// Generates a random double between 0.0 and 1.0
        /// </summary>
        /// <returns>Random double</returns>
        public double NextDouble()
        {
            return _random.NextDouble();
        }
        
        /// <summary>
        /// Generates a random float between 0.0 and 1.0
        /// </summary>
        /// <returns>Random float</returns>
        public float NextFloat()
        {
            return (float)_random.NextDouble();
        }
        
        /// <summary>
        /// Generates a random boolean value
        /// </summary>
        /// <returns>Random boolean</returns>
        public bool NextBool()
        {
            return _random.Next(2) == 1;
        }
        
        /// <summary>
        /// Generates a random boolean with specified probability
        /// </summary>
        /// <param name="probability">Probability of true (0.0 to 1.0)</param>
        /// <returns>Random boolean</returns>
        public bool NextBool(float probability)
        {
            if (probability <= 0.0f) return false;
            if (probability >= 1.0f) return true;
            
            return NextFloat() < probability;
        }
    }
}