import { validationService, _ValidationOptions } from './validation';
import { GenerationConfig } from '../types';

describe('ValidationService', () => {
  const validConfig: GenerationConfig = {
    width: 50,
    height: 50,
    seed: 12345,
    generationAlgorithm: 'perlin',
    algorithmParameters: {},
    terrainTypes: ['ground', 'wall', 'water'],
    entities: [
      {
        type: 'Player',
        count: 1,
        minDistance: 0,
        maxDistanceFromPlayer: 0,
        properties: {},
        placementStrategy: 'center'
      }
    ],
    visualTheme: {
      themeName: 'default',
      colorPalette: { ground: '#8B4513', wall: '#696969', water: '#4169E1' },
      tileSprites: {},
      entitySprites: {},
      effectSettings: {}
    },
    gameplay: {
      playerSpeed: 5,
      playerHealth: 100,
      difficulty: 'normal',
      timeLimit: 300,
      victoryConditions: ['reach_exit'],
      mechanics: {}
    }
  };

  describe('validateConfiguration', () => {
    it('should validate a correct configuration', () => {
      const result = validationService.validateConfiguration(validConfig);
      
      expect(result.isValid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    it('should detect missing required fields', () => {
      const invalidConfig = { ...validConfig };
      delete (invalidConfig as any).width;
      
      const result = validationService.validateConfiguration(invalidConfig);
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'width',
          code: expect.stringContaining('REQUIRED')
        })
      );
    });

    it('should validate width constraints', () => {
      const invalidConfig = { ...validConfig, width: 5 };
      
      const result = validationService.validateConfiguration(invalidConfig);
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'width',
          message: expect.stringContaining('at least 10')
        })
      );
    });

    it('should validate height constraints', () => {
      const invalidConfig = { ...validConfig, height: 1500 };
      
      const result = validationService.validateConfiguration(invalidConfig);
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'height',
          message: expect.stringContaining('cannot exceed 1000')
        })
      );
    });

    it('should validate generation algorithm', () => {
      const invalidConfig = { ...validConfig, generationAlgorithm: 'invalid-algorithm' };
      
      const result = validationService.validateConfiguration(invalidConfig);
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'generationAlgorithm',
          code: 'INVALID_VALUE'
        })
      );
    });

    it('should validate terrain types', () => {
      const invalidConfig = { ...validConfig, terrainTypes: [] };
      
      const result = validationService.validateConfiguration(invalidConfig);
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'terrainTypes',
          code: 'EMPTY_ARRAY'
        })
      );
    });

    it('should validate entity requirements', () => {
      const invalidConfig = { ...validConfig, entities: [] };
      
      const result = validationService.validateConfiguration(invalidConfig);
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'entities',
          code: 'MISSING_PLAYER'
        })
      );
    });

    it('should perform cross-field validation', () => {
      const invalidConfig = {
        ...validConfig,
        width: 10,
        height: 10,
        entities: [
          {
            type: 'Player',
            count: 1,
            minDistance: 0,
            maxDistanceFromPlayer: 0,
            properties: {},
            placementStrategy: 'center'
          },
          {
            type: 'Enemy',
            count: 100, // Too many for small level
            minDistance: 1,
            maxDistanceFromPlayer: 10,
            properties: {},
            placementStrategy: 'random'
          }
        ]
      };
      
      const result = validationService.validateConfiguration(invalidConfig, {
        crossFieldValidation: true
      });
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'entities',
          code: 'ENTITY_DENSITY_TOO_HIGH'
        })
      );
    });

    it('should validate victory conditions and required entities', () => {
      const invalidConfig = {
        ...validConfig,
        gameplay: {
          ...validConfig.gameplay,
          victoryConditions: ['reach_exit']
        },
        entities: [
          {
            type: 'Player',
            count: 1,
            minDistance: 0,
            maxDistanceFromPlayer: 0,
            properties: {},
            placementStrategy: 'center'
          }
          // Missing Exit entity for reach_exit victory condition
        ]
      };
      
      const result = validationService.validateConfiguration(invalidConfig, {
        crossFieldValidation: true
      });
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'entities',
          code: 'MISSING_EXIT_ENTITY'
        })
      );
    });

    it('should provide performance warnings', () => {
      const largeConfig = {
        ...validConfig,
        width: 300,
        height: 300
      };
      
      const result = validationService.validateConfiguration(largeConfig, {
        performanceCheck: true,
        includeWarnings: true
      });
      
      expect(result.warnings.length).toBeGreaterThan(0);
      expect(result.warnings).toContainEqual(
        expect.objectContaining({
          field: expect.stringMatching(/width|height/),
          message: expect.stringContaining('performance')
        })
      );
    });

    it('should handle strict mode', () => {
      const configWithWarnings = {
        ...validConfig,
        width: 250, // This should generate a warning
        height: 250
      };
      
      const result = validationService.validateConfiguration(configWithWarnings, {
        strict: true,
        performanceCheck: true
      });
      
      expect(result.isValid).toBe(false); // Warnings become errors in strict mode
      expect(result.errors.length).toBeGreaterThan(0);
    });
  });

  describe('validateField', () => {
    it('should validate width field', () => {
      const result = validationService.validateField('width', 25);
      
      expect(result.isValid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    it('should validate invalid width', () => {
      const result = validationService.validateField('width', 5);
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'width',
          code: 'VALUE_TOO_SMALL'
        })
      );
    });

    it('should validate height field', () => {
      const result = validationService.validateField('height', 'not-a-number');
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'height',
          code: 'INVALID_TYPE'
        })
      );
    });

    it('should validate seed field', () => {
      const result = validationService.validateField('seed', 12345.67);
      
      expect(result.isValid).toBe(true);
      expect(result.warnings).toContainEqual(
        expect.objectContaining({
          field: 'seed',
          message: expect.stringContaining('integer')
        })
      );
    });

    it('should validate generation algorithm field', () => {
      const result = validationService.validateField('generationAlgorithm', 'invalid');
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'generationAlgorithm',
          code: 'INVALID_VALUE'
        })
      );
    });

    it('should validate terrain types field', () => {
      const result = validationService.validateField('terrainTypes', ['ground', 'invalid-terrain']);
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          field: 'terrainTypes',
          code: 'INVALID_ARRAY_ITEM'
        })
      );
    });

    it('should validate entities field', () => {
      const invalidEntities = [
        {
          type: 'InvalidType',
          count: 5,
          minDistance: 1,
          maxDistanceFromPlayer: 10,
          properties: {},
          placementStrategy: 'random'
        }
      ];
      
      const result = validationService.validateField('entities', invalidEntities);
      
      expect(result.isValid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
    });
  });

  describe('getFieldSuggestions', () => {
    it('should provide suggestions for width field', () => {
      const suggestions = validationService.getFieldSuggestions('width', 500);
      
      expect(suggestions.length).toBeGreaterThan(0);
      expect(suggestions).toContain(expect.stringContaining('20-100'));
    });

    it('should provide suggestions for generation algorithm', () => {
      const suggestions = validationService.getFieldSuggestions('generationAlgorithm', 'perlin');
      
      expect(suggestions.length).toBeGreaterThan(0);
      expect(suggestions).toContain(expect.stringContaining('perlin'));
    });

    it('should provide suggestions for terrain types', () => {
      const suggestions = validationService.getFieldSuggestions('terrainTypes', ['ground']);
      
      expect(suggestions.length).toBeGreaterThan(0);
      expect(suggestions).toContain(expect.stringContaining('ground'));
    });
  });

  describe('edge cases', () => {
    it('should handle null configuration', () => {
      const result = validationService.validateConfiguration(null as any);
      
      expect(result.isValid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
    });

    it('should handle undefined configuration', () => {
      const result = validationService.validateConfiguration(undefined as any);
      
      expect(result.isValid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
    });

    it('should handle empty configuration', () => {
      const result = validationService.validateConfiguration({} as any);
      
      expect(result.isValid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
    });

    it('should handle configuration with extra properties', () => {
      const configWithExtra = {
        ...validConfig,
        extraProperty: 'should be ignored'
      };
      
      const result = validationService.validateConfiguration(configWithExtra);
      
      // Should still be valid, extra properties are typically ignored
      expect(result.isValid).toBe(true);
    });
  });

  describe('performance validation', () => {
    it('should detect memory-intensive configurations', () => {
      const memoryIntensiveConfig = {
        ...validConfig,
        width: 500,
        height: 500,
        entities: Array(20).fill(null).map((_, _i) => ({
          type: 'Enemy',
          count: 50,
          minDistance: 1,
          maxDistanceFromPlayer: 100,
          properties: {},
          placementStrategy: 'random'
        }))
      };
      
      const result = validationService.validateConfiguration(memoryIntensiveConfig, {
        performanceCheck: true
      });
      
      expect(result.isValid).toBe(false);
      expect(result.errors).toContainEqual(
        expect.objectContaining({
          code: expect.stringMatching(/LARGE|MANY|PERFORMANCE/)
        })
      );
    });

    it('should warn about algorithm complexity', () => {
      const complexConfig = {
        ...validConfig,
        width: 200,
        height: 200,
        generationAlgorithm: 'maze'
      };
      
      const result = validationService.validateConfiguration(complexConfig, {
        performanceCheck: true,
        includeWarnings: true
      });
      
      expect(result.warnings).toContainEqual(
        expect.objectContaining({
          field: 'generationAlgorithm',
          message: expect.stringContaining('slow')
        })
      );
    });
  });
});