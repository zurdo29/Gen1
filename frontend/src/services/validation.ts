import { 
  ajv, 
  validateGenerationConfig, 
  validateEntityConfig, 
  validateGameplayConfig, 
  validateVisualThemeConfig,
  ENTITY_TYPES,
  GENERATION_ALGORITHMS,
  TERRAIN_TYPES,
  PLACEMENT_STRATEGIES,
  DIFFICULTY_LEVELS,
  VICTORY_CONDITIONS
} from '../schemas';
import { GenerationConfig, ValidationResult, ValidationError, ValidationWarning } from '../types';

export interface ValidationOptions {
  strict?: boolean;
  includeWarnings?: boolean;
  crossFieldValidation?: boolean;
  performanceCheck?: boolean;
}

export interface FieldValidationResult {
  isValid: boolean;
  errors: ValidationError[];
  warnings: ValidationWarning[];
  suggestions?: string[];
}

export class ValidationService {
  private static instance: ValidationService;

  static getInstance(): ValidationService {
    if (!ValidationService.instance) {
      ValidationService.instance = new ValidationService();
    }
    return ValidationService.instance;
  }

  /**
   * Comprehensive validation of generation configuration
   */
  validateConfiguration(
    config: Partial<GenerationConfig>, 
    options: ValidationOptions = {}
  ): ValidationResult {
    const {
      strict = false,
      includeWarnings = true,
      crossFieldValidation = true,
      performanceCheck = true
    } = options;

    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];

    // Basic schema validation
    const schemaResult = this.validateSchema(config);
    errors.push(...schemaResult.errors);
    if (includeWarnings) {
      warnings.push(...schemaResult.warnings);
    }

    // Cross-field validation
    if (crossFieldValidation) {
      const crossFieldResult = this.validateCrossFields(config);
      errors.push(...crossFieldResult.errors);
      if (includeWarnings) {
        warnings.push(...crossFieldResult.warnings);
      }
    }

    // Performance validation
    if (performanceCheck) {
      const performanceResult = this.validatePerformance(config);
      if (strict) {
        errors.push(...performanceResult.errors);
      } else if (includeWarnings) {
        warnings.push(...performanceResult.warnings);
      }
    }

    // Business logic validation
    const businessResult = this.validateBusinessLogic(config);
    errors.push(...businessResult.errors);
    if (includeWarnings) {
      warnings.push(...businessResult.warnings);
    }

    return {
      isValid: errors.length === 0,
      errors,
      warnings
    };
  }

  /**
   * Validate individual field with real-time feedback
   */
  validateField(
    fieldPath: string, 
    value: any, 
    config: Partial<GenerationConfig> = {}
  ): FieldValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];
    const suggestions: string[] = [];

    switch (fieldPath) {
      case 'width':
        return this.validateWidth(value, config);
      case 'height':
        return this.validateHeight(value, config);
      case 'seed':
        return this.validateSeed(value);
      case 'generationAlgorithm':
        return this.validateGenerationAlgorithm(value);
      case 'terrainTypes':
        return this.validateTerrainTypes(value);
      case 'entities':
        return this.validateEntities(value, config);
      case 'visualTheme':
        return this.validateVisualTheme(value);
      case 'gameplay':
        return this.validateGameplay(value);
      default:
        // Handle nested field paths
        if (fieldPath.includes('.')) {
          return this.validateNestedField(fieldPath, value, config);
        }
        return { isValid: true, errors: [], warnings: [] };
    }
  }

  private validateSchema(config: Partial<GenerationConfig>): ValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];

    // Use AJV for basic schema validation
    const isValid = validateGenerationConfig(config);
    
    if (!isValid && validateGenerationConfig.errors) {
      for (const error of validateGenerationConfig.errors) {
        errors.push({
          field: error.instancePath.replace('/', '') || error.params?.missingProperty || 'root',
          message: this.formatAjvError(error),
          code: this.getErrorCode(error)
        });
      }
    }

    return { isValid: errors.length === 0, errors, warnings };
  }

  private validateCrossFields(config: Partial<GenerationConfig>): ValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];

    // Level size vs entity count validation
    if (config.width && config.height && config.entities) {
      const levelArea = config.width * config.height;
      const totalEntities = config.entities.reduce((sum, entity) => sum + (entity.count || 0), 0);
      const entityDensity = totalEntities / levelArea;

      if (entityDensity > 0.5) {
        errors.push({
          field: 'entities',
          message: 'Too many entities for the level size. This may cause placement conflicts.',
          code: 'ENTITY_DENSITY_TOO_HIGH'
        });
      } else if (entityDensity > 0.3) {
        warnings.push({
          field: 'entities',
          message: 'High entity density may affect performance.',
          suggestion: 'Consider reducing entity count or increasing level size.'
        });
      }
    }

    // Player entity validation
    if (config.entities) {
      const playerEntities = config.entities.filter(e => e.type === 'Player');
      if (playerEntities.length === 0) {
        errors.push({
          field: 'entities',
          message: 'At least one Player entity is required.',
          code: 'MISSING_PLAYER_ENTITY'
        });
      } else if (playerEntities.length > 1) {
        warnings.push({
          field: 'entities',
          message: 'Multiple Player entities detected.',
          suggestion: 'Consider using only one Player entity for clarity.'
        });
      }
    }

    // Exit entity validation for reach_exit victory condition
    if (config.gameplay?.victoryConditions?.includes('reach_exit') && config.entities) {
      const exitEntities = config.entities.filter(e => e.type === 'Exit');
      if (exitEntities.length === 0) {
        errors.push({
          field: 'entities',
          message: 'Exit entity required when victory condition is "reach_exit".',
          code: 'MISSING_EXIT_ENTITY'
        });
      }
    }

    // Item entities validation for collect_all_items victory condition
    if (config.gameplay?.victoryConditions?.includes('collect_all_items') && config.entities) {
      const itemEntities = config.entities.filter(e => e.type === 'Item' || e.type === 'PowerUp');
      if (itemEntities.length === 0) {
        errors.push({
          field: 'entities',
          message: 'Item or PowerUp entities required when victory condition is "collect_all_items".',
          code: 'MISSING_ITEM_ENTITIES'
        });
      }
    }

    // Enemy entities validation for defeat_all_enemies victory condition
    if (config.gameplay?.victoryConditions?.includes('defeat_all_enemies') && config.entities) {
      const enemyEntities = config.entities.filter(e => e.type === 'Enemy');
      if (enemyEntities.length === 0) {
        errors.push({
          field: 'entities',
          message: 'Enemy entities required when victory condition is "defeat_all_enemies".',
          code: 'MISSING_ENEMY_ENTITIES'
        });
      }
    }

    // Time limit validation
    if (config.gameplay?.victoryConditions?.includes('survive_time') && 
        (!config.gameplay.timeLimit || config.gameplay.timeLimit <= 0)) {
      errors.push({
        field: 'gameplay.timeLimit',
        message: 'Time limit must be greater than 0 when victory condition is "survive_time".',
        code: 'INVALID_TIME_LIMIT'
      });
    }

    return { isValid: errors.length === 0, errors, warnings };
  }

  private validatePerformance(config: Partial<GenerationConfig>): ValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];

    // Level size performance check
    if (config.width && config.height) {
      const levelArea = config.width * config.height;
      
      if (levelArea > 100000) { // 316x316 or equivalent
        errors.push({
          field: 'width,height',
          message: 'Level size too large. May cause performance issues or generation timeouts.',
          code: 'LEVEL_SIZE_TOO_LARGE'
        });
      } else if (levelArea > 40000) { // 200x200 or equivalent
        warnings.push({
          field: 'width,height',
          message: 'Large level size may affect performance.',
          suggestion: 'Consider reducing dimensions for better performance.'
        });
      }
    }

    // Entity count performance check
    if (config.entities) {
      const totalEntities = config.entities.reduce((sum, entity) => sum + (entity.count || 0), 0);
      
      if (totalEntities > 1000) {
        errors.push({
          field: 'entities',
          message: 'Too many entities. May cause performance issues.',
          code: 'TOO_MANY_ENTITIES'
        });
      } else if (totalEntities > 500) {
        warnings.push({
          field: 'entities',
          message: 'High entity count may affect performance.',
          suggestion: 'Consider reducing entity counts.'
        });
      }
    }

    // Algorithm complexity check
    if (config.generationAlgorithm === 'maze' && config.width && config.height) {
      const levelArea = config.width * config.height;
      if (levelArea > 10000) { // 100x100
        warnings.push({
          field: 'generationAlgorithm',
          message: 'Maze algorithm with large levels may be slow.',
          suggestion: 'Consider using "perlin" or "cellular" for large levels.'
        });
      }
    }

    return { isValid: errors.length === 0, errors, warnings };
  }

  private validateBusinessLogic(config: Partial<GenerationConfig>): ValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];

    // Minimum viable level validation
    if (config.width && config.width < 10) {
      errors.push({
        field: 'width',
        message: 'Level width too small. Minimum recommended width is 10.',
        code: 'WIDTH_TOO_SMALL'
      });
    }

    if (config.height && config.height < 10) {
      errors.push({
        field: 'height',
        message: 'Level height too small. Minimum recommended height is 10.',
        code: 'HEIGHT_TOO_SMALL'
      });
    }

    // Terrain type validation
    if (config.terrainTypes && config.terrainTypes.length === 0) {
      errors.push({
        field: 'terrainTypes',
        message: 'At least one terrain type is required.',
        code: 'NO_TERRAIN_TYPES'
      });
    }

    // Entity placement validation
    if (config.entities) {
      for (let i = 0; i < config.entities.length; i++) {
        const entity = config.entities[i];
        
        // Check for conflicting placement strategies
        if (entity.placementStrategy === 'near_walls' && 
            config.generationAlgorithm === 'rooms') {
          warnings.push({
            field: `entities[${i}].placementStrategy`,
            message: 'Near walls placement may not work well with rooms algorithm.',
            suggestion: 'Consider using "random" or "clustered" placement.'
          });
        }

        // Check entity distance constraints
        if (entity.minDistance && entity.maxDistanceFromPlayer && 
            entity.minDistance > entity.maxDistanceFromPlayer) {
          errors.push({
            field: `entities[${i}].minDistance`,
            message: 'Minimum distance cannot be greater than maximum distance from player.',
            code: 'INVALID_DISTANCE_CONSTRAINT'
          });
        }
      }
    }

    return { isValid: errors.length === 0, errors, warnings };
  }

  private validateWidth(value: any, config: Partial<GenerationConfig>): FieldValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];
    const suggestions: string[] = [];

    if (typeof value !== 'number') {
      errors.push({
        field: 'width',
        message: 'Width must be a number.',
        code: 'INVALID_TYPE'
      });
    } else {
      if (value < 10) {
        errors.push({
          field: 'width',
          message: 'Width must be at least 10.',
          code: 'VALUE_TOO_SMALL'
        });
      } else if (value > 1000) {
        errors.push({
          field: 'width',
          message: 'Width cannot exceed 1000.',
          code: 'VALUE_TOO_LARGE'
        });
      } else if (value > 200) {
        warnings.push({
          field: 'width',
          message: 'Large width may affect performance.',
          suggestion: 'Consider using a smaller width for better performance.'
        });
      }

      // Suggest optimal ratios
      if (config.height && Math.abs(value - config.height) > value * 0.5) {
        suggestions.push('Consider using similar width and height for balanced levels.');
      }
    }

    return {
      isValid: errors.length === 0,
      errors,
      warnings,
      suggestions
    };
  }

  private validateHeight(value: any, config: Partial<GenerationConfig>): FieldValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];
    const suggestions: string[] = [];

    if (typeof value !== 'number') {
      errors.push({
        field: 'height',
        message: 'Height must be a number.',
        code: 'INVALID_TYPE'
      });
    } else {
      if (value < 10) {
        errors.push({
          field: 'height',
          message: 'Height must be at least 10.',
          code: 'VALUE_TOO_SMALL'
        });
      } else if (value > 1000) {
        errors.push({
          field: 'height',
          message: 'Height cannot exceed 1000.',
          code: 'VALUE_TOO_LARGE'
        });
      } else if (value > 200) {
        warnings.push({
          field: 'height',
          message: 'Large height may affect performance.',
          suggestion: 'Consider using a smaller height for better performance.'
        });
      }

      // Suggest optimal ratios
      if (config.width && Math.abs(value - config.width) > value * 0.5) {
        suggestions.push('Consider using similar width and height for balanced levels.');
      }
    }

    return {
      isValid: errors.length === 0,
      errors,
      warnings,
      suggestions
    };
  }

  private validateSeed(value: any): FieldValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];
    const suggestions: string[] = [];

    if (typeof value !== 'number') {
      errors.push({
        field: 'seed',
        message: 'Seed must be a number.',
        code: 'INVALID_TYPE'
      });
    } else {
      if (!Number.isInteger(value)) {
        warnings.push({
          field: 'seed',
          message: 'Seed should be an integer for consistent results.',
          suggestion: 'Use Math.floor() to convert to integer.'
        });
      }

      if (value < 0) {
        suggestions.push('Negative seeds are valid but positive seeds are more common.');
      }
    }

    return {
      isValid: errors.length === 0,
      errors,
      warnings,
      suggestions
    };
  }

  private validateGenerationAlgorithm(value: any): FieldValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];
    const suggestions: string[] = [];

    if (typeof value !== 'string') {
      errors.push({
        field: 'generationAlgorithm',
        message: 'Generation algorithm must be a string.',
        code: 'INVALID_TYPE'
      });
    } else if (!GENERATION_ALGORITHMS.includes(value as any)) {
      errors.push({
        field: 'generationAlgorithm',
        message: `Invalid generation algorithm. Must be one of: ${GENERATION_ALGORITHMS.join(', ')}.`,
        code: 'INVALID_VALUE'
      });
      suggestions.push('Try "perlin" for natural terrain or "maze" for structured layouts.');
    }

    return {
      isValid: errors.length === 0,
      errors,
      warnings,
      suggestions
    };
  }

  private validateTerrainTypes(value: any): FieldValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];
    const suggestions: string[] = [];

    if (!Array.isArray(value)) {
      errors.push({
        field: 'terrainTypes',
        message: 'Terrain types must be an array.',
        code: 'INVALID_TYPE'
      });
    } else {
      if (value.length === 0) {
        errors.push({
          field: 'terrainTypes',
          message: 'At least one terrain type is required.',
          code: 'EMPTY_ARRAY'
        });
      }

      for (let i = 0; i < value.length; i++) {
        if (!TERRAIN_TYPES.includes(value[i])) {
          errors.push({
            field: 'terrainTypes',
            message: `Invalid terrain type "${value[i]}" at index ${i}.`,
            code: 'INVALID_ARRAY_ITEM'
          });
        }
      }

      if (value.length > 5) {
        warnings.push({
          field: 'terrainTypes',
          message: 'Many terrain types may make levels complex.',
          suggestion: 'Consider using 3-4 terrain types for clarity.'
        });
      }
    }

    return {
      isValid: errors.length === 0,
      errors,
      warnings,
      suggestions
    };
  }

  private validateEntities(value: any, config: Partial<GenerationConfig>): FieldValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];
    const suggestions: string[] = [];

    if (!Array.isArray(value)) {
      errors.push({
        field: 'entities',
        message: 'Entities must be an array.',
        code: 'INVALID_TYPE'
      });
    } else {
      // Validate each entity
      for (let i = 0; i < value.length; i++) {
        const entityResult = this.validateSingleEntity(value[i], i);
        errors.push(...entityResult.errors);
        warnings.push(...entityResult.warnings);
      }

      // Check for required entities
      const playerCount = value.filter(e => e.type === 'Player').length;
      if (playerCount === 0) {
        errors.push({
          field: 'entities',
          message: 'At least one Player entity is required.',
          code: 'MISSING_PLAYER'
        });
      }

      // Performance check
      const totalEntities = value.reduce((sum, entity) => sum + (entity.count || 0), 0);
      if (totalEntities > 100) {
        warnings.push({
          field: 'entities',
          message: 'High entity count may affect performance.',
          suggestion: 'Consider reducing entity counts.'
        });
      }
    }

    return {
      isValid: errors.length === 0,
      errors,
      warnings,
      suggestions
    };
  }

  private validateSingleEntity(entity: any, index: number): FieldValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];

    const isValid = validateEntityConfig(entity);
    if (!isValid && validateEntityConfig.errors) {
      for (const error of validateEntityConfig.errors) {
        errors.push({
          field: `entities[${index}].${error.instancePath.replace('/', '')}`,
          message: this.formatAjvError(error),
          code: this.getErrorCode(error)
        });
      }
    }

    return { isValid: errors.length === 0, errors, warnings };
  }

  private validateVisualTheme(value: any): FieldValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];

    const isValid = validateVisualThemeConfig(value);
    if (!isValid && validateVisualThemeConfig.errors) {
      for (const error of validateVisualThemeConfig.errors) {
        errors.push({
          field: `visualTheme.${error.instancePath.replace('/', '')}`,
          message: this.formatAjvError(error),
          code: this.getErrorCode(error)
        });
      }
    }

    return { isValid: errors.length === 0, errors, warnings };
  }

  private validateGameplay(value: any): FieldValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];

    const isValid = validateGameplayConfig(value);
    if (!isValid && validateGameplayConfig.errors) {
      for (const error of validateGameplayConfig.errors) {
        errors.push({
          field: `gameplay.${error.instancePath.replace('/', '')}`,
          message: this.formatAjvError(error),
          code: this.getErrorCode(error)
        });
      }
    }

    return { isValid: errors.length === 0, errors, warnings };
  }

  private validateNestedField(fieldPath: string, value: any, config: Partial<GenerationConfig>): FieldValidationResult {
    // Handle nested field validation like 'entities[0].count' or 'gameplay.playerSpeed'
    const parts = fieldPath.split('.');
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];

    // This is a simplified implementation - in a real app you'd want more sophisticated nested validation
    return { isValid: true, errors, warnings };
  }

  private formatAjvError(error: any): string {
    switch (error.keyword) {
      case 'required':
        return `Missing required field: ${error.params.missingProperty}`;
      case 'type':
        return `Expected ${error.params.type} but got ${typeof error.data}`;
      case 'minimum':
        return `Value must be at least ${error.params.limit}`;
      case 'maximum':
        return `Value must be at most ${error.params.limit}`;
      case 'enum':
        return `Value must be one of: ${error.params.allowedValues?.join(', ')}`;
      case 'minLength':
        return `Must be at least ${error.params.limit} characters long`;
      case 'maxLength':
        return `Must be at most ${error.params.limit} characters long`;
      case 'pattern':
        return 'Value does not match the required format';
      default:
        return error.message || 'Invalid value';
    }
  }

  private getErrorCode(error: any): string {
    return `AJV_${error.keyword?.toUpperCase() || 'VALIDATION_ERROR'}`;
  }

  /**
   * Get validation suggestions for a field
   */
  getFieldSuggestions(fieldPath: string, currentValue: any): string[] {
    const suggestions: string[] = [];

    switch (fieldPath) {
      case 'width':
      case 'height':
        suggestions.push('Try values between 20-100 for good performance');
        suggestions.push('Use square dimensions (same width and height) for balanced levels');
        break;
      case 'generationAlgorithm':
        suggestions.push('Use "perlin" for natural, organic terrain');
        suggestions.push('Use "cellular" for cave-like structures');
        suggestions.push('Use "maze" for structured, puzzle-like levels');
        suggestions.push('Use "rooms" for dungeon-style layouts');
        break;
      case 'terrainTypes':
        suggestions.push('Include "ground" as a basic walkable terrain');
        suggestions.push('Add "wall" for boundaries and obstacles');
        suggestions.push('Use 3-4 terrain types for visual variety without complexity');
        break;
    }

    return suggestions;
  }
}

export const validationService = ValidationService.getInstance();