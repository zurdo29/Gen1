using System;
using System.Collections.Generic;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Enhanced level assembler with AI-powered content generation
    /// </summary>
    public class AIEnhancedLevelAssembler : ILevelAssembler
    {
        private readonly LevelAssembler _baseLevelAssembler;
        private readonly IAIContentGenerator _aiContentGenerator;
        private readonly ILogger _logger;

        public AIEnhancedLevelAssembler(
            LevelAssembler baseLevelAssembler,
            IAIContentGenerator aiContentGenerator,
            ILogger logger)
        {
            _baseLevelAssembler = baseLevelAssembler ?? throw new ArgumentNullException(nameof(baseLevelAssembler));
            _aiContentGenerator = aiContentGenerator ?? throw new ArgumentNullException(nameof(aiContentGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Assembles a complete level with AI-enhanced content
        /// </summary>
        /// <param name="terrain">Generated terrain</param>
        /// <param name="entities">Placed entities</param>
        /// <param name="config">Generation configuration</param>
        /// <returns>Assembled level with AI-generated content</returns>
        public Level AssembleLevel(TileMap terrain, List<Entity> entities, GenerationConfig config)
        {
            // Use base assembler to create the level structure
            var level = _baseLevelAssembler.AssembleLevel(terrain, entities, config);

            // Enhance with AI-generated content if available
            if (_aiContentGenerator.IsAvailable())
            {
                try
                {
                    EnhanceLevelWithAI(level, config);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to enhance level with AI content: {ex.Message}", ex);
                    // Continue with the base level if AI enhancement fails
                }
            }
            else
            {
                _logger.Info("AI content generation not available, using fallback content");
            }

            return level;
        }

        /// <summary>
        /// Applies a visual theme to a level
        /// </summary>
        /// <param name="level">Level to apply theme to</param>
        /// <param name="theme">Visual theme to apply</param>
        public void ApplyVisualTheme(Level level, VisualTheme theme)
        {
            _baseLevelAssembler.ApplyVisualTheme(level, theme);

            // Generate AI-enhanced level name based on theme
            if (_aiContentGenerator.IsAvailable())
            {
                try
                {
                    var aiGeneratedName = _aiContentGenerator.GenerateLevelName(level, theme);
                    if (!string.IsNullOrWhiteSpace(aiGeneratedName))
                    {
                        level.Name = aiGeneratedName;
                        level.Metadata["AIGeneratedName"] = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to generate AI level name: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Enhances the level with AI-generated content
        /// </summary>
        /// <param name="level">Level to enhance</param>
        /// <param name="config">Generation configuration</param>
        private void EnhanceLevelWithAI(Level level, GenerationConfig config)
        {
            var theme = GetThemeFromConfig(config);
            var enhancedEntities = 0;

            foreach (var entity in level.Entities)
            {
                try
                {
                    // Generate description for the entity
                    var description = _aiContentGenerator.GenerateItemDescription(entity.Type, theme);
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        entity.Properties["Description"] = description;
                        entity.Properties["AIGenerated"] = true;
                        enhancedEntities++;
                    }

                    // Generate dialogue for interactive entities
                    if (IsInteractiveEntity(entity.Type))
                    {
                        var dialogueLineCount = GetDialogueLineCount(entity.Type);
                        var dialogue = _aiContentGenerator.GenerateNPCDialogue(entity.Type, dialogueLineCount);
                        
                        if (dialogue != null && dialogue.Length > 0)
                        {
                            entity.Properties["Dialogue"] = dialogue;
                            entity.Properties["DialogueCount"] = dialogue.Length;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to generate AI content for entity {entity.Type}: {ex.Message}");
                    // Continue with other entities if one fails
                }
            }

            // Update level metadata with AI enhancement information
            level.Metadata["AIEnhancedEntities"] = enhancedEntities;
            level.Metadata["AIEnhancementTimestamp"] = DateTime.UtcNow;
            level.Metadata["TotalEntities"] = level.Entities.Count;
            level.Metadata["AIEnhancementRatio"] = level.Entities.Count > 0 
                ? (double)enhancedEntities / level.Entities.Count 
                : 0.0;

            _logger.Info($"Enhanced {enhancedEntities} out of {level.Entities.Count} entities with AI content");
        }

        /// <summary>
        /// Gets the visual theme from configuration or creates a default one
        /// </summary>
        /// <param name="config">Generation configuration</param>
        /// <returns>Visual theme for AI content generation</returns>
        private VisualTheme GetThemeFromConfig(GenerationConfig config)
        {
            if (config.VisualTheme != null)
            {
                // Convert color palette dictionary to ColorPalette object
                var colorPalette = new ColorPalette();
                if (config.VisualTheme.ColorPalette != null)
                {
                    // Copy color values from dictionary to ColorPalette properties
                    // This is a simplified conversion - in a real implementation you'd map specific colors
                }

                return new VisualTheme
                {
                    Name = config.VisualTheme.ThemeName ?? "Default",
                    TileSprites = new Dictionary<TileType, string>(),
                    EntitySprites = new Dictionary<EntityType, string>(),
                    Colors = colorPalette
                };
            }

            // Return a default theme for AI generation
            return new VisualTheme
            {
                Name = "Classic",
                TileSprites = new Dictionary<TileType, string>(),
                EntitySprites = new Dictionary<EntityType, string>(),
                Colors = new ColorPalette()
            };
        }

        /// <summary>
        /// Determines if an entity type should have interactive dialogue
        /// </summary>
        /// <param name="entityType">Type of entity</param>
        /// <returns>True if entity should have dialogue</returns>
        private bool IsInteractiveEntity(EntityType entityType)
        {
            return entityType == EntityType.Enemy || 
                   entityType == EntityType.PowerUp || 
                   entityType == EntityType.Checkpoint;
        }

        /// <summary>
        /// Gets the appropriate number of dialogue lines for an entity type
        /// </summary>
        /// <param name="entityType">Type of entity</param>
        /// <returns>Number of dialogue lines to generate</returns>
        private int GetDialogueLineCount(EntityType entityType)
        {
            return entityType switch
            {
                EntityType.Enemy => 3, // Aggressive/threatening lines
                EntityType.PowerUp => 2, // Encouraging/empowering lines
                EntityType.Checkpoint => 2, // Welcoming/restful lines
                _ => 1
            };
        }
    }
}