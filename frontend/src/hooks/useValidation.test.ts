import { renderHook, act } from '@testing-library/react';
import { useValidation } from './useValidation';
import { GenerationConfig } from '../types';

// Mock the validation service
jest.mock('../services/validation', () => ({
  validationService: {
    validateConfiguration: jest.fn(),
    validateField: jest.fn(),
    getFieldSuggestions: jest.fn()
  }
}));

// Mock lodash debounce
jest.mock('lodash', () => ({
  debounce: (fn: any) => {
    fn.cancel = jest.fn();
    return fn;
  }
}));

import { validationService } from '../services/validation';

const mockValidationService = validationService as jest.Mocked<typeof validationService>;

describe('useValidation', () => {
  const validConfig: Partial<GenerationConfig> = {
    width: 50,
    height: 50,
    seed: 12345,
    generationAlgorithm: 'perlin',
    terrainTypes: ['ground', 'wall']
  };

  beforeEach(() => {
    jest.clearAllMocks();
    
    mockValidationService.validateConfiguration.mockReturnValue({
      isValid: true,
      errors: [],
      warnings: []
    });
    
    mockValidationService.validateField.mockReturnValue({
      isValid: true,
      errors: [],
      warnings: [],
      suggestions: []
    });
    
    mockValidationService.getFieldSuggestions.mockReturnValue([]);
  });

  it('should initialize with valid state', () => {
    const { result } = renderHook(() => useValidation(validConfig));

    expect(result.current.config).toEqual(validConfig);
    expect(result.current.isValid).toBe(true);
    expect(result.current.isValidating).toBe(false);
  });

  it('should validate on mount when enabled', () => {
    renderHook(() => useValidation(validConfig, { validateOnMount: true }));

    expect(mockValidationService.validateConfiguration).toHaveBeenCalledWith(
      validConfig,
      expect.any(Object)
    );
  });

  it('should not validate on mount when disabled', () => {
    renderHook(() => useValidation(validConfig, { validateOnMount: false }));

    expect(mockValidationService.validateConfiguration).not.toHaveBeenCalled();
  });

  it('should update configuration', () => {
    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.updateConfig({ width: 100 });
    });

    expect(result.current.config.width).toBe(100);
  });

  it('should validate on configuration change when enabled', () => {
    const { result } = renderHook(() => useValidation(validConfig, { 
      validateOnChange: true,
      validateOnMount: false 
    }));

    act(() => {
      result.current.updateConfig({ width: 100 });
    });

    expect(mockValidationService.validateConfiguration).toHaveBeenCalledWith(
      expect.objectContaining({ width: 100 }),
      expect.any(Object)
    );
  });

  it('should not validate on configuration change when disabled', () => {
    const { result } = renderHook(() => useValidation(validConfig, { 
      validateOnChange: false,
      validateOnMount: false 
    }));

    act(() => {
      result.current.updateConfig({ width: 100 });
    });

    expect(mockValidationService.validateConfiguration).not.toHaveBeenCalled();
  });

  it('should update individual fields', () => {
    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.updateField('width', 75);
    });

    expect(result.current.config.width).toBe(75);
  });

  it('should validate individual fields', () => {
    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.validateField('width', 25);
    });

    expect(mockValidationService.validateField).toHaveBeenCalledWith('width', 25, validConfig);
  });

  it('should handle validation errors', () => {
    mockValidationService.validateConfiguration.mockReturnValue({
      isValid: false,
      errors: [
        { field: 'width', message: 'Width is too small', code: 'WIDTH_TOO_SMALL' }
      ],
      warnings: []
    });

    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.validateConfiguration();
    });

    expect(result.current.isValid).toBe(false);
    expect(result.current.getFieldErrors('width')).toContain('Width is too small');
  });

  it('should handle validation warnings', () => {
    mockValidationService.validateConfiguration.mockReturnValue({
      isValid: true,
      errors: [],
      warnings: [
        { field: 'width', message: 'Width is large', suggestion: 'Consider smaller width' }
      ]
    });

    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.validateConfiguration();
    });

    expect(result.current.isValid).toBe(true);
    expect(result.current.getFieldWarnings('width')).toContain('Width is large');
  });

  it('should provide field suggestions', () => {
    mockValidationService.getFieldSuggestions.mockReturnValue(['Try values between 20-100']);

    const { result } = renderHook(() => useValidation(validConfig));

    const suggestions = result.current.getFieldSuggestions('width');

    expect(suggestions).toContain('Try values between 20-100');
    expect(mockValidationService.getFieldSuggestions).toHaveBeenCalledWith('width', 50);
  });

  it('should detect field errors', () => {
    mockValidationService.validateConfiguration.mockReturnValue({
      isValid: false,
      errors: [
        { field: 'width', message: 'Width is invalid', code: 'INVALID_WIDTH' }
      ],
      warnings: []
    });

    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.validateConfiguration();
    });

    expect(result.current.hasFieldError('width')).toBe(true);
    expect(result.current.hasFieldError('height')).toBe(false);
  });

  it('should detect field warnings', () => {
    mockValidationService.validateConfiguration.mockReturnValue({
      isValid: true,
      errors: [],
      warnings: [
        { field: 'height', message: 'Height is large', suggestion: 'Consider smaller height' }
      ]
    });

    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.validateConfiguration();
    });

    expect(result.current.hasFieldWarning('height')).toBe(true);
    expect(result.current.hasFieldWarning('width')).toBe(false);
  });

  it('should determine field status correctly', () => {
    mockValidationService.validateConfiguration.mockReturnValue({
      isValid: false,
      errors: [
        { field: 'width', message: 'Width error', code: 'WIDTH_ERROR' }
      ],
      warnings: [
        { field: 'height', message: 'Height warning', suggestion: 'Height suggestion' }
      ]
    });

    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.validateConfiguration();
    });

    expect(result.current.getFieldStatus('width')).toBe('error');
    expect(result.current.getFieldStatus('height')).toBe('warning');
    expect(result.current.getFieldStatus('seed')).toBe('valid');
  });

  it('should clear field validation', () => {
    mockValidationService.validateConfiguration.mockReturnValue({
      isValid: false,
      errors: [
        { field: 'width', message: 'Width error', code: 'WIDTH_ERROR' }
      ],
      warnings: []
    });

    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.validateConfiguration();
    });

    expect(result.current.hasFieldError('width')).toBe(true);

    act(() => {
      result.current.clearFieldValidation('width');
    });

    expect(result.current.hasFieldError('width')).toBe(false);
  });

  it('should clear all validation', () => {
    mockValidationService.validateConfiguration.mockReturnValue({
      isValid: false,
      errors: [
        { field: 'width', message: 'Width error', code: 'WIDTH_ERROR' }
      ],
      warnings: [
        { field: 'height', message: 'Height warning', suggestion: 'Height suggestion' }
      ]
    });

    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.validateConfiguration();
    });

    expect(result.current.isValid).toBe(false);

    act(() => {
      result.current.clearValidation();
    });

    expect(result.current.isValid).toBe(true);
    expect(result.current.hasFieldError('width')).toBe(false);
    expect(result.current.hasFieldWarning('height')).toBe(false);
  });

  it('should provide validation summary', () => {
    mockValidationService.validateConfiguration.mockReturnValue({
      isValid: false,
      errors: [
        { field: 'width', message: 'Width error', code: 'WIDTH_ERROR' },
        { field: 'height', message: 'Height error', code: 'HEIGHT_ERROR' }
      ],
      warnings: [
        { field: 'seed', message: 'Seed warning', suggestion: 'Seed suggestion' }
      ]
    });

    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.validateConfiguration();
    });

    const summary = result.current.getValidationSummary();

    expect(summary.isValid).toBe(false);
    expect(summary.errorCount).toBe(2);
    expect(summary.warningCount).toBe(1);
    expect(summary.hasErrors).toBe(true);
    expect(summary.hasWarnings).toBe(true);
  });

  it('should handle nested field updates', () => {
    const { result } = renderHook(() => useValidation({
      ...validConfig,
      entities: [
        {
          type: 'Player',
          count: 1,
          minDistance: 0,
          maxDistanceFromPlayer: 0,
          properties: {},
          placementStrategy: 'center'
        }
      ]
    }));

    act(() => {
      result.current.updateField('entities[0].count', 5);
    });

    expect(result.current.config.entities?.[0]?.count).toBe(5);
  });

  it('should handle array field updates', () => {
    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.updateField('terrainTypes', ['ground', 'wall', 'water']);
    });

    expect(result.current.config.terrainTypes).toEqual(['ground', 'wall', 'water']);
  });

  it('should force validation', () => {
    const { result } = renderHook(() => useValidation(validConfig, { 
      validateOnMount: false 
    }));

    expect(mockValidationService.validateConfiguration).not.toHaveBeenCalled();

    act(() => {
      result.current.forceValidation();
    });

    expect(mockValidationService.validateConfiguration).toHaveBeenCalled();
  });

  it('should handle functional config updates', () => {
    const { result } = renderHook(() => useValidation(validConfig));

    act(() => {
      result.current.updateConfig(prev => ({ ...prev, width: prev.width! + 10 }));
    });

    expect(result.current.config.width).toBe(60);
  });

  it('should handle validation with cross-field validation enabled', () => {
    const { result } = renderHook(() => useValidation(validConfig, {
      crossFieldValidation: true,
      validateOnMount: false
    }));

    act(() => {
      result.current.updateField('width', 25);
    });

    expect(mockValidationService.validateField).toHaveBeenCalledWith('width', 25, expect.any(Object));
    expect(mockValidationService.validateConfiguration).toHaveBeenCalled();
  });
});