# JSON Configuration Documentation

This document describes the structure and validation rules for JSON configuration files used by the Procedural Mini-game Generator.

## Configuration Structure

The JSON configuration file contains the following main sections:

### Root Level Properties

- **width** (integer, 10-1000): Width of the generated level in tiles (default: 50)
- **height** (integer, 10-1000): Height of the generated level in tiles (default: 50)
- **seed** (integer): Random seed for reproducible generation (default: 0)
- **generationAlgorithm** (string): Algorithm to use for terrain generation
  - Valid values: "perlin", "cellular", "maze", "rooms"
  - Default: "perlin"
- **algorithmParameters** (object): Algorithm-specific parameters (optional)

### Entities Array

Each entity configuration object contains:

- **type** (string, required): Type of entity to place
  - Valid values: "Player", "Enemy", "Item", "PowerUp", "Checkpoint", "Exit", "NPC", "Interactive"
- **count** (integer, 0-1000): Number of entities to place (default: 1)
- **minDistance** (float, 0-100): Minimum distance from other entities (default: 1.0)
- **maxDistanceFromPlayer** (float, positive): Maximum distance from player spawn (default: unlimited)
- **placementStrategy** (string, required): Strategy for placing entities
  - Valid values: "random", "clustered", "spread", "near_walls", "center"
  - Default: "random"
- **properties** (object): Entity-specific properties (optional)

### Visual Theme Configuration

- **themeName** (string, 1-50 chars, required): Name of the visual theme (default: "default")
- **colorPalette** (object): Color mappings for different elements
  - Colors must be in hex format (#RRGGBB or #RRGGBBAA) or named colors
  - Example: `{"ground": "#8B4513", "water": "blue"}`
- **tileSprites** (object): Sprite path mappings for tile types
- **entitySprites** (object): Sprite path mappings for entity types
- **effectSettings** (object): Additional visual effect settings

### Gameplay Configuration

- **playerSpeed** (float, 0.1-50): Player movement speed (default: 5.0)
- **playerHealth** (integer, 1-10000): Player health points (default: 100)
- **difficulty** (string, required): Game difficulty level
  - Valid values: "easy", "normal", "hard", "extreme"
  - Default: "normal"
- **timeLimit** (float, 0-3600): Time limit in seconds, 0 = no limit (default: 0)
- **victoryConditions** (array, required): List of victory conditions
  - Valid values: "reach_exit", "collect_all_items", "defeat_all_enemies", "survive_time", "reach_score"
  - Must contain at least one condition
- **mechanics** (object): Special gameplay mechanics (optional)

## Validation Rules

### Automatic Validation

The system automatically validates:
- Data types and ranges for all numeric values
- Required fields are present
- String values match allowed enums
- Color values are in valid format
- Victory conditions are not empty

### Custom Validation

Additional validation includes:
- Algorithm names must be recognized
- Placement strategies must be valid
- Entity minimum distance cannot exceed maximum distance from player
- Color palette values must be valid hex codes or named colors
- At least one victory condition must be specified

### Default Value Application

When invalid values are detected, the system:
- Applies default values for out-of-range numbers
- Uses default strings for invalid enum values
- Initializes empty collections where null
- Logs warnings for all applied defaults

## Example Configurations

### Minimal Configuration
```json
{
  "width": 50,
  "height": 50,
  "generationAlgorithm": "perlin",
  "entities": [
    {
      "type": "Enemy",
      "count": 3
    },
    {
      "type": "Item",
      "count": 5
    }
  ]
}
```

### Complete Configuration
See `example-config.json` for a comprehensive example with all available options.

## Error Handling

Validation errors are categorized as:
- **Errors**: Invalid values that prevent generation
- **Warnings**: Suboptimal values that use defaults
- **Info**: Applied default values

All validation messages include:
- Specific field names
- Current and expected values
- Suggested corrections

## Usage

```csharp
var config = new GenerationConfig();
var errors = config.Validate();
var warnings = config.ApplyDefaults();

if (errors.Count == 0)
{
    // Configuration is valid, proceed with generation
}
else
{
    // Handle validation errors
    foreach (var error in errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```