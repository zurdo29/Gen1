import { useState, useCallback, useEffect, useMemo } from 'react';
import { validationService, ValidationOptions, FieldValidationResult } from '../services/validation';
import { GenerationConfig, ValidationResult } from '../types';
import { debounce } from 'lodash';

export interface UseValidationOptions extends ValidationOptions {
  validateOnChange?: boolean;
  debounceMs?: number;
  validateOnMount?: boolean;
}

export interface ValidationState {
  isValid: boolean;
  isValidating: boolean;
  errors: Record<string, string[]>;
  warnings: Record<string, string[]>;
  suggestions: Record<string, string[]>;
  fieldResults: Record<string, FieldValidationResult>;
  overallResult: ValidationResult | null;
}

export const useValidation = (
  initialConfig: Partial<GenerationConfig> = {},
  options: UseValidationOptions = {}
) => {
  const {
    validateOnChange = true,
    debounceMs = 300,
    validateOnMount = true,
    ...validationOptions
  } = options;

  const [config, setConfig] = useState<Partial<GenerationConfig>>(initialConfig);
  const [validationState, setValidationState] = useState<ValidationState>({
    isValid: true,
    isValidating: false,
    errors: {},
    warnings: {},
    suggestions: {},
    fieldResults: {},
    overallResult: null
  });

  // Debounced validation function
  const debouncedValidate = useMemo(
    () => debounce((configToValidate: Partial<GenerationConfig>) => {
      setValidationState(prev => ({ ...prev, isValidating: true }));
      
      const result = validationService.validateConfiguration(configToValidate, validationOptions);
      
      // Group errors and warnings by field
      const errors: Record<string, string[]> = {};
      const warnings: Record<string, string[]> = {};
      
      result.errors.forEach(error => {
        if (!errors[error.field]) errors[error.field] = [];
        errors[error.field].push(error.message);
      });
      
      result.warnings.forEach(warning => {
        if (!warnings[warning.field]) warnings[warning.field] = [];
        warnings[warning.field].push(warning.message);
      });

      setValidationState({
        isValid: result.isValid,
        isValidating: false,
        errors,
        warnings,
        suggestions: {},
        fieldResults: {},
        overallResult: result
      });
    }, debounceMs),
    [validationOptions, debounceMs]
  );

  // Validate configuration
  const validateConfiguration = useCallback((configToValidate?: Partial<GenerationConfig>) => {
    const targetConfig = configToValidate || config;
    debouncedValidate(targetConfig);
  }, [config, debouncedValidate]);

  // Validate individual field
  const validateField = useCallback((fieldPath: string, value: any) => {
    const result = validationService.validateField(fieldPath, value, config);
    
    setValidationState(prev => ({
      ...prev,
      fieldResults: {
        ...prev.fieldResults,
        [fieldPath]: result
      },
      errors: {
        ...prev.errors,
        [fieldPath]: result.errors.map(e => e.message)
      },
      warnings: {
        ...prev.warnings,
        [fieldPath]: result.warnings.map(w => w.message)
      },
      suggestions: {
        ...prev.suggestions,
        [fieldPath]: result.suggestions || []
      }
    }));

    return result;
  }, [config]);

  // Update configuration and validate if needed
  const updateConfig = useCallback((
    updates: Partial<GenerationConfig> | ((prev: Partial<GenerationConfig>) => Partial<GenerationConfig>)
  ) => {
    const newConfig = typeof updates === 'function' ? updates(config) : { ...config, ...updates };
    setConfig(newConfig);
    
    if (validateOnChange) {
      validateConfiguration(newConfig);
    }
  }, [config, validateOnChange, validateConfiguration]);

  // Update single field
  const updateField = useCallback((fieldPath: string, value: any) => {
    const fieldParts = fieldPath.split('.');
    const newConfig = { ...config };
    
    // Handle nested field updates
    let current: any = newConfig;
    for (let i = 0; i < fieldParts.length - 1; i++) {
      const part = fieldParts[i];
      if (part.includes('[') && part.includes(']')) {
        // Handle array indices like 'entities[0]'
        const [arrayName, indexStr] = part.split('[');
        const index = parseInt(indexStr.replace(']', ''));
        if (!current[arrayName]) current[arrayName] = [];
        if (!current[arrayName][index]) current[arrayName][index] = {};
        current = current[arrayName][index];
      } else {
        if (!current[part]) current[part] = {};
        current = current[part];
      }
    }
    
    const lastPart = fieldParts[fieldParts.length - 1];
    if (lastPart.includes('[') && lastPart.includes(']')) {
      const [arrayName, indexStr] = lastPart.split('[');
      const index = parseInt(indexStr.replace(']', ''));
      if (!current[arrayName]) current[arrayName] = [];
      current[arrayName][index] = value;
    } else {
      current[lastPart] = value;
    }
    
    setConfig(newConfig);
    
    // Validate the specific field
    validateField(fieldPath, value);
    
    // Validate entire config if cross-field validation is enabled
    if (validateOnChange && validationOptions.crossFieldValidation) {
      validateConfiguration(newConfig);
    }
  }, [config, validateField, validateOnChange, validateConfiguration, validationOptions.crossFieldValidation]);

  // Get field error messages
  const getFieldErrors = useCallback((fieldPath: string): string[] => {
    return validationState.errors[fieldPath] || [];
  }, [validationState.errors]);

  // Get field warning messages
  const getFieldWarnings = useCallback((fieldPath: string): string[] => {
    return validationState.warnings[fieldPath] || [];
  }, [validationState.warnings]);

  // Get field suggestions
  const getFieldSuggestions = useCallback((fieldPath: string): string[] => {
    const validationSuggestions = validationState.suggestions[fieldPath] || [];
    const serviceSuggestions = validationService.getFieldSuggestions(fieldPath, getFieldValue(fieldPath, config));
    return [...validationSuggestions, ...serviceSuggestions];
  }, [validationState.suggestions, config]);

  // Check if field has errors
  const hasFieldError = useCallback((fieldPath: string): boolean => {
    return (validationState.errors[fieldPath]?.length || 0) > 0;
  }, [validationState.errors]);

  // Check if field has warnings
  const hasFieldWarning = useCallback((fieldPath: string): boolean => {
    return (validationState.warnings[fieldPath]?.length || 0) > 0;
  }, [validationState.warnings]);

  // Get field validation status
  const getFieldStatus = useCallback((fieldPath: string): 'valid' | 'warning' | 'error' => {
    if (hasFieldError(fieldPath)) return 'error';
    if (hasFieldWarning(fieldPath)) return 'warning';
    return 'valid';
  }, [hasFieldError, hasFieldWarning]);

  // Clear field validation
  const clearFieldValidation = useCallback((fieldPath: string) => {
    setValidationState(prev => {
      const newErrors = { ...prev.errors };
      const newWarnings = { ...prev.warnings };
      const newSuggestions = { ...prev.suggestions };
      const newFieldResults = { ...prev.fieldResults };
      
      delete newErrors[fieldPath];
      delete newWarnings[fieldPath];
      delete newSuggestions[fieldPath];
      delete newFieldResults[fieldPath];
      
      return {
        ...prev,
        errors: newErrors,
        warnings: newWarnings,
        suggestions: newSuggestions,
        fieldResults: newFieldResults
      };
    });
  }, []);

  // Clear all validation
  const clearValidation = useCallback(() => {
    setValidationState({
      isValid: true,
      isValidating: false,
      errors: {},
      warnings: {},
      suggestions: {},
      fieldResults: {},
      overallResult: null
    });
  }, []);

  // Force validation
  const forceValidation = useCallback(() => {
    validateConfiguration();
  }, [validateConfiguration]);

  // Get validation summary
  const getValidationSummary = useCallback(() => {
    const totalErrors = Object.values(validationState.errors).reduce((sum, errors) => sum + errors.length, 0);
    const totalWarnings = Object.values(validationState.warnings).reduce((sum, warnings) => sum + warnings.length, 0);
    
    return {
      isValid: validationState.isValid,
      errorCount: totalErrors,
      warningCount: totalWarnings,
      hasErrors: totalErrors > 0,
      hasWarnings: totalWarnings > 0,
      isValidating: validationState.isValidating
    };
  }, [validationState]);

  // Validate on mount if enabled
  useEffect(() => {
    if (validateOnMount) {
      validateConfiguration();
    }
  }, [validateOnMount, validateConfiguration]);

  // Cleanup debounced function on unmount
  useEffect(() => {
    return () => {
      debouncedValidate.cancel();
    };
  }, [debouncedValidate]);

  return {
    // Configuration state
    config,
    updateConfig,
    updateField,
    
    // Validation state
    validationState,
    isValid: validationState.isValid,
    isValidating: validationState.isValidating,
    
    // Validation functions
    validateConfiguration,
    validateField,
    forceValidation,
    
    // Field-specific functions
    getFieldErrors,
    getFieldWarnings,
    getFieldSuggestions,
    hasFieldError,
    hasFieldWarning,
    getFieldStatus,
    
    // Utility functions
    clearFieldValidation,
    clearValidation,
    getValidationSummary
  };
};

// Helper function to get nested field value
function getFieldValue(fieldPath: string, config: any): any {
  const parts = fieldPath.split('.');
  let current = config;
  
  for (const part of parts) {
    if (part.includes('[') && part.includes(']')) {
      const [arrayName, indexStr] = part.split('[');
      const index = parseInt(indexStr.replace(']', ''));
      current = current?.[arrayName]?.[index];
    } else {
      current = current?.[part];
    }
    
    if (current === undefined) break;
  }
  
  return current;
}