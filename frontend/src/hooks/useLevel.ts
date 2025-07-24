import { useState, useCallback } from 'react';
import { Level, Entity, Position } from '../types/level';
import { useUndoRedo, UndoRedoAction } from './useUndoRedo';

export interface EditValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
}

interface UseLevelReturn {
  level: Level | null;
  isLoading: boolean;
  error: string | null;
  canUndo: boolean;
  canRedo: boolean;
  generateLevel: (config: any) => Promise<void>;
  updateTile: (x: number, y: number, tileType: string) => EditValidationResult;
  updateEntity: (entityId: string, updates: Partial<Entity>) => EditValidationResult;
  moveEntity: (entityId: string, newPosition: Position) => EditValidationResult;
  addEntity: (entity: Omit<Entity, 'id'>, position: Position) => EditValidationResult;
  removeEntity: (entityId: string) => EditValidationResult;
  clearLevel: () => void;
  undo: () => void;
  redo: () => void;
  validateEntityPlacement: (entityType: string, position: Position, excludeEntityId?: string) => EditValidationResult;
  validateTerrainChange: (x: number, y: number, newTerrainType: string) => EditValidationResult;
}

export const useLevel = (): UseLevelReturn => {
  const [level, setLevel] = useState<Level | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { canUndo, canRedo, executeAction, undo, redo } = useUndoRedo();

  const generateLevel = useCallback(async (config: any) => {
    setIsLoading(true);
    setError(null);
    
    try {
      // TODO: Replace with actual API call
      // For now, generate a mock level based on config
      const mockLevel: Level = {
        id: `level-${Date.now()}`,
        width: config.width || 20,
        height: config.height || 20,
        tiles: [],
        entities: [],
        spawnPoints: [{ x: 1, y: 1 }],
        metadata: {
          seed: config.seed || Math.floor(Math.random() * 10000),
          generationAlgorithm: config.generationAlgorithm || 'perlin',
          createdAt: new Date()
        }
      };

      // Generate tiles
      mockLevel.tiles = Array(mockLevel.height).fill(null).map((_, y) =>
        Array(mockLevel.width).fill(null).map((_, x) => ({
          type: generateTileType(x, y, mockLevel.width, mockLevel.height, config),
          position: { x, y }
        }))
      );

      // Generate entities
      mockLevel.entities = generateEntities(mockLevel.width, mockLevel.height, config);

      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      setLevel(mockLevel);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate level');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const validateTerrainChange = useCallback((x: number, y: number, newTerrainType: string): EditValidationResult => {
    if (!level) {
      return { isValid: false, errors: ['No level loaded'], warnings: [] };
    }

    if (x < 0 || x >= level.width || y < 0 || y >= level.height) {
      return { isValid: false, errors: ['Position is outside level bounds'], warnings: [] };
    }

    const currentTile = level.tiles[y][x];
    if (currentTile.type === newTerrainType) {
      return { isValid: false, errors: ['Terrain type is already set to this value'], warnings: [] };
    }

    // Check if changing this tile would block essential paths
    const warnings: string[] = [];
    if (newTerrainType === 'wall' && (currentTile.type === 'ground' || currentTile.type === 'grass')) {
      // Check if there are entities on this tile
      const entitiesOnTile = level.entities.filter(e => e.position.x === x && e.position.y === y);
      if (entitiesOnTile.length > 0) {
        warnings.push('This will block entities on this tile');
      }
    }

    return { isValid: true, errors: [], warnings };
  }, [level]);

  const updateTile = useCallback((x: number, y: number, tileType: string): EditValidationResult => {
    const validation = validateTerrainChange(x, y, tileType);
    if (!validation.isValid || !level) {
      return validation;
    }

    const oldTileType = level.tiles[y][x].type;
    
    const action: UndoRedoAction = {
      id: `tile-${x}-${y}-${Date.now()}`,
      type: 'terrain-change',
      description: `Change tile at (${x}, ${y}) from ${oldTileType} to ${tileType}`,
      timestamp: new Date(),
      undo: () => {
        setLevel(prevLevel => {
          if (!prevLevel) return prevLevel;
          const newLevel = { ...prevLevel };
          newLevel.tiles = prevLevel.tiles.map(row => [...row]);
          newLevel.tiles[y][x] = { ...newLevel.tiles[y][x], type: oldTileType };
          return newLevel;
        });
      },
      redo: () => {
        setLevel(prevLevel => {
          if (!prevLevel) return prevLevel;
          const newLevel = { ...prevLevel };
          newLevel.tiles = prevLevel.tiles.map(row => [...row]);
          newLevel.tiles[y][x] = { ...newLevel.tiles[y][x], type: tileType };
          return newLevel;
        });
      }
    };

    executeAction(action);
    return validation;
  }, [level, validateTerrainChange, executeAction]);

  const validateEntityPlacement = useCallback((entityType: string, position: Position, excludeEntityId?: string): EditValidationResult => {
    if (!level) {
      return { isValid: false, errors: ['No level loaded'], warnings: [] };
    }

    if (position.x < 0 || position.x >= level.width || position.y < 0 || position.y >= level.height) {
      return { isValid: false, errors: ['Position is outside level bounds'], warnings: [] };
    }

    const tile = level.tiles[position.y][position.x];
    const errors: string[] = [];
    const warnings: string[] = [];

    // Check terrain compatibility
    if (tile.type === 'wall') {
      errors.push('Cannot place entity on wall tile');
    } else if (tile.type === 'water' && !['Player', 'Item', 'PowerUp'].includes(entityType)) {
      warnings.push('This entity type may not work well on water');
    }

    // Check for entity conflicts
    const existingEntities = level.entities.filter(e => 
      e.position.x === position.x && 
      e.position.y === position.y && 
      e.id !== excludeEntityId
    );

    if (existingEntities.length > 0) {
      if (entityType === 'Player' || existingEntities.some(e => e.type === 'Player')) {
        errors.push('Cannot place multiple entities on the same tile with Player');
      } else {
        warnings.push('Multiple entities on the same tile may cause conflicts');
      }
    }

    // Check for Player uniqueness
    if (entityType === 'Player') {
      const existingPlayers = level.entities.filter(e => e.type === 'Player' && e.id !== excludeEntityId);
      if (existingPlayers.length > 0) {
        errors.push('Only one Player entity is allowed per level');
      }
    }

    return { isValid: errors.length === 0, errors, warnings };
  }, [level]);

  const updateEntity = useCallback((entityId: string, updates: Partial<Entity>): EditValidationResult => {
    if (!level) {
      return { isValid: false, errors: ['No level loaded'], warnings: [] };
    }

    const entity = level.entities.find(e => e.id === entityId);
    if (!entity) {
      return { isValid: false, errors: ['Entity not found'], warnings: [] };
    }

    // Validate if position is being updated
    if (updates.position) {
      const validation = validateEntityPlacement(entity.type, updates.position, entityId);
      if (!validation.isValid) {
        return validation;
      }
    }

    const oldEntity = { ...entity };
    
    const action: UndoRedoAction = {
      id: `entity-update-${entityId}-${Date.now()}`,
      type: 'entity-update',
      description: `Update ${entity.type} entity`,
      timestamp: new Date(),
      undo: () => {
        setLevel(prevLevel => {
          if (!prevLevel) return prevLevel;
          return {
            ...prevLevel,
            entities: prevLevel.entities.map(e =>
              e.id === entityId ? oldEntity : e
            )
          };
        });
      },
      redo: () => {
        setLevel(prevLevel => {
          if (!prevLevel) return prevLevel;
          return {
            ...prevLevel,
            entities: prevLevel.entities.map(e =>
              e.id === entityId ? { ...e, ...updates } : e
            )
          };
        });
      }
    };

    executeAction(action);
    return { isValid: true, errors: [], warnings: [] };
  }, [level, validateEntityPlacement, executeAction]);

  const moveEntity = useCallback((entityId: string, newPosition: Position): EditValidationResult => {
    return updateEntity(entityId, { position: newPosition });
  }, [updateEntity]);

  const addEntity = useCallback((entityData: Omit<Entity, 'id'>, position: Position): EditValidationResult => {
    const validation = validateEntityPlacement(entityData.type, position);
    if (!validation.isValid || !level) {
      return validation;
    }

    const newEntity: Entity = {
      ...entityData,
      position,
      id: `entity-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`
    };

    const action: UndoRedoAction = {
      id: `add-entity-${newEntity.id}`,
      type: 'entity-add',
      description: `Add ${entityData.type} entity at (${position.x}, ${position.y})`,
      timestamp: new Date(),
      undo: () => {
        setLevel(prevLevel => {
          if (!prevLevel) return prevLevel;
          return {
            ...prevLevel,
            entities: prevLevel.entities.filter(e => e.id !== newEntity.id)
          };
        });
      },
      redo: () => {
        setLevel(prevLevel => {
          if (!prevLevel) return prevLevel;
          return {
            ...prevLevel,
            entities: [...prevLevel.entities, newEntity]
          };
        });
      }
    };

    executeAction(action);
    return validation;
  }, [level, validateEntityPlacement, executeAction]);

  const removeEntity = useCallback((entityId: string): EditValidationResult => {
    if (!level) {
      return { isValid: false, errors: ['No level loaded'], warnings: [] };
    }

    const entity = level.entities.find(e => e.id === entityId);
    if (!entity) {
      return { isValid: false, errors: ['Entity not found'], warnings: [] };
    }

    const action: UndoRedoAction = {
      id: `remove-entity-${entityId}`,
      type: 'entity-remove',
      description: `Remove ${entity.type} entity from (${entity.position.x}, ${entity.position.y})`,
      timestamp: new Date(),
      undo: () => {
        setLevel(prevLevel => {
          if (!prevLevel) return prevLevel;
          return {
            ...prevLevel,
            entities: [...prevLevel.entities, entity]
          };
        });
      },
      redo: () => {
        setLevel(prevLevel => {
          if (!prevLevel) return prevLevel;
          return {
            ...prevLevel,
            entities: prevLevel.entities.filter(e => e.id !== entityId)
          };
        });
      }
    };

    executeAction(action);
    return { isValid: true, errors: [], warnings: [] };
  }, [level, executeAction]);

  const clearLevel = useCallback(() => {
    setLevel(null);
    setError(null);
  }, []);

  return {
    level,
    isLoading,
    error,
    canUndo,
    canRedo,
    generateLevel,
    updateTile,
    updateEntity,
    moveEntity,
    addEntity,
    removeEntity,
    clearLevel,
    undo,
    redo,
    validateEntityPlacement,
    validateTerrainChange
  };
};

// Helper function to generate tile types based on position and config
function generateTileType(x: number, y: number, width: number, height: number, _config: any): string {
  // Simple border walls
  if (x === 0 || x === width - 1 || y === 0 || y === height - 1) {
    return 'wall';
  }

  // Add some variety based on position
  const centerX = width / 2;
  const centerY = height / 2;
  const distanceFromCenter = Math.sqrt((x - centerX) ** 2 + (y - centerY) ** 2);
  
  if (distanceFromCenter < 3) {
    return 'grass';
  } else if (Math.random() < 0.1) {
    return Math.random() < 0.5 ? 'stone' : 'water';
  }
  
  return 'ground';
}

// Helper function to generate entities
function generateEntities(width: number, height: number, _config: any): Entity[] {
  const entities: Entity[] = [];
  
  // Add player spawn
  entities.push({
    id: 'player-spawn',
    type: 'Player',
    position: { x: 1, y: 1 },
    properties: {}
  });

  // Add some random enemies
  for (let i = 0; i < 3; i++) {
    entities.push({
      id: `enemy-${i}`,
      type: 'Enemy',
      position: {
        x: Math.floor(Math.random() * (width - 4)) + 2,
        y: Math.floor(Math.random() * (height - 4)) + 2
      },
      properties: {}
    });
  }

  // Add some items
  for (let i = 0; i < 2; i++) {
    entities.push({
      id: `item-${i}`,
      type: 'Item',
      position: {
        x: Math.floor(Math.random() * (width - 4)) + 2,
        y: Math.floor(Math.random() * (height - 4)) + 2
      },
      properties: {}
    });
  }

  // Add exit
  entities.push({
    id: 'exit',
    type: 'Exit',
    position: { x: width - 2, y: height - 2 },
    properties: {}
  });

  return entities;
}