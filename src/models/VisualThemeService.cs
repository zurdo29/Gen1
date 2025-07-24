using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Service for managing visual themes, color palettes, and tile sets
    /// </summary>
    public class VisualThemeService : IVisualThemeService
    {
        private readonly Dictionary<string, VisualTheme> _themes;
        private readonly List<ColorPalette> _colorPalettes;
        private readonly Dictionary<string, Dictionary<TileType, string>> _tileSets;
        
        public VisualThemeService()
        {
            _themes = new Dictionary<string, VisualTheme>();
            _colorPalettes = new List<ColorPalette>();
            _tileSets = new Dictionary<string, Dictionary<TileType, string>>();
            
            InitializeDefaultThemes();
            InitializeDefaultColorPalettes();
            InitializeDefaultTileSets();
        }
        
        /// <summary>
        /// Gets all available visual themes
        /// </summary>
        public List<VisualTheme> GetAvailableThemes()
        {
            return _themes.Values.ToList();
        }
        
        /// <summary>
        /// Gets a theme by name
        /// </summary>
        public VisualTheme GetTheme(string themeName)
        {
            if (string.IsNullOrEmpty(themeName))
                return GetDefaultTheme();
                
            return _themes.ContainsKey(themeName) ? _themes[themeName] : GetDefaultTheme();
        }
        
        /// <summary>
        /// Creates a theme from configuration
        /// </summary>
        public VisualTheme CreateTheme(VisualThemeConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            var validationResult = ValidateTheme(config);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid theme configuration: {string.Join(", ", validationResult.Errors)}");
            }
            
            var theme = VisualTheme.FromConfig(config);
            
            // Apply fallbacks for missing sprites
            ApplyFallbackSprites(theme);
            
            return theme;
        }
        
        /// <summary>
        /// Validates a theme configuration
        /// </summary>
        public ValidationResult ValidateTheme(VisualThemeConfig config)
        {
            var result = new ValidationResult();
            
            if (config == null)
            {
                result.Errors.Add("Theme configuration cannot be null");
                return result;
            }
                
            var errors = config.Validate();
            foreach (var error in errors)
            {
                result.Errors.Add(error);
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets the default theme
        /// </summary>
        public VisualTheme GetDefaultTheme()
        {
            return _themes.ContainsKey("default") ? _themes["default"] : CreateDefaultTheme();
        }
        
        /// <summary>
        /// Registers a new theme
        /// </summary>
        public void RegisterTheme(VisualTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));
                
            if (string.IsNullOrEmpty(theme.Name))
                throw new ArgumentException("Theme name cannot be null or empty");
                
            _themes[theme.Name] = theme;
        }
        
        /// <summary>
        /// Gets available color palettes
        /// </summary>
        public List<ColorPalette> GetAvailableColorPalettes()
        {
            return _colorPalettes.ToList();
        }
        
        /// <summary>
        /// Gets available tile sets
        /// </summary>
        public Dictionary<string, Dictionary<TileType, string>> GetAvailableTileSets()
        {
            return new Dictionary<string, Dictionary<TileType, string>>(_tileSets);
        }
        
        /// <summary>
        /// Initializes default themes
        /// </summary>
        private void InitializeDefaultThemes()
        {
            // Default theme
            var defaultTheme = CreateDefaultTheme();
            _themes["default"] = defaultTheme;
            
            // Fantasy theme
            var fantasyTheme = CreateFantasyTheme();
            _themes["fantasy"] = fantasyTheme;
            
            // Sci-fi theme
            var scifiTheme = CreateSciFiTheme();
            _themes["scifi"] = scifiTheme;
            
            // Retro theme
            var retroTheme = CreateRetroTheme();
            _themes["retro"] = retroTheme;
        }
        
        /// <summary>
        /// Creates the default theme
        /// </summary>
        private VisualTheme CreateDefaultTheme()
        {
            var theme = new VisualTheme
            {
                Name = "default",
                Colors = new ColorPalette
                {
                    Primary = "#FFFFFF",
                    Secondary = "#000000",
                    Accent = "#FF0000",
                    Background = "#808080"
                }
            };
            
            // Default tile sprites
            theme.TileSprites[TileType.Empty] = "sprites/tiles/empty.png";
            theme.TileSprites[TileType.Ground] = "sprites/tiles/ground.png";
            theme.TileSprites[TileType.Wall] = "sprites/tiles/wall.png";
            theme.TileSprites[TileType.Water] = "sprites/tiles/water.png";
            theme.TileSprites[TileType.Grass] = "sprites/tiles/grass.png";
            theme.TileSprites[TileType.Stone] = "sprites/tiles/stone.png";
            theme.TileSprites[TileType.Sand] = "sprites/tiles/sand.png";
            theme.TileSprites[TileType.Lava] = "sprites/tiles/lava.png";
            theme.TileSprites[TileType.Ice] = "sprites/tiles/ice.png";
            
            // Default entity sprites
            theme.EntitySprites[EntityType.Player] = "sprites/entities/player.png";
            theme.EntitySprites[EntityType.Enemy] = "sprites/entities/enemy.png";
            theme.EntitySprites[EntityType.Item] = "sprites/entities/item.png";
            theme.EntitySprites[EntityType.PowerUp] = "sprites/entities/powerup.png";
            theme.EntitySprites[EntityType.NPC] = "sprites/entities/npc.png";
            theme.EntitySprites[EntityType.Exit] = "sprites/entities/exit.png";
            theme.EntitySprites[EntityType.Checkpoint] = "sprites/entities/checkpoint.png";
            theme.EntitySprites[EntityType.Obstacle] = "sprites/entities/obstacle.png";
            theme.EntitySprites[EntityType.Trigger] = "sprites/entities/trigger.png";
            
            return theme;
        }
        
        /// <summary>
        /// Creates a fantasy-themed visual theme
        /// </summary>
        private VisualTheme CreateFantasyTheme()
        {
            var theme = new VisualTheme
            {
                Name = "fantasy",
                Colors = new ColorPalette
                {
                    Primary = "#8B4513",    // Brown
                    Secondary = "#228B22",  // Forest Green
                    Accent = "#FFD700",     // Gold
                    Background = "#2F4F2F"  // Dark Green
                }
            };
            
            // Fantasy tile sprites
            theme.TileSprites[TileType.Empty] = "sprites/fantasy/tiles/sky.png";
            theme.TileSprites[TileType.Ground] = "sprites/fantasy/tiles/dirt.png";
            theme.TileSprites[TileType.Wall] = "sprites/fantasy/tiles/stone_wall.png";
            theme.TileSprites[TileType.Water] = "sprites/fantasy/tiles/river.png";
            theme.TileSprites[TileType.Grass] = "sprites/fantasy/tiles/forest_floor.png";
            theme.TileSprites[TileType.Stone] = "sprites/fantasy/tiles/cobblestone.png";
            theme.TileSprites[TileType.Sand] = "sprites/fantasy/tiles/desert_sand.png";
            theme.TileSprites[TileType.Lava] = "sprites/fantasy/tiles/molten_rock.png";
            theme.TileSprites[TileType.Ice] = "sprites/fantasy/tiles/frozen_ground.png";
            
            // Fantasy entity sprites
            theme.EntitySprites[EntityType.Player] = "sprites/fantasy/entities/hero.png";
            theme.EntitySprites[EntityType.Enemy] = "sprites/fantasy/entities/goblin.png";
            theme.EntitySprites[EntityType.Item] = "sprites/fantasy/entities/treasure.png";
            theme.EntitySprites[EntityType.PowerUp] = "sprites/fantasy/entities/magic_potion.png";
            theme.EntitySprites[EntityType.NPC] = "sprites/fantasy/entities/villager.png";
            theme.EntitySprites[EntityType.Exit] = "sprites/fantasy/entities/portal.png";
            theme.EntitySprites[EntityType.Checkpoint] = "sprites/fantasy/entities/waystone.png";
            theme.EntitySprites[EntityType.Obstacle] = "sprites/fantasy/entities/boulder.png";
            theme.EntitySprites[EntityType.Trigger] = "sprites/fantasy/entities/magic_circle.png";
            
            return theme;
        }
        
        /// <summary>
        /// Creates a sci-fi themed visual theme
        /// </summary>
        private VisualTheme CreateSciFiTheme()
        {
            var theme = new VisualTheme
            {
                Name = "scifi",
                Colors = new ColorPalette
                {
                    Primary = "#00FFFF",    // Cyan
                    Secondary = "#4169E1",  // Royal Blue
                    Accent = "#FF00FF",     // Magenta
                    Background = "#191970"  // Midnight Blue
                }
            };
            
            // Sci-fi tile sprites
            theme.TileSprites[TileType.Empty] = "sprites/scifi/tiles/space.png";
            theme.TileSprites[TileType.Ground] = "sprites/scifi/tiles/metal_floor.png";
            theme.TileSprites[TileType.Wall] = "sprites/scifi/tiles/hull_wall.png";
            theme.TileSprites[TileType.Water] = "sprites/scifi/tiles/coolant.png";
            theme.TileSprites[TileType.Grass] = "sprites/scifi/tiles/bio_deck.png";
            theme.TileSprites[TileType.Stone] = "sprites/scifi/tiles/asteroid.png";
            theme.TileSprites[TileType.Sand] = "sprites/scifi/tiles/moon_dust.png";
            theme.TileSprites[TileType.Lava] = "sprites/scifi/tiles/plasma.png";
            theme.TileSprites[TileType.Ice] = "sprites/scifi/tiles/cryo_floor.png";
            
            // Sci-fi entity sprites
            theme.EntitySprites[EntityType.Player] = "sprites/scifi/entities/astronaut.png";
            theme.EntitySprites[EntityType.Enemy] = "sprites/scifi/entities/alien.png";
            theme.EntitySprites[EntityType.Item] = "sprites/scifi/entities/data_chip.png";
            theme.EntitySprites[EntityType.PowerUp] = "sprites/scifi/entities/energy_cell.png";
            theme.EntitySprites[EntityType.NPC] = "sprites/scifi/entities/android.png";
            theme.EntitySprites[EntityType.Exit] = "sprites/scifi/entities/teleporter.png";
            theme.EntitySprites[EntityType.Checkpoint] = "sprites/scifi/entities/save_terminal.png";
            theme.EntitySprites[EntityType.Obstacle] = "sprites/scifi/entities/force_field.png";
            theme.EntitySprites[EntityType.Trigger] = "sprites/scifi/entities/sensor.png";
            
            return theme;
        }
        
        /// <summary>
        /// Creates a retro-themed visual theme
        /// </summary>
        private VisualTheme CreateRetroTheme()
        {
            var theme = new VisualTheme
            {
                Name = "retro",
                Colors = new ColorPalette
                {
                    Primary = "#FF6B35",    // Orange
                    Secondary = "#F7931E",  // Amber
                    Accent = "#FFE135",     // Yellow
                    Background = "#1B1B1B"  // Dark Gray
                }
            };
            
            // Retro tile sprites (8-bit style)
            theme.TileSprites[TileType.Empty] = "sprites/retro/tiles/black.png";
            theme.TileSprites[TileType.Ground] = "sprites/retro/tiles/brick.png";
            theme.TileSprites[TileType.Wall] = "sprites/retro/tiles/block.png";
            theme.TileSprites[TileType.Water] = "sprites/retro/tiles/blue_tile.png";
            theme.TileSprites[TileType.Grass] = "sprites/retro/tiles/green_tile.png";
            theme.TileSprites[TileType.Stone] = "sprites/retro/tiles/gray_block.png";
            theme.TileSprites[TileType.Sand] = "sprites/retro/tiles/yellow_tile.png";
            theme.TileSprites[TileType.Lava] = "sprites/retro/tiles/red_tile.png";
            theme.TileSprites[TileType.Ice] = "sprites/retro/tiles/white_tile.png";
            
            // Retro entity sprites
            theme.EntitySprites[EntityType.Player] = "sprites/retro/entities/player_sprite.png";
            theme.EntitySprites[EntityType.Enemy] = "sprites/retro/entities/enemy_sprite.png";
            theme.EntitySprites[EntityType.Item] = "sprites/retro/entities/coin.png";
            theme.EntitySprites[EntityType.PowerUp] = "sprites/retro/entities/star.png";
            theme.EntitySprites[EntityType.NPC] = "sprites/retro/entities/character.png";
            theme.EntitySprites[EntityType.Exit] = "sprites/retro/entities/door.png";
            theme.EntitySprites[EntityType.Checkpoint] = "sprites/retro/entities/flag.png";
            theme.EntitySprites[EntityType.Obstacle] = "sprites/retro/entities/spike.png";
            theme.EntitySprites[EntityType.Trigger] = "sprites/retro/entities/switch.png";
            
            return theme;
        }
        
        /// <summary>
        /// Initializes default color palettes
        /// </summary>
        private void InitializeDefaultColorPalettes()
        {
            // Classic palette
            _colorPalettes.Add(new ColorPalette
            {
                Primary = "#FFFFFF",
                Secondary = "#000000",
                Accent = "#FF0000",
                Background = "#808080",
                CustomColors = new Dictionary<string, string>
                {
                    ["highlight"] = "#FFFF00",
                    ["shadow"] = "#404040"
                }
            });
            
            // Warm palette
            _colorPalettes.Add(new ColorPalette
            {
                Primary = "#FF6B35",
                Secondary = "#8B4513",
                Accent = "#FFD700",
                Background = "#FFA500",
                CustomColors = new Dictionary<string, string>
                {
                    ["fire"] = "#FF4500",
                    ["earth"] = "#D2691E"
                }
            });
            
            // Cool palette
            _colorPalettes.Add(new ColorPalette
            {
                Primary = "#00BFFF",
                Secondary = "#4169E1",
                Accent = "#00FFFF",
                Background = "#191970",
                CustomColors = new Dictionary<string, string>
                {
                    ["ice"] = "#B0E0E6",
                    ["ocean"] = "#006994"
                }
            });
            
            // Nature palette
            _colorPalettes.Add(new ColorPalette
            {
                Primary = "#228B22",
                Secondary = "#8B4513",
                Accent = "#FFD700",
                Background = "#2F4F2F",
                CustomColors = new Dictionary<string, string>
                {
                    ["leaf"] = "#32CD32",
                    ["bark"] = "#8B4513"
                }
            });
        }
        
        /// <summary>
        /// Initializes default tile sets
        /// </summary>
        private void InitializeDefaultTileSets()
        {
            // Basic tile set
            _tileSets["basic"] = new Dictionary<TileType, string>
            {
                [TileType.Empty] = "sprites/basic/empty.png",
                [TileType.Ground] = "sprites/basic/ground.png",
                [TileType.Wall] = "sprites/basic/wall.png",
                [TileType.Water] = "sprites/basic/water.png",
                [TileType.Grass] = "sprites/basic/grass.png",
                [TileType.Stone] = "sprites/basic/stone.png",
                [TileType.Sand] = "sprites/basic/sand.png",
                [TileType.Lava] = "sprites/basic/lava.png",
                [TileType.Ice] = "sprites/basic/ice.png"
            };
            
            // Detailed tile set
            _tileSets["detailed"] = new Dictionary<TileType, string>
            {
                [TileType.Empty] = "sprites/detailed/void.png",
                [TileType.Ground] = "sprites/detailed/cobblestone.png",
                [TileType.Wall] = "sprites/detailed/brick_wall.png",
                [TileType.Water] = "sprites/detailed/flowing_water.png",
                [TileType.Grass] = "sprites/detailed/lush_grass.png",
                [TileType.Stone] = "sprites/detailed/rough_stone.png",
                [TileType.Sand] = "sprites/detailed/fine_sand.png",
                [TileType.Lava] = "sprites/detailed/bubbling_lava.png",
                [TileType.Ice] = "sprites/detailed/crystal_ice.png"
            };
            
            // Minimalist tile set
            _tileSets["minimalist"] = new Dictionary<TileType, string>
            {
                [TileType.Empty] = "sprites/minimal/black.png",
                [TileType.Ground] = "sprites/minimal/white.png",
                [TileType.Wall] = "sprites/minimal/gray.png",
                [TileType.Water] = "sprites/minimal/blue.png",
                [TileType.Grass] = "sprites/minimal/green.png",
                [TileType.Stone] = "sprites/minimal/dark_gray.png",
                [TileType.Sand] = "sprites/minimal/yellow.png",
                [TileType.Lava] = "sprites/minimal/red.png",
                [TileType.Ice] = "sprites/minimal/light_blue.png"
            };
        }
        
        /// <summary>
        /// Applies fallback sprites for missing entries in a theme
        /// </summary>
        private void ApplyFallbackSprites(VisualTheme theme)
        {
            var defaultTheme = GetDefaultTheme();
            
            // Apply fallback tile sprites
            foreach (TileType tileType in Enum.GetValues<TileType>())
            {
                if (!theme.TileSprites.ContainsKey(tileType) && defaultTheme.TileSprites.ContainsKey(tileType))
                {
                    theme.TileSprites[tileType] = defaultTheme.TileSprites[tileType];
                }
            }
            
            // Apply fallback entity sprites
            foreach (EntityType entityType in Enum.GetValues<EntityType>())
            {
                if (!theme.EntitySprites.ContainsKey(entityType) && defaultTheme.EntitySprites.ContainsKey(entityType))
                {
                    theme.EntitySprites[entityType] = defaultTheme.EntitySprites[entityType];
                }
            }
        }
    }
}