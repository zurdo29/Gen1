// JSON Schema definitions for configuration validation
import Ajv from 'ajv';
import addFormats from 'ajv-formats';

// Initialize AJV with formats
export const ajv = new Ajv({ allErrors: true });
addFormats(ajv);

// Entity Type enum values
export const ENTITY_TYPES = [
  'Player', 'Enemy', 'Item', 'PowerUp', 'NPC', 
  'Exit', 'Checkpoint', 'Obstacle', 'Trigger'
] as const;

// Generation algorithms
export const GENERATION_ALGORITHMS = [
  'perlin', 'cellular', 'maze', 'rooms'
] as const;

// Terrain types
export const TERRAIN_TYPES = [
  'ground', 'wall', 'water', 'grass', 'stone', 
  'sand', 'lava', 'ice'
] as const;

// Placement strategies
export const PLACEMENT_STRATEGIES = [
  'random', 'clustered', 'spread', 'near_walls', 
  'center', 'far_from_player', 'corners'
] as const;

// Difficulty levels
export const DIFFICULTY_LEVELS = [
  'easy', 'normal', 'hard', 'extreme'
] as const;

// Victory conditions
export const VICTORY_CONDITIONS = [
  'reach_exit', 'collect_all_items', 'defeat_all_enemies', 
  'survive_time', 'reach_score'
] as const;

// Visual Theme Configuration Schema
export const visualThemeConfigSchema = {
  type: 'object',
  properties: {
    themeName: {
      type: 'string',
      minLength: 1,
      maxLength: 50
    },
    colorPalette: {
      type: 'object',
      additionalProperties: {
        type: 'string',
        pattern: '^(#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})|red|green|blue|yellow|orange|purple|pink|brown|black|white|gray|grey|cyan|magenta|lime|navy|maroon|olive|teal|silver|aqua|fuchsia)$'
      }
    },
    tileSprites: {
      type: 'object',
      additionalProperties: {
        type: 'string',
        minLength: 1
      }
    },
    entitySprites: {
      type: 'object',
      additionalProperties: {
        type: 'string',
        minLength: 1
      }
    },
    effectSettings: {
      type: 'object',
      additionalProperties: true
    }
  },
  required: ['themeName', 'colorPalette', 'tileSprites', 'entitySprites', 'effectSettings'],
  additionalProperties: false
};

// Entity Configuration Schema
export const entityConfigSchema = {
  type: 'object',
  properties: {
    type: {
      type: 'string',
      enum: [...ENTITY_TYPES]
    },
    count: {
      type: 'number',
      minimum: 0,
      maximum: 1000
    },
    minDistance: {
      type: 'number',
      minimum: 0,
      maximum: 100
    },
    maxDistanceFromPlayer: {
      type: 'number',
      minimum: 0
    },
    properties: {
      type: 'object',
      additionalProperties: true
    },
    placementStrategy: {
      type: 'string',
      enum: [...PLACEMENT_STRATEGIES]
    }
  },
  required: ['type', 'count', 'minDistance', 'maxDistanceFromPlayer', 'properties', 'placementStrategy'],
  additionalProperties: false
};

// Gameplay Configuration Schema
export const gameplayConfigSchema = {
  type: 'object',
  properties: {
    playerSpeed: {
      type: 'number',
      minimum: 0.1,
      maximum: 50
    },
    playerHealth: {
      type: 'number',
      minimum: 1,
      maximum: 10000
    },
    difficulty: {
      type: 'string',
      enum: [...DIFFICULTY_LEVELS]
    },
    timeLimit: {
      type: 'number',
      minimum: 0,
      maximum: 3600
    },
    victoryConditions: {
      type: 'array',
      items: {
        type: 'string',
        enum: [...VICTORY_CONDITIONS]
      },
      minItems: 1
    },
    mechanics: {
      type: 'object',
      additionalProperties: true
    }
  },
  required: ['playerSpeed', 'playerHealth', 'difficulty', 'timeLimit', 'victoryConditions', 'mechanics'],
  additionalProperties: false
};

// Main Generation Configuration Schema
export const generationConfigSchema = {
  type: 'object',
  properties: {
    width: {
      type: 'number',
      minimum: 10,
      maximum: 1000
    },
    height: {
      type: 'number',
      minimum: 10,
      maximum: 1000
    },
    seed: {
      type: 'number'
    },
    generationAlgorithm: {
      type: 'string',
      enum: [...GENERATION_ALGORITHMS]
    },
    algorithmParameters: {
      type: 'object',
      additionalProperties: true
    },
    terrainTypes: {
      type: 'array',
      items: {
        type: 'string',
        enum: [...TERRAIN_TYPES]
      },
      minItems: 1
    },
    entities: {
      type: 'array',
      items: entityConfigSchema
    },
    visualTheme: visualThemeConfigSchema,
    gameplay: gameplayConfigSchema
  },
  required: [
    'width', 'height', 'seed', 'generationAlgorithm', 
    'algorithmParameters', 'terrainTypes', 'entities', 
    'visualTheme', 'gameplay'
  ],
  additionalProperties: false
};

// Compile schemas
export const validateGenerationConfig = ajv.compile(generationConfigSchema);
export const validateEntityConfig = ajv.compile(entityConfigSchema);
export const validateGameplayConfig = ajv.compile(gameplayConfigSchema);
export const validateVisualThemeConfig = ajv.compile(visualThemeConfigSchema);