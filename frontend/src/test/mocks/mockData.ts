export const mockLevel = {
  id: 'test-level-1',
  width: 20,
  height: 20,
  terrain: Array(20).fill(null).map(() => Array(20).fill('grass')),
  entities: [
    {
      id: 'entity-1',
      type: 'player-spawn',
      position: { x: 5, y: 5 },
      properties: {}
    },
    {
      id: 'entity-2',
      type: 'enemy',
      position: { x: 15, y: 15 },
      properties: { health: 100 }
    }
  ],
  metadata: {
    generatedAt: new Date().toISOString(),
    generationTime: 1500,
    seed: 12345
  }
}

export const mockGenerationConfig = {
  terrain: {
    generator: 'perlin-noise',
    width: 20,
    height: 20,
    seed: 12345,
    scale: 0.1,
    octaves: 4
  },
  entities: {
    placer: 'random',
    density: 0.1,
    types: ['enemy', 'pickup', 'obstacle']
  },
  visual: {
    theme: 'forest',
    tileSize: 32
  },
  gameplay: {
    difficulty: 'medium',
    playerSpawns: 1
  }
}

export const mockExportFormats = [
  {
    id: 'json',
    name: 'JSON',
    description: 'Standard JSON format',
    fileExtension: 'json',
    supportsCustomization: true
  },
  {
    id: 'unity',
    name: 'Unity Prefab Data',
    description: 'Unity-compatible format',
    fileExtension: 'json',
    supportsCustomization: true
  },
  {
    id: 'csv',
    name: 'CSV',
    description: 'Comma-separated values',
    fileExtension: 'csv',
    supportsCustomization: false
  }
]

export const mockPresets = [
  {
    id: 'preset-1',
    name: 'Forest Level',
    description: 'Dense forest with enemies',
    config: mockGenerationConfig
  },
  {
    id: 'preset-2',
    name: 'Desert Level',
    description: 'Open desert terrain',
    config: {
      ...mockGenerationConfig,
      visual: { theme: 'desert', tileSize: 32 }
    }
  }
]

export const mockValidationErrors = [
  {
    field: 'terrain.width',
    message: 'Width must be between 10 and 100',
    code: 'INVALID_RANGE'
  },
  {
    field: 'entities.density',
    message: 'Density must be between 0 and 1',
    code: 'INVALID_RANGE'
  }
]