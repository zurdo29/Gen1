namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Represents a 2D tile-based map
    /// </summary>
    public class TileMap
    {
        private readonly TileType[,] _tiles;
        
        /// <summary>
        /// Width of the tile map
        /// </summary>
        public int Width { get; }
        
        /// <summary>
        /// Height of the tile map
        /// </summary>
        public int Height { get; }
        
        /// <summary>
        /// Access to the tiles array
        /// </summary>
        public TileType[,] Tiles => _tiles;
        
        /// <summary>
        /// Creates a new tile map with specified dimensions
        /// </summary>
        /// <param name="width">Width in tiles</param>
        /// <param name="height">Height in tiles</param>
        public TileMap(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new TileType[width, height];
        }
        
        /// <summary>
        /// Gets the tile type at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Tile type at the position</returns>
        public TileType GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return TileType.Wall; // Out of bounds is considered a wall
                
            return _tiles[x, y];
        }
        
        /// <summary>
        /// Sets the tile type at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="type">Tile type to set</param>
        public void SetTile(int x, int y, TileType type)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                _tiles[x, y] = type;
            }
        }
        
        /// <summary>
        /// Checks if the tile at the specified coordinates is walkable
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if the tile is walkable</returns>
        public bool IsWalkable(int x, int y)
        {
            var tile = GetTile(x, y);
            return tile == TileType.Ground || tile == TileType.Grass || tile == TileType.Sand;
        }
    }
}