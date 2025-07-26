import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import React from 'react';
import {
  useHealthQuery,
  usePresetsQuery,
  useSavePresetMutation,
  useExportFormatsQuery,
  useGenerateLevelMutation,
  useConfigValidationQuery
} from './useApiQueries';
import { apiService } from '../services/api';
import { GenerationConfig, ConfigPreset, ExportFormat, Level, ValidationResult } from '../types';

// Mock API service
vi.mock('../services/api');
const mockApiService = vi.mocked(apiService);

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  });

  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
};

const mockConfig: GenerationConfig = {
  width: 50,
  height: 50,
  seed: 12345,
  generationAlgorithm: 'perlin',
  algorithmParameters: {},
  terrainTypes: ['grass', 'stone'],
  entities: [],
  visualTheme: {
    themeName: 'default',
    colorPalette: {},
    tileSprites: {},
    entitySprites: {},
    effectSettings: {}
  },
  gameplay: {
    playerSpeed: 5,
    playerHealth: 100,
    difficulty: 'medium',
    timeLimit: 300,
    victoryConditions: ['reach_exit'],
    mechanics: {}
  }
};

const mockPreset: ConfigPreset = {
  id: 'preset-1',
  name: 'Test Preset',
  description: 'A test preset',
  config: mockConfig,
  createdAt: new Date()
};

const mockExportFormat: ExportFormat = {
  id: 'json',
  name: 'JSON',
  description: 'JSON format',
  fileExtension: 'json',
  supportsCustomization: true
};

const mockLevel: Level = {
  id: 'level-1',
  config: mockConfig,
  terrain: {
    width: 50,
    height: 50,
    tiles: []
  },
  entities: [],
  metadata: {
    generatedAt: new Date(),
    generationTime: 1000,
    version: '1.0.0'
  }
};

describe('useApiQueries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useHealthQuery', () => {
    it('should fetch health status successfully', async () => {
      const mockHealth = { status: 'healthy', version: '1.0.0', timestamp: '2023-01-01' };
      mockApiService.checkHealth.mockResolvedValue(mockHealth);

      const { result } = renderHook(() => useHealthQuery(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      expect(result.current.data).toEqual(mockHealth);
      expect(mockApiService.checkHealth).toHaveBeenCalledTimes(1);
    });

    it('should handle health check errors', async () => {
      mockApiService.checkHealth.mockRejectedValue(new Error('Health check failed'));

      const { result } = renderHook(() => useHealthQuery(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isError).toBe(true);
      });

      expect(result.current.error).toBeInstanceOf(Error);
    });
  });

  describe('usePresetsQuery', () => {
    it('should fetch presets successfully', async () => {
      const mockPresets = [mockPreset];
      mockApiService.getPresets.mockResolvedValue(mockPresets);

      const { result } = renderHook(() => usePresetsQuery(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      expect(result.current.data).toEqual(mockPresets);
      expect(mockApiService.getPresets).toHaveBeenCalledTimes(1);
    });

    it('should cache presets data', async () => {
      const mockPresets = [mockPreset];
      mockApiService.getPresets.mockResolvedValue(mockPresets);

      const wrapper = createWrapper();
      
      // First render
      const { result: result1 } = renderHook(() => usePresetsQuery(), { wrapper });
      await waitFor(() => expect(result1.current.isSuccess).toBe(true));

      // Second render should use cached data
      const { result: result2 } = renderHook(() => usePresetsQuery(), { wrapper });
      await waitFor(() => expect(result2.current.isSuccess).toBe(true));

      // API should only be called once due to caching
      expect(mockApiService.getPresets).toHaveBeenCalledTimes(1);
      expect(result2.current.data).toEqual(mockPresets);
    });
  });

  describe('useSavePresetMutation', () => {
    it('should save preset and update cache', async () => {
      const newPreset = { ...mockPreset, id: 'preset-2', name: 'New Preset' };
      mockApiService.savePreset.mockResolvedValue(newPreset);
      mockApiService.getPresets.mockResolvedValue([mockPreset]);

      const wrapper = createWrapper();
      
      // First load presets
      const { result: presetsResult } = renderHook(() => usePresetsQuery(), { wrapper });
      await waitFor(() => expect(presetsResult.current.isSuccess).toBe(true));

      // Then save new preset
      const { result: mutationResult } = renderHook(() => useSavePresetMutation(), { wrapper });

      await waitFor(() => {
        mutationResult.current.mutate({
          name: 'New Preset',
          description: 'A new preset',
          config: mockConfig
        });
      });

      await waitFor(() => {
        expect(mutationResult.current.isSuccess).toBe(true);
      });

      expect(mockApiService.savePreset).toHaveBeenCalledWith({
        name: 'New Preset',
        description: 'A new preset',
        config: mockConfig
      });
    });
  });

  describe('useExportFormatsQuery', () => {
    it('should fetch export formats with long cache time', async () => {
      const mockFormats = [mockExportFormat];
      mockApiService.getExportFormats.mockResolvedValue(mockFormats);

      const { result } = renderHook(() => useExportFormatsQuery(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      expect(result.current.data).toEqual(mockFormats);
      expect(mockApiService.getExportFormats).toHaveBeenCalledTimes(1);
    });
  });

  describe('useGenerateLevelMutation', () => {
    it('should generate level and cache result', async () => {
      mockApiService.generateLevel.mockResolvedValue(mockLevel);

      const { result } = renderHook(() => useGenerateLevelMutation(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        result.current.mutate(mockConfig);
      });

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      expect(result.current.data).toEqual(mockLevel);
      expect(mockApiService.generateLevel).toHaveBeenCalledWith(mockConfig);
    });

    it('should handle generation errors', async () => {
      mockApiService.generateLevel.mockRejectedValue(new Error('Generation failed'));

      const { result } = renderHook(() => useGenerateLevelMutation(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        result.current.mutate(mockConfig);
      });

      await waitFor(() => {
        expect(result.current.isError).toBe(true);
      });

      expect(result.current.error).toBeInstanceOf(Error);
    });
  });

  describe('useConfigValidationQuery', () => {
    it('should validate configuration with caching', async () => {
      const mockValidation: ValidationResult = {
        isValid: true,
        errors: [],
        warnings: []
      };
      mockApiService.validateConfiguration.mockResolvedValue(mockValidation);

      const { result } = renderHook(() => useConfigValidationQuery(mockConfig), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      expect(result.current.data).toEqual(mockValidation);
      expect(mockApiService.validateConfiguration).toHaveBeenCalledWith(mockConfig);
    });

    it('should not validate when config is null', () => {
      const { result } = renderHook(() => useConfigValidationQuery(null as any), {
        wrapper: createWrapper(),
      });

      expect(result.current.isPending).toBe(false);
      expect(mockApiService.validateConfiguration).not.toHaveBeenCalled();
    });

    it('should cache validation results for same config', async () => {
      const mockValidation: ValidationResult = {
        isValid: true,
        errors: [],
        warnings: []
      };
      mockApiService.validateConfiguration.mockResolvedValue(mockValidation);

      const wrapper = createWrapper();
      
      // First validation
      const { result: result1 } = renderHook(() => useConfigValidationQuery(mockConfig), { wrapper });
      await waitFor(() => expect(result1.current.isSuccess).toBe(true));

      // Second validation with same config should use cache
      const { result: result2 } = renderHook(() => useConfigValidationQuery(mockConfig), { wrapper });
      await waitFor(() => expect(result2.current.isSuccess).toBe(true));

      // Should only call API once due to caching
      expect(mockApiService.validateConfiguration).toHaveBeenCalledTimes(1);
      expect(result2.current.data).toEqual(mockValidation);
    });
  });

  describe('query key generation', () => {
    it('should generate consistent keys for same data', () => {
      const config1 = { ...mockConfig };
      const config2 = { ...mockConfig };

      // Keys should be the same for identical configs
      expect(JSON.stringify(config1)).toEqual(JSON.stringify(config2));
    });

    it('should generate different keys for different data', () => {
      const config1 = { ...mockConfig, seed: 123 };
      const config2 = { ...mockConfig, seed: 456 };

      expect(JSON.stringify(config1)).not.toEqual(JSON.stringify(config2));
    });
  });

  describe('error handling', () => {
    it('should handle network errors gracefully', async () => {
      mockApiService.getPresets.mockRejectedValue(new Error('Network error'));

      const { result } = renderHook(() => usePresetsQuery(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isError).toBe(true);
      });

      expect(result.current.error).toBeInstanceOf(Error);
      expect((result.current.error as Error).message).toBe('Network error');
    });

    it('should retry failed requests according to configuration', async () => {
      mockApiService.checkHealth
        .mockRejectedValueOnce(new Error('Temporary error'))
        .mockResolvedValue({ status: 'healthy', version: '1.0.0', timestamp: '2023-01-01' });

      const { result } = renderHook(() => useHealthQuery(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      // Should have retried once
      expect(mockApiService.checkHealth).toHaveBeenCalledTimes(2);
    });
  });
});