using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Service for applying visual themes to levels with fallback handling
    /// </summary>
    public class ThemeApplicationService
    {
        private readonly IVisualThemeService _themeService;
        private readonly Dictionary<string, string> _fallbackAssets;
        
        /// <summary>
        /// Initializes a new instance of the ThemeApplicationService
        /// </summary>
        /// <param name="themeService">Visual theme service for theme management</param>
        public ThemeApplicationService(IVisualThemeService themeService)
        {
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _fallbackAssets = InitializeFallbackAssets();
        }
        
        /// <summary>
        /// Applies a visual theme to a level with comprehensive fallback handling
        /// </summary>
        /// <param name="level">Level to apply theme to</param>
        /// <param name="theme">Visual theme to apply</param>
        /// <returns>List of warnings for missing assets that used fallbacks</returns>
        public List<string> ApplyThemeToLevel(Level level, VisualTheme theme)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            var warnings = new List<string>();
            
            // Apply theme to terrain tiles
            warnings.AddRange(ApplyThemeToTerrain(level, theme));
            
            // Apply theme to entities
            warnings.AddRange(ApplyThemeToEntities(level, theme));
            
            // Apply color palette to level
            ApplyColorPalette(level, theme);
            
            // Store theme metadata
            StoreThemeMetadata(level, theme);
            
            return warnings;
        }
        
        /// <summary>
        /// Applies theme to terrain tiles with fallback handling
        /// </summary>
        private List<string> ApplyThemeToTerrain(Level level, VisualTheme theme)
        {
            var warnings = new List<string>();
            var appliedTileSprites = new Dictionary<TileType, string>();
            
            // Get all unique tile types in the terrain
            var usedTileTypes = GetUsedTileTypes(level.Terrain);
            
            foreach (var tileType in usedTileTypes)
            {
                string spritePath;
                
                if (theme.TileSprites.ContainsKey(tileType))
                {
                    spritePath = theme.TileSprites[tileType];
                    
                    // Check if the asset exists
                    if (!AssetExists(spritePath))
                    {
                        var fallbackPath = GetFallbackAsset($"tile_{tileType}");
                        warnings.Add($"Tile sprite '{spritePath}' for {tileType} not found, using fallback '{fallbackPath}'");
                        spritePath = fallbackPath;
                    }
                }
                else
                {
                    // No sprite defined for this tile type, use fallback
                    spritePath = GetFallbackAsset($"tile_{tileType}");
                    warnings.Add($"No sprite defined for tile type {tileType} in theme '{theme.Name}', using fallback '{spritePath}'");
                }
                
                appliedTileSprites[tileType] = spritePath;
            }
            
            // Store the applied tile sprites in level metadata
            level.Metadata["AppliedTileSprites"] = appliedTileSprites;
            
            return warnings;
        }
        
        /// <summary>
        /// Applies theme to entities with fallback handling
        /// </summary>
        private List<string> ApplyThemeToEntities(Level level, VisualTheme theme)
        {
            var warnings = new List<string>();
            
            foreach (var entity in level.Entities)
            {
                string spritePath;
                
                if (theme.EntitySprites.ContainsKey(entity.Type))
                {
                    spritePath = theme.EntitySprites[entity.Type];
                    
                    // Check if the asset exists
                    if (!AssetExists(spritePath))
                    {
                        var fallbackPath = GetFallbackAsset($"entity_{entity.Type}");
                        warnings.Add($"Entity sprite '{spritePath}' for {entity.Type} not found, using fallback '{fallbackPath}'");
                        spritePath = fallbackPath;
                    }
                }
                else
                {
                    // No sprite defined for this entity type, use fallback
                    spritePath = GetFallbackAsset($"entity_{entity.Type}");
                    warnings.Add($"No sprite defined for entity type {entity.Type} in theme '{theme.Name}', using fallback '{spritePath}'");
                }
                
                // Apply the sprite to the entity
                entity.Properties["Sprite"] = spritePath;
                entity.Properties["ThemeApplied"] = theme.Name;
            }
            
            return warnings;
        }
        
        /// <summary>
        /// Applies color palette to the level
        /// </summary>
        private void ApplyColorPalette(Level level, VisualTheme theme)
        {
            // Store color palette in metadata for use by rendering system
            level.Metadata["ColorPalette"] = theme.Colors;
            
            // Apply theme-specific color properties to entities if they support it
            foreach (var entity in level.Entities)
            {
                // Some entities might have color properties that can be themed
                if (entity.Properties.ContainsKey("Color"))
                {
                    var entityColorKey = $"{entity.Type}_Color";
                    if (theme.Colors.CustomColors.ContainsKey(entityColorKey))
                    {
                        entity.Properties["Color"] = theme.Colors.CustomColors[entityColorKey];
                    }
                }
            }
        }
        
        /// <summary>
        /// Stores theme metadata in the level
        /// </summary>
        private void StoreThemeMetadata(Level level, VisualTheme theme)
        {
            level.Metadata["VisualTheme"] = theme.Name;
            level.Metadata["ThemeProperties"] = new Dictionary<string, object>(theme.Properties);
            level.Metadata["ThemeAppliedAt"] = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Gets all unique tile types used in the terrain
        /// </summary>
        private HashSet<TileType> GetUsedTileTypes(TileMap terrain)
        {
            var usedTypes = new HashSet<TileType>();
            
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    usedTypes.Add(terrain.GetTile(x, y));
                }
            }
            
            return usedTypes;
        }
        
        /// <summary>
        /// Checks if an asset file exists
        /// </summary>
        private bool AssetExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
                
            // In a real implementation, this would check the actual asset system
            // For now, we'll simulate by checking if it's a valid path format
            try
            {
                // Check if it's a valid path format and not obviously invalid
                return !string.IsNullOrWhiteSpace(assetPath) && 
                       !assetPath.Contains("..") && 
                       !Path.GetInvalidPathChars().Any(assetPath.Contains) &&
                       !assetPath.Equals("missing", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets a fallback asset path for a given asset type
        /// </summary>
        private string GetFallbackAsset(string assetType)
        {
            if (_fallbackAssets.ContainsKey(assetType))
            {
                return _fallbackAssets[assetType];
            }
            
            // Return a generic fallback based on asset type
            if (assetType.StartsWith("tile_"))
            {
                return "assets/fallback/default_tile.png";
            }
            else if (assetType.StartsWith("entity_"))
            {
                return "assets/fallback/default_entity.png";
            }
            
            return "assets/fallback/missing.png";
        }
        
        /// <summary>
        /// Initializes the fallback asset mappings
        /// </summary>
        private Dictionary<string, string> InitializeFallbackAssets()
        {
            return new Dictionary<string, string>
            {
                // Tile fallbacks
                ["tile_Ground"] = "assets/fallback/ground.png",
                ["tile_Wall"] = "assets/fallback/wall.png",
                ["tile_Water"] = "assets/fallback/water.png",
                ["tile_Lava"] = "assets/fallback/lava.png",
                ["tile_Ice"] = "assets/fallback/ice.png",
                ["tile_Sand"] = "assets/fallback/sand.png",
                ["tile_Grass"] = "assets/fallback/grass.png",
                ["tile_Stone"] = "assets/fallback/stone.png",
                
                // Entity fallbacks
                ["entity_Player"] = "assets/fallback/player.png",
                ["entity_Enemy"] = "assets/fallback/enemy.png",
                ["entity_Item"] = "assets/fallback/item.png",
                ["entity_Collectible"] = "assets/fallback/collectible.png",
                ["entity_PowerUp"] = "assets/fallback/powerup.png",
                ["entity_Obstacle"] = "assets/fallback/obstacle.png",
                ["entity_NPC"] = "assets/fallback/npc.png",
                ["entity_Checkpoint"] = "assets/fallback/checkpoint.png",
                ["entity_Exit"] = "assets/fallback/exit.png",
                ["entity_Key"] = "assets/fallback/key.png",
                ["entity_Door"] = "assets/fallback/door.png",
                ["entity_Trap"] = "assets/fallback/trap.png"
            };
        }
        
        /// <summary>
        /// Validates that a theme can be applied to a level
        /// </summary>
        /// <param name="level">Level to validate</param>
        /// <param name="theme">Theme to validate</param>
        /// <returns>Validation result with any issues found</returns>
        public ValidationResult ValidateThemeApplication(Level level, VisualTheme theme)
        {
            var result = new ValidationResult();
            
            if (level == null)
            {
                result.Errors.Add("Level cannot be null");
                return result;
            }
            
            if (theme == null)
            {
                result.Errors.Add("Theme cannot be null");
                return result;
            }
            
            // Check if theme has required assets for the level's content
            var usedTileTypes = GetUsedTileTypes(level.Terrain);
            var missingTileSprites = usedTileTypes.Where(t => !theme.TileSprites.ContainsKey(t)).ToList();
            
            if (missingTileSprites.Any())
            {
                result.Warnings.Add($"Theme '{theme.Name}' is missing sprites for tile types: {string.Join(", ", missingTileSprites)}");
            }
            
            var usedEntityTypes = level.Entities.Select(e => e.Type).Distinct().ToList();
            var missingEntitySprites = usedEntityTypes.Where(e => !theme.EntitySprites.ContainsKey(e)).ToList();
            
            if (missingEntitySprites.Any())
            {
                result.Warnings.Add($"Theme '{theme.Name}' is missing sprites for entity types: {string.Join(", ", missingEntitySprites)}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Creates a theme application report
        /// </summary>
        /// <param name="level">Level that had theme applied</param>
        /// <param name="theme">Theme that was applied</param>
        /// <param name="warnings">Warnings generated during application</param>
        /// <returns>Detailed application report</returns>
        public ThemeApplicationReport CreateApplicationReport(Level level, VisualTheme theme, List<string> warnings)
        {
            var usedTileTypes = GetUsedTileTypes(level.Terrain);
            var usedEntityTypes = level.Entities.Select(e => e.Type).Distinct().ToList();
            
            return new ThemeApplicationReport
            {
                ThemeName = theme.Name,
                LevelName = level.Name,
                AppliedAt = DateTime.UtcNow,
                TileTypesProcessed = usedTileTypes.Count,
                EntitiesProcessed = level.Entities.Count,
                EntityTypesProcessed = usedEntityTypes.Count,
                Warnings = warnings,
                Success = true,
                FallbacksUsed = warnings.Count
            };
        }
    }
    
    /// <summary>
    /// Report of theme application results
    /// </summary>
    public class ThemeApplicationReport
    {
        public string ThemeName { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public int TileTypesProcessed { get; set; }
        public int EntitiesProcessed { get; set; }
        public int EntityTypesProcessed { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public bool Success { get; set; }
        public int FallbacksUsed { get; set; }
    }}
