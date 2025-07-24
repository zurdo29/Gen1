using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// AI-powered content generator with fallback support
    /// </summary>
    public class AIContentGenerator : IAIContentGenerator
    {
        private readonly HttpClient _httpClient;
        private readonly AIServiceConfig _config;
        private readonly ILogger _logger;
        private bool _isServiceAvailable;

        public AIContentGenerator(HttpClient httpClient, AIServiceConfig config, ILogger logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isServiceAvailable = true;
        }

        public string GenerateItemDescription(EntityType type, VisualTheme theme)
        {
            try
            {
                if (!IsAvailable())
                {
                    return GetFallbackItemDescription(type, theme);
                }

                var prompt = $"Generate a brief description for a {type} item in a {theme.Name} themed game. Keep it under 50 words.";
                var result = CallAIServiceAsync(prompt).Result;
                
                if (string.IsNullOrWhiteSpace(result))
                {
                    _logger.Warning("AI service returned empty result for item description");
                    return GetFallbackItemDescription(type, theme);
                }

                return result.Trim();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error generating item description: {ex.Message}", ex);
                _isServiceAvailable = false;
                return GetFallbackItemDescription(type, theme);
            }
        }

        public string[] GenerateNPCDialogue(EntityType type, int lineCount)
        {
            try
            {
                if (!IsAvailable())
                {
                    return GetFallbackNPCDialogue(type, lineCount);
                }

                var prompt = $"Generate {lineCount} dialogue lines for a {type} NPC. Each line should be short and game-appropriate.";
                var result = CallAIServiceAsync(prompt).Result;
                
                if (string.IsNullOrWhiteSpace(result))
                {
                    _logger.Warning("AI service returned empty result for NPC dialogue");
                    return GetFallbackNPCDialogue(type, lineCount);
                }

                var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < lineCount)
                {
                    _logger.Warning($"AI service returned fewer lines than requested ({lines.Length} vs {lineCount})");
                    return GetFallbackNPCDialogue(type, lineCount);
                }

                return lines;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error generating NPC dialogue: {ex.Message}", ex);
                _isServiceAvailable = false;
                return GetFallbackNPCDialogue(type, lineCount);
            }
        }

        public string GenerateLevelName(Level level, VisualTheme theme)
        {
            try
            {
                if (!IsAvailable())
                {
                    return GetFallbackLevelName(level, theme);
                }

                var enemyCount = level.Entities?.Count ?? 0;
                var prompt = $"Generate a creative name for a {theme.Name} themed level with {enemyCount} enemies. Keep it under 30 characters.";
                var result = CallAIServiceAsync(prompt).Result;
                
                if (string.IsNullOrWhiteSpace(result))
                {
                    _logger.Warning("AI service returned empty result for level name");
                    return GetFallbackLevelName(level, theme);
                }

                return result.Trim();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error generating level name: {ex.Message}", ex);
                _isServiceAvailable = false;
                return GetFallbackLevelName(level, theme);
            }
        }

        public bool IsAvailable()
        {
            return _isServiceAvailable && _config.IsEnabled && !string.IsNullOrWhiteSpace(_config.ApiEndpoint);
        }

        private async Task<string> CallAIServiceAsync(string prompt)
        {
            if (!IsAvailable())
            {
                throw new InvalidOperationException("AI service is not available");
            }

            var requestBody = new
            {
                prompt = prompt,
                max_tokens = _config.MaxTokens,
                temperature = _config.Temperature
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (!string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);
            }

            var response = await _httpClient.PostAsync(_config.ApiEndpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"AI service returned error: {response.StatusCode} - {response.ReasonPhrase}");
                _isServiceAvailable = false;
                throw new HttpRequestException($"AI service error: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            if (responseObj.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString();
            }
            else if (responseObj.TryGetProperty("choices", out var choicesElement) && 
                     choicesElement.GetArrayLength() > 0)
            {
                var firstChoice = choicesElement[0];
                if (firstChoice.TryGetProperty("text", out var choiceText))
                {
                    return choiceText.GetString();
                }
            }

            throw new InvalidOperationException("Unexpected AI service response format");
        }

        private string GetFallbackItemDescription(EntityType type, VisualTheme theme)
        {
            var descriptions = new Dictionary<EntityType, string[]>
            {
                [EntityType.Enemy] = new[] { "A dangerous foe lurking in the shadows", "An aggressive creature blocking your path", "A hostile entity guarding the area" },
                [EntityType.Item] = new[] { "A useful item waiting to be collected", "A valuable object gleaming in the light", "An essential tool for your journey" },
                [EntityType.PowerUp] = new[] { "A power enhancement glowing with energy", "A boost that will aid your quest", "A magical enhancement radiating power" },
                [EntityType.Checkpoint] = new[] { "A safe haven to rest and save progress", "A beacon marking your journey's milestone", "A sanctuary offering respite" }
            };

            if (descriptions.TryGetValue(type, out var options))
            {
                var random = new Random();
                return options[random.Next(options.Length)];
            }

            return $"A {type.ToString().ToLower()} in the {theme.Name} world";
        }

        private string[] GetFallbackNPCDialogue(EntityType type, int lineCount)
        {
            var dialogues = new Dictionary<EntityType, string[]>
            {
                [EntityType.Enemy] = new[] { "You shall not pass!", "Prepare for battle!", "This is my domain!", "Turn back now!" },
                [EntityType.Item] = new[] { "Take me with you!", "I might be useful!", "Don't leave me behind!" },
                [EntityType.PowerUp] = new[] { "Use my power wisely!", "I will make you stronger!", "Channel my energy!" },
                [EntityType.Checkpoint] = new[] { "Rest here, traveler", "Your progress is saved", "Take a moment to recover" }
            };

            if (dialogues.TryGetValue(type, out var options))
            {
                var result = new string[lineCount];
                var random = new Random();
                
                for (int i = 0; i < lineCount; i++)
                {
                    result[i] = options[random.Next(options.Length)];
                }
                
                return result;
            }

            var fallback = new string[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                fallback[i] = $"Generic {type} dialogue line {i + 1}";
            }
            
            return fallback;
        }

        private string GetFallbackLevelName(Level level, VisualTheme theme)
        {
            var prefixes = new[] { "The", "Dark", "Ancient", "Mysterious", "Hidden", "Lost", "Forgotten" };
            var suffixes = new[] { "Chamber", "Cavern", "Realm", "Domain", "Sanctum", "Depths", "Maze" };
            
            var random = new Random();
            var prefix = prefixes[random.Next(prefixes.Length)];
            var suffix = suffixes[random.Next(suffixes.Length)];
            
            return $"{prefix} {theme.Name} {suffix}";
        }
    }
}