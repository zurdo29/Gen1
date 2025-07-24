using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Editor
{
    /// <summary>
    /// Editor window for procedural level generation
    /// </summary>
    public class GenerationWindow
    {
        private readonly IEditorIntegration _editorIntegration;
        private readonly IGenerationManager _generationManager;
        private string _selectedConfigPath = string.Empty;
        private GenerationConfig? _currentConfig;
        private Level? _previewLevel;
        private bool _isVisible = false;
        
        public GenerationWindow(IEditorIntegration editorIntegration, IGenerationManager generationManager)
        {
            _editorIntegration = editorIntegration ?? throw new ArgumentNullException(nameof(editorIntegration));
            _generationManager = generationManager ?? throw new ArgumentNullException(nameof(generationManager));
        }
        
        /// <summary>
        /// Shows the generation window
        /// </summary>
        public void Show()
        {
            _isVisible = true;
            DisplayWindow();
        }
        
        /// <summary>
        /// Hides the generation window
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
            Console.WriteLine("Generation window closed");
        }
        
        /// <summary>
        /// Checks if the window is currently visible
        /// </summary>
        public bool IsVisible => _isVisible;
        
        private void DisplayWindow()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    LEVEL GENERATION WINDOW                  ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║                                                              ║");
            
            DisplayConfigurationSection();
            DisplayGenerationSection();
            DisplayPreviewSection();
            DisplayControls();
            
            Console.WriteLine("║                                                              ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            
            HandleUserInput();
        }
        
        private void DisplayConfigurationSection()
        {
            Console.WriteLine("║  Configuration File:                                         ║");
            
            if (string.IsNullOrEmpty(_selectedConfigPath))
            {
                Console.WriteLine("║    [No file selected]                                       ║");
            }
            else
            {
                var fileName = Path.GetFileName(_selectedConfigPath);
                Console.WriteLine($"║    {fileName,-54} ║");
                
                if (_currentConfig != null)
                {
                    Console.WriteLine($"║    Size: {_currentConfig.Width}x{_currentConfig.Height,-42} ║");
                    Console.WriteLine($"║    Algorithm: {_currentConfig.GenerationAlgorithm,-43} ║");
                    Console.WriteLine($"║    Entities: {_currentConfig.Entities?.Count ?? 0,-44} ║");
                }
            }
            
            Console.WriteLine("║                                                              ║");
        }
        
        private void DisplayGenerationSection()
        {
            Console.WriteLine("║  Generation Options:                                         ║");
            Console.WriteLine("║    [G] Generate New Level                                    ║");
            Console.WriteLine("║    [R] Regenerate with Same Seed                            ║");
            Console.WriteLine("║    [S] Generate with Random Seed                            ║");
            Console.WriteLine("║                                                              ║");
        }
        
        private void DisplayPreviewSection()
        {
            Console.WriteLine("║  Level Preview:                                              ║");
            
            if (_previewLevel == null)
            {
                Console.WriteLine("║    [No level generated yet]                                 ║");
            }
            else
            {
                Console.WriteLine($"║    Name: {_previewLevel.Name,-49} ║");
                Console.WriteLine($"║    Size: {_previewLevel.Terrain?.Width ?? 0}x{_previewLevel.Terrain?.Height ?? 0,-49} ║");
                Console.WriteLine($"║    Entities: {_previewLevel.Entities?.Count ?? 0,-45} ║");
                
                // Show mini preview
                DisplayMiniPreview();
            }
            
            Console.WriteLine("║                                                              ║");
        }
        
        private void DisplayMiniPreview()
        {
            if (_previewLevel?.Terrain == null) return;
            
            Console.WriteLine("║    Mini Preview:                                             ║");
            
            var width = Math.Min(_previewLevel.Terrain.Width, 20);
            var height = Math.Min(_previewLevel.Terrain.Height, 8);
            
            for (int y = 0; y < height; y++)
            {
                Console.Write("║      ");
                for (int x = 0; x < width; x++)
                {
                    var tile = _previewLevel.Terrain.GetTile(x, y);
                    var symbol = GetTileSymbol(tile);
                    
                    // Check for entities at this position
                    var entity = _previewLevel.Entities?.FirstOrDefault(e => 
                        Math.Abs(e.Position.X - x) < 0.5f && Math.Abs(e.Position.Y - y) < 0.5f);
                    
                    if (entity != null)
                    {
                        symbol = GetEntitySymbol(entity.Type);
                    }
                    
                    Console.Write(symbol);
                }
                
                // Pad the rest of the line
                var padding = 54 - width;
                Console.Write(new string(' ', padding));
                Console.WriteLine("║");
            }
        }
        
        private void DisplayControls()
        {
            Console.WriteLine("║  Controls:                                                   ║");
            Console.WriteLine("║    [O] Open Configuration File                              ║");
            Console.WriteLine("║    [P] Show Full Preview                                    ║");
            Console.WriteLine("║    [E] Export Level                                         ║");
            Console.WriteLine("║    [Q] Close Window                                         ║");
        }
        
        private void HandleUserInput()
        {
            Console.WriteLine();
            Console.Write("Select an option: ");
            
            var input = Console.ReadKey(true);
            Console.WriteLine();
            
            try
            {
                switch (char.ToUpper(input.KeyChar))
                {
                    case 'O':
                        SelectConfigurationFile();
                        break;
                    case 'G':
                        GenerateNewLevel();
                        break;
                    case 'R':
                        RegenerateLevel(false);
                        break;
                    case 'S':
                        RegenerateLevel(true);
                        break;
                    case 'P':
                        ShowFullPreview();
                        break;
                    case 'E':
                        ExportLevel();
                        break;
                    case 'Q':
                        Hide();
                        return;
                    default:
                        Console.WriteLine("Invalid option. Press any key to continue...");
                        Console.ReadKey(true);
                        break;
                }
            }
            catch (Exception ex)
            {
                _editorIntegration.ReportErrors(new List<string> { $"Operation failed: {ex.Message}" });
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
            
            if (_isVisible)
            {
                DisplayWindow(); // Refresh the window
            }
        }
        
        private void SelectConfigurationFile()
        {
            _selectedConfigPath = _editorIntegration.SelectConfigurationFile();
            
            if (!string.IsNullOrEmpty(_selectedConfigPath))
            {
                LoadConfiguration();
            }
        }
        
        private void LoadConfiguration()
        {
            try
            {
                if (!File.Exists(_selectedConfigPath))
                {
                    _editorIntegration.ReportErrors(new List<string> { $"Configuration file not found: {_selectedConfigPath}" });
                    return;
                }
                
                var json = File.ReadAllText(_selectedConfigPath);
                _currentConfig = System.Text.Json.JsonSerializer.Deserialize<GenerationConfig>(json);
                
                // Validate the configuration
                if (_currentConfig != null)
                {
                    var validationResult = ConfigurationValidator.ValidateConfiguration(_currentConfig);
                    _editorIntegration.ReportValidationResult(validationResult.ToValidationResult());
                    
                    if (validationResult.IsValid && !validationResult.HasWarnings)
                    {
                        _editorIntegration.DisplaySuccessMessage("Configuration loaded successfully");
                    }
                }
                else
                {
                    _editorIntegration.ReportErrors(new List<string> { "Failed to deserialize configuration file" });
                }
            }
            catch (Exception ex)
            {
                _editorIntegration.ReportErrors(new List<string> { $"Failed to load configuration: {ex.Message}" });
            }
        }
        
        private void GenerateNewLevel()
        {
            if (_currentConfig == null)
            {
                _editorIntegration.ReportErrors(new List<string> { "No configuration loaded. Please select a configuration file first." });
                return;
            }
            
            try
            {
                Console.WriteLine("Generating new level...");
                
                // TODO: Use actual generation manager when available
                // For now, create a mock level for testing
                _previewLevel = CreateMockLevel(_currentConfig);
                
                Console.WriteLine("✓ Level generated successfully");
                _editorIntegration.ShowLevelPreview(_previewLevel);
            }
            catch (Exception ex)
            {
                _editorIntegration.ReportErrors(new List<string> { $"Generation failed: {ex.Message}" });
            }
        }
        
        private void RegenerateLevel(bool randomSeed)
        {
            if (_currentConfig == null)
            {
                _editorIntegration.ReportErrors(new List<string> { "No configuration loaded. Please select a configuration file first." });
                return;
            }
            
            try
            {
                if (randomSeed)
                {
                    _currentConfig.Seed = new Random().Next();
                    Console.WriteLine($"Regenerating with random seed: {_currentConfig.Seed}");
                }
                else
                {
                    Console.WriteLine($"Regenerating with same seed: {_currentConfig.Seed}");
                }
                
                GenerateNewLevel();
            }
            catch (Exception ex)
            {
                _editorIntegration.ReportErrors(new List<string> { $"Regeneration failed: {ex.Message}" });
            }
        }
        
        private void ShowFullPreview()
        {
            if (_previewLevel == null)
            {
                _editorIntegration.ReportErrors(new List<string> { "No level to preview. Generate a level first." });
                return;
            }
            
            _editorIntegration.ShowLevelPreview(_previewLevel);
            Console.WriteLine("Press any key to return to generation window...");
            Console.ReadKey(true);
        }
        
        private void ExportLevel()
        {
            if (_previewLevel == null)
            {
                _editorIntegration.ReportErrors(new List<string> { "No level to export. Generate a level first." });
                return;
            }
            
            try
            {
                var exportPath = $"exported_level_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var json = _previewLevel.ExportToJson();
                File.WriteAllText(exportPath, json);
                
                Console.WriteLine($"✓ Level exported to: {exportPath}");
            }
            catch (Exception ex)
            {
                _editorIntegration.ReportErrors(new List<string> { $"Export failed: {ex.Message}" });
            }
        }
        
        private Level CreateMockLevel(GenerationConfig config)
        {
            // Create a mock level for testing purposes
            // This would be replaced with actual generation logic
            var level = new Level
            {
                Name = $"Generated Level {DateTime.Now:HH:mm:ss}",
                Terrain = new TileMap(config.Width, config.Height),
                Entities = new List<Entity>()
            };
            
            // Fill with some basic terrain
            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    if (x == 0 || y == 0 || x == config.Width - 1 || y == config.Height - 1)
                    {
                        level.Terrain.SetTile(x, y, TileType.Wall);
                    }
                    else
                    {
                        level.Terrain.SetTile(x, y, TileType.Ground);
                    }
                }
            }
            
            // Add some mock entities
            var random = new Random(config.Seed);
            foreach (var entityConfig in config.Entities ?? new List<EntityConfig>())
            {
                for (int i = 0; i < entityConfig.Count; i++)
                {
                    var x = random.Next(1, config.Width - 1);
                    var y = random.Next(1, config.Height - 1);
                    
                    var entity = EntityFactory.CreateEntity(entityConfig.Type);
                    entity.Position = new System.Numerics.Vector2(x, y);
                    entity.Properties = entityConfig.Properties ?? new Dictionary<string, object>();
                    
                    level.Entities.Add(entity);
                }
            }
            
            return level;
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
    }
}