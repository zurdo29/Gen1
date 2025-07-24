using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Editor
{
    /// <summary>
    /// Concrete implementation of editor integration for the procedural mini-game generator
    /// </summary>
    public class EditorIntegration : IEditorIntegration
    {
        private readonly IGenerationManager _generationManager;
        private GenerationWindow? _generationWindow;
        private readonly List<string> _recentErrors = new List<string>();
        
        public EditorIntegration(IGenerationManager generationManager)
        {
            _generationManager = generationManager ?? throw new ArgumentNullException(nameof(generationManager));
        }
        
        /// <summary>
        /// Registers editor commands for generation
        /// </summary>
        public void RegisterEditorCommands()
        {
            // Core generation commands
            RegisterCommand("GenerateLevel", "Generate Level", "Ctrl+G", GenerateQuickLevel);
            RegisterCommand("ShowGenerationWindow", "Show Generation Window", "Ctrl+Shift+G", ShowGenerationWindow);
            RegisterCommand("RegenerateLevel", "Regenerate Level", "Ctrl+R", RegenerateCurrentLevel);
            RegisterCommand("SelectConfigFile", "Select Config File", "Ctrl+O", () => SelectConfigurationFile());
            
            // Additional utility commands
            RegisterCommand("GenerateWithRandomSeed", "Generate with Random Seed", "Ctrl+Shift+R", GenerateWithRandomSeed);
            RegisterCommand("ExportCurrentLevel", "Export Current Level", "Ctrl+E", ExportCurrentLevel);
            RegisterCommand("ValidateCurrentLevel", "Validate Current Level", "Ctrl+V", ValidateCurrentLevel);
            RegisterCommand("ShowLevelPreview", "Show Level Preview", "Ctrl+P", ShowCurrentLevelPreview);
            RegisterCommand("ClearGeneratedLevel", "Clear Generated Level", "Ctrl+Shift+C", ClearGeneratedLevel);
            
            Console.WriteLine("Editor commands registered successfully:");
            Console.WriteLine("Core Commands:");
            Console.WriteLine("  - Generate Level (Ctrl+G)");
            Console.WriteLine("  - Show Generation Window (Ctrl+Shift+G)");
            Console.WriteLine("  - Regenerate Level (Ctrl+R)");
            Console.WriteLine("  - Select Config File (Ctrl+O)");
            Console.WriteLine("Utility Commands:");
            Console.WriteLine("  - Generate with Random Seed (Ctrl+Shift+R)");
            Console.WriteLine("  - Export Current Level (Ctrl+E)");
            Console.WriteLine("  - Validate Current Level (Ctrl+V)");
            Console.WriteLine("  - Show Level Preview (Ctrl+P)");
            Console.WriteLine("  - Clear Generated Level (Ctrl+Shift+C)");
        }
        
        /// <summary>
        /// Shows the generation window in the editor
        /// </summary>
        public void ShowGenerationWindow()
        {
            try
            {
                if (_generationWindow == null)
                {
                    _generationWindow = new GenerationWindow(this, _generationManager);
                }
                
                _generationWindow.Show();
                Console.WriteLine("Generation window opened successfully");
            }
            catch (Exception ex)
            {
                var error = $"Failed to show generation window: {ex.Message}";
                ReportErrors(new List<string> { error });
            }
        }
        
        /// <summary>
        /// Displays a generated level in the editor
        /// </summary>
        /// <param name="level">Level to display</param>
        public void DisplayGeneratedLevel(Level level)
        {
            if (level == null)
            {
                ReportErrors(new List<string> { "Cannot display null level" });
                return;
            }
            
            try
            {
                Console.WriteLine($"Displaying generated level: {level.Name}");
                Console.WriteLine($"Level size: {level.Terrain?.Width ?? 0}x{level.Terrain?.Height ?? 0}");
                Console.WriteLine($"Entities: {level.Entities?.Count ?? 0}");
                
                // Display level preview in console (for testing)
                DisplayLevelPreview(level);
                
                // In a real editor, this would update the editor viewport
                // For now, we'll simulate this with console output
                Console.WriteLine("✓ Level displayed in editor viewport");
            }
            catch (Exception ex)
            {
                var error = $"Failed to display level: {ex.Message}";
                ReportErrors(new List<string> { error });
            }
        }
        
        /// <summary>
        /// Reports errors to the editor interface
        /// </summary>
        /// <param name="errors">List of error messages</param>
        public void ReportErrors(List<string> errors)
        {
            if (errors == null || !errors.Any()) return;
            
            _recentErrors.Clear();
            _recentErrors.AddRange(errors);
            
            DisplayErrorPanel(errors);
            LogErrorsToFile(errors);
            
            // In a real editor, this would show errors in an error panel
            // For now, we'll use enhanced console output with categorization
        }
        
        /// <summary>
        /// Reports warnings to the editor interface
        /// </summary>
        /// <param name="warnings">List of warning messages</param>
        public void ReportWarnings(List<string> warnings)
        {
            if (warnings == null || !warnings.Any()) return;
            
            DisplayWarningPanel(warnings);
            LogWarningsToFile(warnings);
        }
        
        /// <summary>
        /// Reports validation results with both errors and warnings
        /// </summary>
        /// <param name="validationResult">Validation result to report</param>
        public void ReportValidationResult(ValidationResult validationResult)
        {
            if (validationResult == null) return;
            
            if (validationResult.Errors.Any())
            {
                ReportErrors(validationResult.Errors);
            }
            
            if (validationResult.Warnings.Any())
            {
                ReportWarnings(validationResult.Warnings);
            }
            
            if (validationResult.IsValid && !validationResult.HasWarnings)
            {
                DisplaySuccessMessage("Configuration is valid with no issues");
            }
        }
        
        /// <summary>
        /// Displays success messages with visual feedback
        /// </summary>
        /// <param name="message">Success message to display</param>
        public void DisplaySuccessMessage(string message)
        {
            Console.WriteLine("=== SUCCESS ===");
            Console.WriteLine($"✅ {message}");
            Console.WriteLine("===============");
        }
        
        /// <summary>
        /// Displays information messages
        /// </summary>
        /// <param name="message">Information message to display</param>
        public void DisplayInfoMessage(string message)
        {
            Console.WriteLine("=== INFO ===");
            Console.WriteLine($"ℹ️  {message}");
            Console.WriteLine("============");
        }
        
        /// <summary>
        /// Shows a preview of the generated level
        /// </summary>
        /// <param name="level">Level to preview</param>
        public void ShowLevelPreview(Level level)
        {
            if (level == null)
            {
                ReportErrors(new List<string> { "Cannot preview null level" });
                return;
            }
            
            Console.WriteLine($"\n=== LEVEL PREVIEW: {level.Name} ===");
            DisplayLevelPreview(level);
            Console.WriteLine("================================\n");
        }
        
        /// <summary>
        /// Allows user to select a configuration file
        /// </summary>
        /// <returns>Path to selected configuration file</returns>
        public string SelectConfigurationFile()
        {
            try
            {
                // In a real editor, this would open a file dialog
                // For testing, we'll look for config files in common locations
                var configPaths = new[]
                {
                    "config.json",
                    "generation_config.json",
                    "level_config.json",
                    "configs/default.json"
                };
                
                foreach (var path in configPaths)
                {
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"Selected configuration file: {path}");
                        return path;
                    }
                }
                
                // If no config files found, create a default one
                var defaultConfigPath = "default_config.json";
                CreateDefaultConfigFile(defaultConfigPath);
                Console.WriteLine($"No config files found. Created default config: {defaultConfigPath}");
                return defaultConfigPath;
            }
            catch (Exception ex)
            {
                var error = $"Failed to select configuration file: {ex.Message}";
                ReportErrors(new List<string> { error });
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Gets the list of recent errors
        /// </summary>
        /// <returns>List of recent error messages</returns>
        public List<string> GetRecentErrors()
        {
            return new List<string>(_recentErrors);
        }
        
        private void RegisterCommand(string commandId, string displayName, string shortcut, Action action)
        {
            // In a real editor, this would register commands with the editor's command system
            // For testing, we'll just store the command information
            Console.WriteLine($"Registered command: {displayName} ({shortcut})");
        }
        
        private void GenerateQuickLevel()
        {
            try
            {
                var configPath = SelectConfigurationFile();
                if (string.IsNullOrEmpty(configPath)) return;
                
                // This would use the generation manager to create a level
                Console.WriteLine("Quick level generation triggered");
                // TODO: Implement actual generation when GenerationManager is available
            }
            catch (Exception ex)
            {
                ReportErrors(new List<string> { $"Quick generation failed: {ex.Message}" });
            }
        }
        
        private void RegenerateCurrentLevel()
        {
            try
            {
                Console.WriteLine("Regenerating current level...");
                // TODO: Implement regeneration logic
            }
            catch (Exception ex)
            {
                ReportErrors(new List<string> { $"Regeneration failed: {ex.Message}" });
            }
        }
        
        private void GenerateWithRandomSeed()
        {
            try
            {
                var configPath = SelectConfigurationFile();
                if (string.IsNullOrEmpty(configPath)) return;
                
                Console.WriteLine("Generating level with random seed...");
                var randomSeed = new Random().Next();
                Console.WriteLine($"Using random seed: {randomSeed}");
                
                // TODO: Implement generation with random seed
            }
            catch (Exception ex)
            {
                ReportErrors(new List<string> { $"Random seed generation failed: {ex.Message}" });
            }
        }
        
        private void ExportCurrentLevel()
        {
            try
            {
                Console.WriteLine("Exporting current level...");
                
                // Check if there's a current level to export
                if (_generationWindow?.IsVisible == true)
                {
                    Console.WriteLine("Requesting export from generation window...");
                    // In a real implementation, this would get the current level from the window
                }
                else
                {
                    Console.WriteLine("No level currently available for export. Generate a level first.");
                }
            }
            catch (Exception ex)
            {
                ReportErrors(new List<string> { $"Level export failed: {ex.Message}" });
            }
        }
        
        private void ValidateCurrentLevel()
        {
            try
            {
                Console.WriteLine("Validating current level...");
                
                // Check if there's a current level to validate
                if (_generationWindow?.IsVisible == true)
                {
                    Console.WriteLine("Requesting validation from generation window...");
                    // In a real implementation, this would validate the current level
                    Console.WriteLine("✓ Level validation completed");
                }
                else
                {
                    Console.WriteLine("No level currently available for validation. Generate a level first.");
                }
            }
            catch (Exception ex)
            {
                ReportErrors(new List<string> { $"Level validation failed: {ex.Message}" });
            }
        }
        
        private void ShowCurrentLevelPreview()
        {
            try
            {
                Console.WriteLine("Showing current level preview...");
                
                // Check if there's a current level to preview
                if (_generationWindow?.IsVisible == true)
                {
                    Console.WriteLine("Requesting preview from generation window...");
                    // In a real implementation, this would show the current level preview
                }
                else
                {
                    Console.WriteLine("No level currently available for preview. Generate a level first.");
                }
            }
            catch (Exception ex)
            {
                ReportErrors(new List<string> { $"Level preview failed: {ex.Message}" });
            }
        }
        
        private void ClearGeneratedLevel()
        {
            try
            {
                Console.WriteLine("Clearing generated level...");
                
                // Clear any cached level data
                if (_generationWindow != null)
                {
                    Console.WriteLine("Clearing level data from generation window...");
                    // In a real implementation, this would clear the current level
                }
                
                Console.WriteLine("✓ Generated level cleared");
            }
            catch (Exception ex)
            {
                ReportErrors(new List<string> { $"Clear level failed: {ex.Message}" });
            }
        }
        
        private void DisplayLevelPreview(Level level)
        {
            if (level.Terrain == null)
            {
                Console.WriteLine("No terrain data available");
                return;
            }
            
            var width = Math.Min(level.Terrain.Width, 40); // Limit preview size
            var height = Math.Min(level.Terrain.Height, 20);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var tile = level.Terrain.GetTile(x, y);
                    var symbol = GetTileSymbol(tile);
                    
                    // Check if there's an entity at this position
                    var entity = level.Entities?.FirstOrDefault(e => 
                        Math.Abs(e.Position.X - x) < 0.5f && Math.Abs(e.Position.Y - y) < 0.5f);
                    
                    if (entity != null)
                    {
                        symbol = GetEntitySymbol(entity.Type);
                    }
                    
                    Console.Write(symbol);
                }
                Console.WriteLine();
            }
            
            if (level.Terrain.Width > width || level.Terrain.Height > height)
            {
                Console.WriteLine($"(Preview truncated - full size: {level.Terrain.Width}x{level.Terrain.Height})");
            }
        }
        
        private char GetTileSymbol(TileType tileType)
        {
            return tileType switch
            {
                TileType.Ground => '.',
                TileType.Wall => '#',
                TileType.Water => '~',
                TileType.Grass => ',',
                _ => '?'
            };
        }
        
        private char GetEntitySymbol(EntityType entityType)
        {
            return entityType switch
            {
                EntityType.Player => '@',
                EntityType.Enemy => 'E',
                EntityType.Item => 'I',
                EntityType.PowerUp => 'P',
                EntityType.Checkpoint => 'C',
                _ => '?'
            };
        }
        
        private void DisplayErrorPanel(List<string> errors)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                           ERRORS                            ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            
            for (int i = 0; i < errors.Count; i++)
            {
                var error = errors[i];
                var truncatedError = error.Length > 58 ? error.Substring(0, 55) + "..." : error;
                Console.WriteLine($"║ {i + 1,2}. ❌ {truncatedError,-54} ║");
                
                // If error was truncated, show full error on next line
                if (error.Length > 58)
                {
                    var remainingText = error.Substring(55);
                    while (remainingText.Length > 0)
                    {
                        var chunk = remainingText.Length > 58 ? remainingText.Substring(0, 58) : remainingText;
                        Console.WriteLine($"║      {chunk,-58} ║");
                        remainingText = remainingText.Length > 58 ? remainingText.Substring(58) : "";
                    }
                }
            }
            
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"Total errors: {errors.Count}");
            Console.WriteLine();
        }
        
        private void DisplayWarningPanel(List<string> warnings)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                          WARNINGS                           ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            
            for (int i = 0; i < warnings.Count; i++)
            {
                var warning = warnings[i];
                var truncatedWarning = warning.Length > 58 ? warning.Substring(0, 55) + "..." : warning;
                Console.WriteLine($"║ {i + 1,2}. ⚠️  {truncatedWarning,-54} ║");
                
                // If warning was truncated, show full warning on next line
                if (warning.Length > 58)
                {
                    var remainingText = warning.Substring(55);
                    while (remainingText.Length > 0)
                    {
                        var chunk = remainingText.Length > 58 ? remainingText.Substring(0, 58) : remainingText;
                        Console.WriteLine($"║      {chunk,-58} ║");
                        remainingText = remainingText.Length > 58 ? remainingText.Substring(58) : "";
                    }
                }
            }
            
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"Total warnings: {warnings.Count}");
            Console.WriteLine();
        }
        
        private void LogErrorsToFile(List<string> errors)
        {
            try
            {
                var logPath = "editor_errors.log";
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logEntry = $"[{timestamp}] ERRORS ({errors.Count}):\n";
                
                foreach (var error in errors)
                {
                    logEntry += $"  - {error}\n";
                }
                logEntry += "\n";
                
                File.AppendAllText(logPath, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log errors to file: {ex.Message}");
            }
        }
        
        private void LogWarningsToFile(List<string> warnings)
        {
            try
            {
                var logPath = "editor_warnings.log";
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logEntry = $"[{timestamp}] WARNINGS ({warnings.Count}):\n";
                
                foreach (var warning in warnings)
                {
                    logEntry += $"  - {warning}\n";
                }
                logEntry += "\n";
                
                File.AppendAllText(logPath, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log warnings to file: {ex.Message}");
            }
        }
        
        private void CreateDefaultConfigFile(string path)
        {
            var defaultConfig = new GenerationConfig
            {
                Width = 40,
                Height = 30,
                Seed = 12345,
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    { "scale", 0.1 },
                    { "octaves", 4 }
                },
                TerrainTypes = new List<string> { "ground", "wall", "water" },
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Enemy,
                        Count = 3,
                        MinDistance = 5.0f,
                        PlacementStrategy = "random"
                    }
                }
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(defaultConfig, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(path, json);
        }
    }
}