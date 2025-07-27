namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Represents a 2D position
    /// </summary>
    public class Position
    {
        /// <summary>
        /// X coordinate
        /// </summary>
        public float X { get; set; }
        
        /// <summary>
        /// Y coordinate
        /// </summary>
        public float Y { get; set; }
        
        /// <summary>
        /// Creates a new Position
        /// </summary>
        public Position() { }
        
        /// <summary>
        /// Creates a new Position with specified coordinates
        /// </summary>
        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }
        
        /// <summary>
        /// Converts to System.Numerics.Vector2
        /// </summary>
        public System.Numerics.Vector2 ToVector2()
        {
            return new System.Numerics.Vector2(X, Y);
        }
        
        /// <summary>
        /// Creates Position from System.Numerics.Vector2
        /// </summary>
        public static Position FromVector2(System.Numerics.Vector2 vector)
        {
            return new Position(vector.X, vector.Y);
        }
    }
}