using System;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Interface for random number generation with seeding support
    /// </summary>
    public interface IRandomGenerator
    {
        /// <summary>
        /// Sets the seed for reproducible random generation
        /// </summary>
        /// <param name="seed">Random seed value</param>
        void SetSeed(int seed);
        
        /// <summary>
        /// Gets the current seed value
        /// </summary>
        /// <returns>Current seed</returns>
        int GetSeed();
        
        /// <summary>
        /// Generates a random integer between min (inclusive) and max (exclusive)
        /// </summary>
        /// <param name="min">Minimum value (inclusive)</param>
        /// <param name="max">Maximum value (exclusive)</param>
        /// <returns>Random integer</returns>
        int Next(int min, int max);
        
        /// <summary>
        /// Generates a random integer between 0 and max (exclusive)
        /// </summary>
        /// <param name="max">Maximum value (exclusive)</param>
        /// <returns>Random integer</returns>
        int Next(int max);
        
        /// <summary>
        /// Generates a random integer
        /// </summary>
        /// <returns>Random integer</returns>
        int Next();
        
        /// <summary>
        /// Generates a random double between 0.0 and 1.0
        /// </summary>
        /// <returns>Random double</returns>
        double NextDouble();
        
        /// <summary>
        /// Generates a random float between 0.0 and 1.0
        /// </summary>
        /// <returns>Random float</returns>
        float NextFloat();
        
        /// <summary>
        /// Generates a random boolean value
        /// </summary>
        /// <returns>Random boolean</returns>
        bool NextBool();
        
        /// <summary>
        /// Generates a random boolean with specified probability
        /// </summary>
        /// <param name="probability">Probability of true (0.0 to 1.0)</param>
        /// <returns>Random boolean</returns>
        bool NextBool(float probability);
    }
}