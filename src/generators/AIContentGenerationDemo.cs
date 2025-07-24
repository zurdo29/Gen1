using System;
using System.Collections.Generic;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Demonstration class showing AI content generation capabilities
    /// </summary>
    public class AIContentGenerationDemo
    {
        private readonly AIContentService _aiContentService;
        private readonly ILogger _logger;

        public AIContentGenerationDemo(AIContentService aiContentService, ILogger logger)
        {
            _aiContentService = aiContentService ?? throw new ArgumentNullException(nameof(aiContentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Demonstrates AI content generation for a sample level
        /// </summary>
        /// <returns>Demo results</returns>
        public AIContentDemoResults RunDemo()
        {
            var results = new AIContentDemoResults
            {
                StartTime = DateTime.UtcNow,
                IsAIAvailable = _aiContentService.IsAvailable()
            };

            _logger.Info("Starting AI content generation demo");

            try
            {
                // Create a sample level
                var sampleLevel = CreateSampleLevel();
                var sampleTheme = CreateSampleTheme();

                results.SampleLevel = sampleLevel;
                results.SampleTheme = sampleTheme;

                if (!_aiContentService.IsAvailable())
                {
                    _logger.Warning("AI services not available, demo will show fallback behavior");
                    results.Messages.Add("AI services not available - using fallback content");
                }

                // Demonstrate level name generation
                DemonstrateNameGeneration(sampleLevel, sampleTheme, results);

                // Demonstrate entity description generation
                DemonstrateDescriptionGeneration(sampleLevel.Entities, sampleTheme, results);

                // Demonstrate dialogue generation
                DemonstrateDialogueGeneration(sampleLevel.Entities, sampleTheme, results);

                // Demonstrate full level enhancement
                DemonstrateFullEnhancement(sampleLevel, sampleTheme, results);

                results.Success = true;
                _logger.Info("AI content generation demo completed successfully");
            }
            catch (Exception ex)
            {
                results.Success = false;
                results.ErrorMessage = ex.Message;
                _logger.Error($"AI content generation demo failed: {ex.Message}", ex);
            }
            finally
            {
                results.EndTime = DateTime.UtcNow;
            }

            return results;
        }

        private Level CreateSampleLevel()
        {
            var terrain = new TileMap(20, 20);
            
            // Create sample entities
            var entities = new List<Entity>
            {
                new EnemyEntity { Position = new System.Numerics.Vector2(5, 5) },
                new EnemyEntity { Position = new System.Numerics.Vector2(15, 15) },
                new PowerUpEntity { Position = new System.Numerics.Vector2(10, 10) },
                new ItemEntity { Position = new System.Numerics.Vector2(8, 12) },
                new CheckpointEntity { Position = new System.Numerics.Vector2(3, 18) }
            };

            return new Level
            {
                Terrain = terrain,
                Entities = entities,
                Name = "Sample Demo Level",
                Metadata = new Dictionary<string, object>
                {
                    ["CreatedAt"] = DateTime.UtcNow,
                    ["Purpose"] = "AI Content Generation Demo"
                }
            };
        }

        private VisualTheme CreateSampleTheme()
        {
            return new VisualTheme
            {
                Name = "Mystical Forest",
                TileSprites = new Dictionary<TileType, string>(),
                EntitySprites = new Dictionary<EntityType, string>(),
                Colors = new ColorPalette()
            };
        }

        private void DemonstrateNameGeneration(Level level, VisualTheme theme, AIContentDemoResults results)
        {
            _logger.Info("Demonstrating level name generation");
            
            var originalName = level.Name;
            var generatedName = _aiContentService.GenerateLevelName(level, theme);
            
            results.OriginalLevelName = originalName;
            results.GeneratedLevelName = generatedName ?? "No name generated";
            results.NameGenerationSuccessful = !string.IsNullOrWhiteSpace(generatedName);
            
            if (results.NameGenerationSuccessful)
            {
                results.Messages.Add($"Generated level name: '{generatedName}' (was: '{originalName}')");
            }
            else
            {
                results.Messages.Add($"Level name generation failed, keeping original: '{originalName}'");
            }
        }

        private void DemonstrateDescriptionGeneration(List<Entity> entities, VisualTheme theme, AIContentDemoResults results)
        {
            _logger.Info("Demonstrating entity description generation");
            
            var enhancedCount = _aiContentService.EnhanceEntityDescriptions(entities, theme);
            results.EntitiesWithDescriptions = enhancedCount;
            results.TotalEntities = entities.Count;
            
            results.Messages.Add($"Enhanced {enhancedCount} out of {entities.Count} entities with descriptions");
            
            // Show examples of generated descriptions
            foreach (var entity in entities.Where(e => e.HasAIContent()))
            {
                var description = entity.GetAIDescription();
                if (!string.IsNullOrWhiteSpace(description))
                {
                    results.SampleDescriptions.Add($"{entity.Type}: {description}");
                }
            }
        }

        private void DemonstrateDialogueGeneration(List<Entity> entities, VisualTheme theme, AIContentDemoResults results)
        {
            _logger.Info("Demonstrating dialogue generation");
            
            var dialogueCount = _aiContentService.GenerateEntityDialogue(entities, theme);
            results.EntitiesWithDialogue = dialogueCount;
            
            results.Messages.Add($"Generated dialogue for {dialogueCount} entities");
            
            // Show examples of generated dialogue
            foreach (var entity in entities.Where(e => e.GetAIDialogue() != null))
            {
                var dialogue = entity.GetAIDialogue();
                results.SampleDialogue.Add($"{entity.Type}: {string.Join(" | ", dialogue)}");
            }
        }

        private void DemonstrateFullEnhancement(Level level, VisualTheme theme, AIContentDemoResults results)
        {
            _logger.Info("Demonstrating full level enhancement");
            
            var enhancementReport = _aiContentService.EnhanceLevel(level, theme);
            results.EnhancementReport = enhancementReport;
            
            results.Messages.Add($"Full enhancement completed in {enhancementReport.Duration.TotalMilliseconds:F0}ms");
            results.Messages.Add($"Enhancement ratio: {enhancementReport.EnhancementRatio:P1}");
            
            if (enhancementReport.Warnings.Any())
            {
                results.Messages.Add($"Warnings: {string.Join(", ", enhancementReport.Warnings)}");
            }
        }
    }

    /// <summary>
    /// Results from the AI content generation demo
    /// </summary>
    public class AIContentDemoResults
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public bool IsAIAvailable { get; set; }
        public string ErrorMessage { get; set; }
        public Level SampleLevel { get; set; }
        public VisualTheme SampleTheme { get; set; }
        public string OriginalLevelName { get; set; }
        public string GeneratedLevelName { get; set; }
        public bool NameGenerationSuccessful { get; set; }
        public int TotalEntities { get; set; }
        public int EntitiesWithDescriptions { get; set; }
        public int EntitiesWithDialogue { get; set; }
        public List<string> SampleDescriptions { get; set; } = new List<string>();
        public List<string> SampleDialogue { get; set; } = new List<string>();
        public List<string> Messages { get; set; } = new List<string>();
        public AIEnhancementReport EnhancementReport { get; set; }

        public TimeSpan Duration => EndTime - StartTime;
        public double DescriptionRatio => TotalEntities > 0 ? (double)EntitiesWithDescriptions / TotalEntities : 0.0;
    }
}