using FluentValidation;
using Microsoft.Extensions.Options;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Configuration;

namespace ProceduralMiniGameGenerator.WebAPI.Validators
{
    /// <summary>
    /// Validator for GenerationConfig
    /// </summary>
    public class GenerationConfigValidator : AbstractValidator<GenerationConfig>
    {
        public GenerationConfigValidator(IOptions<GenerationConfiguration> config)
        {
            var settings = config.Value;

            RuleFor(x => x.Width)
                .InclusiveBetween(settings.MinLevelWidth, settings.MaxLevelWidth)
                .WithMessage($"Width must be between {settings.MinLevelWidth} and {settings.MaxLevelWidth}");

            RuleFor(x => x.Height)
                .InclusiveBetween(settings.MinLevelHeight, settings.MaxLevelHeight)
                .WithMessage($"Height must be between {settings.MinLevelHeight} and {settings.MaxLevelHeight}");

            RuleFor(x => x.Seed)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Seed must be non-negative");

            RuleFor(x => x.GenerationAlgorithm)
                .NotEmpty()
                .WithMessage("Generation algorithm is required")
                .Must(algorithm => settings.SupportedAlgorithms.Contains(algorithm))
                .WithMessage($"Generation algorithm must be one of: {string.Join(", ", settings.SupportedAlgorithms)}");

            RuleFor(x => x.Entities)
                .Must(entities => entities == null || entities.Sum(e => e.Count) <= settings.MaxEntitiesPerLevel)
                .WithMessage($"Total entity count cannot exceed {settings.MaxEntitiesPerLevel}");

            RuleForEach(x => x.Entities)
                .SetValidator(new EntityConfigValidator());

            RuleFor(x => x.VisualTheme)
                .SetValidator(new VisualThemeConfigValidator(settings))
                .When(x => x.VisualTheme != null);

            RuleFor(x => x.Gameplay)
                .SetValidator(new GameplayConfigValidator(settings))
                .When(x => x.Gameplay != null);
        }
    }

    /// <summary>
    /// Validator for EntityConfig
    /// </summary>
    public class EntityConfigValidator : AbstractValidator<EntityConfig>
    {
        public EntityConfigValidator()
        {
            RuleFor(x => x.Type)
                .NotEmpty()
                .WithMessage("Entity type is required");

            RuleFor(x => x.Count)
                .GreaterThan(0)
                .WithMessage("Entity count must be greater than 0");

            RuleFor(x => x.MinDistance)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum distance must be non-negative");

            RuleFor(x => x.MaxDistanceFromPlayer)
                .GreaterThan(0)
                .WithMessage("Maximum distance from player must be greater than 0");

            RuleFor(x => x.MaxDistanceFromPlayer)
                .GreaterThan(x => x.MinDistance)
                .WithMessage("Maximum distance must be greater than minimum distance");

            RuleFor(x => x.PlacementStrategy)
                .NotEmpty()
                .WithMessage("Placement strategy is required");
        }
    }

    /// <summary>
    /// Validator for VisualThemeConfig
    /// </summary>
    public class VisualThemeConfigValidator : AbstractValidator<VisualThemeConfig>
    {
        public VisualThemeConfigValidator(GenerationConfiguration settings)
        {
            RuleFor(x => x.ThemeName)
                .NotEmpty()
                .WithMessage("Theme name is required")
                .Must(theme => settings.SupportedThemes.Contains(theme))
                .WithMessage($"Theme must be one of: {string.Join(", ", settings.SupportedThemes)}");
        }
    }

    /// <summary>
    /// Validator for GameplayConfig
    /// </summary>
    public class GameplayConfigValidator : AbstractValidator<GameplayConfig>
    {
        public GameplayConfigValidator(GenerationConfiguration settings)
        {
            RuleFor(x => x.Difficulty)
                .Must(difficulty => string.IsNullOrEmpty(difficulty) || settings.SupportedDifficulties.Contains(difficulty))
                .WithMessage($"Difficulty must be one of: {string.Join(", ", settings.SupportedDifficulties)}")
                .When(x => !string.IsNullOrEmpty(x.Difficulty));

            RuleFor(x => x.PlayerSpeed)
                .InclusiveBetween(settings.MinPlayerSpeed, settings.MaxPlayerSpeed)
                .WithMessage($"Player speed must be between {settings.MinPlayerSpeed} and {settings.MaxPlayerSpeed}")
                .When(x => x.PlayerSpeed > 0f);

            RuleFor(x => x.TimeLimit)
                .InclusiveBetween(1, settings.MaxTimeLimit)
                .WithMessage($"Time limit must be between 1 and {settings.MaxTimeLimit} seconds")
                .When(x => x.TimeLimit > 0f);
        }
    }
}