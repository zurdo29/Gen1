using System;
using System.Collections.Generic;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Service for managing AI content generation across the system
    /// </summary>
    public class AIContentService
    {
        private readonly IAIContentGenerator _aiContentGenerator;
        private readonly ILogger _logger;

        public AIContentService(IAIContentGenerator aiContentGenerator, ILogger logger)
        {
            _aiContentGenerator = aiContentGenerator ?? throw new ArgumentNullException(nameof(aiContentGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Enhances a level with AI-generated content
        /// </summary>
        /// <param name="level">Level to enhance</param>
        /// <param name="theme">Visual theme for context</param>
        /// <returns>Enhancement report</returns>
        public AIEnhancementReport EnhanceLevel(Level level, VisualTheme theme)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));
            
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            var report = new AIEnhancementReport
            {
                StartTime = DateTime.UtcNow,
                TotalEntities = level.Entities?.Count ?? 0,
                IsAIAvailable = _aiContentGenerator.IsAvailable()
            };

            if (!report.IsAIAvailable)
            {
                _logger.Info("AI content generation not available, skipping enhancement");
                report.EndTime = DateTime.UtcNow;
                return report;
            }

            try
            {
                // Generate level name
                EnhanceLevelName(level, theme, report);

                // Enhance entities
                if (level.Entities != null)
                {
                    EnhanceEntities(level.Entities, theme, report);
                }

                report.Success = true;
                _logger.Info($"AI enhancement completed: {report.EnhancedEntities}/{report.TotalEntities} entities enhanced");
            }
            catch (Exception ex)
            {
                report.Success = false;
                report.ErrorMessage = ex.Message;
                _logger.Error($"AI enhancement failed: {ex.Message}", ex);
            }
            finally
            {
                report.EndTime = DateTime.UtcNow;
            }

            return report;
        }

        /// <summary>
        /// Generates descriptions for a batch of entities
        /// </summary>
        /// <param name="entities">Entities to enhance</param>
        /// <param name="theme">Visual theme for context</param>
        /// <returns>Number of successfully enhanced entities</returns>
        public int EnhanceEntityDescriptions(List<Entity> entities, VisualTheme theme)
        {
            if (entities == null || !entities.Any())
                return 0;

            if (!_aiContentGenerator.IsAvailable())
                return 0;

            int enhancedCount = 0;

            foreach (var entity in entities)
            {
                try
                {
                    var description = _aiContentGenerator.GenerateItemDescription(entity.Type, theme);
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        entity.Properties["Description"] = description;
                        entity.Properties["AIGenerated"] = true;
                        enhancedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to generate description for {entity.Type}: {ex.Message}");
                }
            }

            return enhancedCount;
        }

        /// <summary>
        /// Generates dialogue for interactive entities
        /// </summary>
        /// <param name="entities">Entities to enhance with dialogue</param>
        /// <param name="theme">Visual theme for context</param>
        /// <returns>Number of entities with generated dialogue</returns>
        public int GenerateEntityDialogue(List<Entity> entities, VisualTheme theme)
        {
            if (entities == null || !entities.Any())
                return 0;

            if (!_aiContentGenerator.IsAvailable())
                return 0;

            int dialogueCount = 0;
            var interactiveEntities = entities.Where(IsInteractiveEntity).ToList();

            foreach (var entity in interactiveEntities)
            {
                try
                {
                    var lineCount = GetDialogueLineCount(entity.Type);
                    var dialogue = _aiContentGenerator.GenerateNPCDialogue(entity.Type, lineCount);
                    
                    if (dialogue != null && dialogue.Length > 0)
                    {
                        entity.Properties["Dialogue"] = dialogue;
                        entity.Properties["DialogueCount"] = dialogue.Length;
                        entity.Properties["AIGeneratedDialogue"] = true;
                        dialogueCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to generate dialogue for {entity.Type}: {ex.Message}");
                }
            }

            return dialogueCount;
        }

        /// <summary>
        /// Generates a creative name for a level
        /// </summary>
        /// <param name="level">Level to name</param>
        /// <param name="theme">Visual theme for context</param>
        /// <returns>Generated name or null if generation fails</returns>
        public string GenerateLevelName(Level level, VisualTheme theme)
        {
            if (level == null || theme == null)
                return null;

            if (!_aiContentGenerator.IsAvailable())
                return null;

            try
            {
                return _aiContentGenerator.GenerateLevelName(level, theme);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to generate level name: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if AI content generation is available
        /// </summary>
        /// <returns>True if AI services are available</returns>
        public bool IsAvailable()
        {
            return _aiContentGenerator.IsAvailable();
        }

        private void EnhanceLevelName(Level level, VisualTheme theme, AIEnhancementReport report)
        {
            try
            {
                var aiName = _aiContentGenerator.GenerateLevelName(level, theme);
                if (!string.IsNullOrWhiteSpace(aiName))
                {
                    var originalName = level.Name;
                    level.Name = aiName;
                    level.Metadata["OriginalName"] = originalName;
                    level.Metadata["AIGeneratedName"] = true;
                    report.LevelNameGenerated = true;
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to generate AI level name: {ex.Message}");
                report.Warnings.Add($"Level name generation failed: {ex.Message}");
            }
        }

        private void EnhanceEntities(List<Entity> entities, VisualTheme theme, AIEnhancementReport report)
        {
            foreach (var entity in entities)
            {
                try
                {
                    // Generate description
                    var description = _aiContentGenerator.GenerateItemDescription(entity.Type, theme);
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        entity.Properties["Description"] = description;
                        entity.Properties["AIGenerated"] = true;
                        report.EnhancedEntities++;
                    }

                    // Generate dialogue for interactive entities
                    if (IsInteractiveEntity(entity))
                    {
                        var lineCount = GetDialogueLineCount(entity.Type);
                        var dialogue = _aiContentGenerator.GenerateNPCDialogue(entity.Type, lineCount);
                        
                        if (dialogue != null && dialogue.Length > 0)
                        {
                            entity.Properties["Dialogue"] = dialogue;
                            entity.Properties["DialogueCount"] = dialogue.Length;
                            entity.Properties["AIGeneratedDialogue"] = true;
                            report.EntitiesWithDialogue++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to enhance entity {entity.Type}: {ex.Message}");
                    report.Warnings.Add($"Entity {entity.Type} enhancement failed: {ex.Message}");
                }
            }
        }

        private bool IsInteractiveEntity(Entity entity)
        {
            return entity.Type == EntityType.Enemy || 
                   entity.Type == EntityType.PowerUp || 
                   entity.Type == EntityType.Checkpoint;
        }

        private int GetDialogueLineCount(EntityType entityType)
        {
            return entityType switch
            {
                EntityType.Enemy => 3,
                EntityType.PowerUp => 2,
                EntityType.Checkpoint => 2,
                _ => 1
            };
        }
    }

    /// <summary>
    /// Report of AI enhancement operations
    /// </summary>
    public class AIEnhancementReport
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public bool IsAIAvailable { get; set; }
        public string ErrorMessage { get; set; }
        public int TotalEntities { get; set; }
        public int EnhancedEntities { get; set; }
        public int EntitiesWithDialogue { get; set; }
        public bool LevelNameGenerated { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();

        public TimeSpan Duration => EndTime - StartTime;
        public double EnhancementRatio => TotalEntities > 0 ? (double)EnhancedEntities / TotalEntities : 0.0;
    }
}