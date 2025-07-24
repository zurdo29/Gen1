import { GenerationConfig, EntityType } from '../types';

/**
 * Creates a default generation configuration with sensible defaults
 */
export const createDefaultConfig = (): GenerationConfig => {
  return {
    width: 50,
    height: 50,
    seed: 0,
    generationAlgorithm: 'perlin',
    algorithmParameters: {
      scale: 0.1,
      octaves: 4,
      persistence: 0.5,
      lacunarity: 2.0,
      threshold: 0.0
    },
    terrainTypes: ['ground', 'wall', 'water'],
    entities: [
      {
        type: 'Player' as EntityType,
        count: 1,
        minDistance: 1.0,
        maxDistanceFromPlayer: Number.MAX_VALUE,
        properties: {},
        placementStrategy: 'center'
      },
      {
        type: 'Exit' as EntityType,
        count: 1,
        minDistance: 5.0,
        maxDistanceFromPlayer: Number.MAX_VALUE,
        properties: {},
        placementStrategy: 'far_from_player'
      },
      {
        type: 'Item' as EntityType,
        count: 5,
        minDistance: 2.0,
        maxDistanceFromPlayer: Number.MAX_VALUE,
        properties: {},
        placementStrategy: 'random'
      }
    ],
    visualTheme: {
      themeName: 'default',
      colorPalette: {
        ground: '#8B4513',
        wall: '#696969',
        water: '#4169E1',
        grass: '#228B22',
        stone: '#708090',
        sand: '#F4A460',
        lava: '#FF4500',
        ice: '#B0E0E6'
      },
      tileSprites: {},
      entitySprites: {},
      effectSettings: {
        showGrid: true,
        tileSize: 32,
        animationSpeed: 1.0
      }
    },
    gameplay: {
      playerSpeed: 5.0,
      playerHealth: 100,
      difficulty: 'normal',
      timeLimit: 0,
      victoryConditions: ['reach_exit'],
      mechanics: {}
    }
  };
};

/**
 * Creates preset configurations for common use cases
 */
export const createPresetConfigs = () => {
  const baseConfig = createDefaultConfig();

  return {
    dungeon: {
      ...baseConfig,
      generationAlgorithm: 'rooms',
      algorithmParameters: {
        minRoomSize: 6,
        maxRoomSize: 12,
        roomCount: 8,
        corridorWidth: 2
      },
      terrainTypes: ['ground', 'wall'],
      entities: [
        {
          type: 'Player' as EntityType,
          count: 1,
          minDistance: 1.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: {},
          placementStrategy: 'center'
        },
        {
          type: 'Exit' as EntityType,
          count: 1,
          minDistance: 10.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: {},
          placementStrategy: 'far_from_player'
        },
        {
          type: 'Enemy' as EntityType,
          count: 8,
          minDistance: 3.0,
          maxDistanceFromPlayer: 5.0,
          properties: { health: 50, damage: 10 },
          placementStrategy: 'random'
        },
        {
          type: 'Item' as EntityType,
          count: 12,
          minDistance: 2.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: { value: 10 },
          placementStrategy: 'random'
        }
      ],
      visualTheme: {
        ...baseConfig.visualTheme,
        themeName: 'dungeon',
        colorPalette: {
          ground: '#4A4A4A',
          wall: '#2F2F2F'
        }
      },
      gameplay: {
        ...baseConfig.gameplay,
        difficulty: 'normal',
        victoryConditions: ['reach_exit', 'defeat_all_enemies']
      }
    },

    maze: {
      ...baseConfig,
      generationAlgorithm: 'maze',
      algorithmParameters: {
        wallThickness: 1,
        pathWidth: 2,
        complexity: 0.75
      },
      terrainTypes: ['ground', 'wall'],
      entities: [
        {
          type: 'Player' as EntityType,
          count: 1,
          minDistance: 1.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: {},
          placementStrategy: 'corners'
        },
        {
          type: 'Exit' as EntityType,
          count: 1,
          minDistance: 1.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: {},
          placementStrategy: 'corners'
        },
        {
          type: 'Item' as EntityType,
          count: 3,
          minDistance: 5.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: {},
          placementStrategy: 'random'
        }
      ],
      visualTheme: {
        ...baseConfig.visualTheme,
        themeName: 'maze',
        colorPalette: {
          ground: '#F5F5DC',
          wall: '#8B4513'
        }
      },
      gameplay: {
        ...baseConfig.gameplay,
        playerSpeed: 3.0,
        timeLimit: 300,
        victoryConditions: ['reach_exit']
      }
    },

    survival: {
      ...baseConfig,
      width: 80,
      height: 80,
      generationAlgorithm: 'cellular',
      algorithmParameters: {
        initialDensity: 0.45,
        iterations: 5,
        birthLimit: 4,
        deathLimit: 3
      },
      terrainTypes: ['ground', 'wall', 'water'],
      entities: [
        {
          type: 'Player' as EntityType,
          count: 1,
          minDistance: 1.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: {},
          placementStrategy: 'center'
        },
        {
          type: 'Enemy' as EntityType,
          count: 20,
          minDistance: 2.0,
          maxDistanceFromPlayer: 10.0,
          properties: { health: 30, damage: 15 },
          placementStrategy: 'spread'
        },
        {
          type: 'PowerUp' as EntityType,
          count: 8,
          minDistance: 3.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: { type: 'health', amount: 25 },
          placementStrategy: 'random'
        },
        {
          type: 'Item' as EntityType,
          count: 15,
          minDistance: 2.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: { type: 'ammo', amount: 10 },
          placementStrategy: 'clustered'
        }
      ],
      visualTheme: {
        ...baseConfig.visualTheme,
        themeName: 'survival',
        colorPalette: {
          ground: '#654321',
          wall: '#2F4F2F',
          water: '#191970'
        }
      },
      gameplay: {
        ...baseConfig.gameplay,
        playerHealth: 150,
        difficulty: 'hard',
        timeLimit: 600,
        victoryConditions: ['survive_time', 'defeat_all_enemies']
      }
    },

    platformer: {
      ...baseConfig,
      width: 100,
      height: 30,
      generationAlgorithm: 'perlin',
      algorithmParameters: {
        scale: 0.05,
        octaves: 3,
        persistence: 0.6,
        lacunarity: 2.0,
        threshold: 0.2
      },
      terrainTypes: ['ground', 'wall', 'grass'],
      entities: [
        {
          type: 'Player' as EntityType,
          count: 1,
          minDistance: 1.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: {},
          placementStrategy: 'center'
        },
        {
          type: 'Exit' as EntityType,
          count: 1,
          minDistance: 20.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: {},
          placementStrategy: 'far_from_player'
        },
        {
          type: 'Enemy' as EntityType,
          count: 12,
          minDistance: 3.0,
          maxDistanceFromPlayer: 8.0,
          properties: { patrol: true, speed: 2 },
          placementStrategy: 'spread'
        },
        {
          type: 'Item' as EntityType,
          count: 20,
          minDistance: 2.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: { type: 'coin', value: 100 },
          placementStrategy: 'random'
        },
        {
          type: 'PowerUp' as EntityType,
          count: 5,
          minDistance: 5.0,
          maxDistanceFromPlayer: Number.MAX_VALUE,
          properties: { type: 'jump_boost', duration: 10 },
          placementStrategy: 'spread'
        }
      ],
      visualTheme: {
        ...baseConfig.visualTheme,
        themeName: 'platformer',
        colorPalette: {
          ground: '#8B4513',
          wall: '#696969',
          grass: '#32CD32'
        }
      },
      gameplay: {
        ...baseConfig.gameplay,
        playerSpeed: 7.0,
        victoryConditions: ['reach_exit', 'collect_all_items']
      }
    }
  };
};