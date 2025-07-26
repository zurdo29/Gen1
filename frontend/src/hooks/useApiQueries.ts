import { useQuery, useMutation, useQueryClient, UseQueryOptions, UseMutationOptions } from '@tanstack/react-query';
import { 
  GenerationConfig, 
  Level, 
  ValidationResult, 
  ConfigPreset, 
  ShareResult, 
  ExportFormat,
  ExportOptions,
  JobStatus,
  BatchGenerationRequest
} from '../types';
import { apiService } from '../services/api';

// Query Keys
export const queryKeys = {
  health: ['health'] as const,
  presets: ['presets'] as const,
  exportFormats: ['export', 'formats'] as const,
  sharedConfig: (shareId: string) => ['shared-config', shareId] as const,
  jobStatus: (jobId: string) => ['job-status', jobId] as const,
  batchExportStatus: (jobId: string) => ['batch-export-status', jobId] as const,
  levelGeneration: (config: GenerationConfig) => ['level-generation', config] as const,
  configValidation: (config: GenerationConfig) => ['config-validation', config] as const,
} as const;

// Cache configuration
const CACHE_TIMES = {
  short: 1000 * 60 * 5, // 5 minutes
  medium: 1000 * 60 * 15, // 15 minutes
  long: 1000 * 60 * 60, // 1 hour
  veryLong: 1000 * 60 * 60 * 24, // 24 hours
} as const;

// Health Check Query
export const useHealthQuery = (options?: UseQueryOptions<{ status: string; version: string; timestamp: string }>) => {
  return useQuery({
    queryKey: queryKeys.health,
    queryFn: () => apiService.checkHealth(),
    staleTime: CACHE_TIMES.short,
    gcTime: CACHE_TIMES.medium,
    retry: 3,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    ...options,
  });
};

// Presets Queries
export const usePresetsQuery = (options?: UseQueryOptions<ConfigPreset[]>) => {
  return useQuery({
    queryKey: queryKeys.presets,
    queryFn: () => apiService.getPresets(),
    staleTime: CACHE_TIMES.medium,
    gcTime: CACHE_TIMES.long,
    ...options,
  });
};

export const useSavePresetMutation = (options?: UseMutationOptions<ConfigPreset, Error, Omit<ConfigPreset, 'id' | 'createdAt'>>) => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (preset) => apiService.savePreset(preset),
    onSuccess: (newPreset) => {
      // Update the presets cache
      queryClient.setQueryData(queryKeys.presets, (old: ConfigPreset[] | undefined) => {
        if (!old) return [newPreset];
        return [...old, newPreset];
      });
    },
    ...options,
  });
};

// Export Format Query
export const useExportFormatsQuery = (options?: UseQueryOptions<ExportFormat[]>) => {
  return useQuery({
    queryKey: queryKeys.exportFormats,
    queryFn: () => apiService.getExportFormats(),
    staleTime: CACHE_TIMES.long,
    gcTime: CACHE_TIMES.veryLong,
    ...options,
  });
};

// Shared Configuration Query
export const useSharedConfigQuery = (shareId: string, options?: UseQueryOptions<GenerationConfig>) => {
  return useQuery({
    queryKey: queryKeys.sharedConfig(shareId),
    queryFn: () => apiService.getSharedConfiguration(shareId),
    enabled: !!shareId,
    staleTime: CACHE_TIMES.long,
    gcTime: CACHE_TIMES.veryLong,
    retry: 1, // Don't retry too much for shared configs
    ...options,
  });
};

// Share Configuration Mutation
export const useCreateShareLinkMutation = (options?: UseMutationOptions<ShareResult, Error, { config: GenerationConfig; expiryDays?: number }>) => {
  return useMutation({
    mutationFn: ({ config, expiryDays }) => apiService.createShareLink(config, expiryDays),
    ...options,
  });
};

// Job Status Query (with polling)
export const useJobStatusQuery = (jobId: string, options?: UseQueryOptions<JobStatus>) => {
  return useQuery({
    queryKey: queryKeys.jobStatus(jobId),
    queryFn: () => apiService.getJobStatus(jobId),
    enabled: !!jobId,
    refetchInterval: (query) => {
      // Stop polling when job is completed or failed
      const data = query.state.data;
      if (data?.status === 'completed' || data?.status === 'failed') {
        return false;
      }
      return 1000; // Poll every second
    },
    staleTime: 0, // Always fetch fresh data for job status
    gcTime: CACHE_TIMES.short,
    ...options,
  });
};

// Batch Export Status Query (with polling)
export const useBatchExportStatusQuery = (jobId: string, options?: UseQueryOptions<JobStatus>) => {
  return useQuery({
    queryKey: queryKeys.batchExportStatus(jobId),
    queryFn: () => apiService.getJobStatus(jobId), // Uses same endpoint
    enabled: !!jobId,
    refetchInterval: (query) => {
      const data = query.state.data;
      if (data?.status === 'completed' || data?.status === 'failed') {
        return false;
      }
      return 1000;
    },
    staleTime: 0,
    gcTime: CACHE_TIMES.short,
    ...options,
  });
};

// Level Generation Mutation with caching
export const useGenerateLevelMutation = (options?: UseMutationOptions<Level, Error, GenerationConfig>) => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (config) => apiService.generateLevel(config),
    onSuccess: (level, config) => {
      // Cache the generated level
      queryClient.setQueryData(queryKeys.levelGeneration(config), level);
    },
    ...options,
  });
};

// Batch Generation Mutation
export const useGenerateBatchMutation = (options?: UseMutationOptions<string, Error, BatchGenerationRequest>) => {
  return useMutation({
    mutationFn: (request) => apiService.generateBatch(request),
    ...options,
  });
};

// Configuration Validation Query with caching
export const useConfigValidationQuery = (config: GenerationConfig, options?: UseQueryOptions<ValidationResult>) => {
  return useQuery({
    queryKey: queryKeys.configValidation(config),
    queryFn: () => apiService.validateConfiguration(config),
    enabled: !!config,
    staleTime: CACHE_TIMES.short,
    gcTime: CACHE_TIMES.medium,
    ...options,
  });
};

// Export Level Mutation
export const useExportLevelMutation = (options?: UseMutationOptions<Blob, Error, { level: Level; format: string; options: ExportOptions }>) => {
  return useMutation({
    mutationFn: ({ level, format, options: exportOptions }) => apiService.exportLevel(level, format, exportOptions),
    ...options,
  });
};

// Export Batch Mutation
export const useExportBatchMutation = (options?: UseMutationOptions<string, Error, { levels: Level[]; format: string; options: ExportOptions }>) => {
  return useMutation({
    mutationFn: ({ levels, format, options: exportOptions }) => apiService.exportBatch(levels, format, exportOptions),
    ...options,
  });
};

// Utility hook for invalidating related queries
export const useInvalidateQueries = () => {
  const queryClient = useQueryClient();
  
  return {
    invalidatePresets: () => queryClient.invalidateQueries({ queryKey: queryKeys.presets }),
    invalidateExportFormats: () => queryClient.invalidateQueries({ queryKey: queryKeys.exportFormats }),
    invalidateJobStatus: (jobId: string) => queryClient.invalidateQueries({ queryKey: queryKeys.jobStatus(jobId) }),
    invalidateAll: () => queryClient.invalidateQueries(),
    clearCache: () => queryClient.clear(),
  };
};

// Prefetch utilities
export const usePrefetchQueries = () => {
  const queryClient = useQueryClient();
  
  return {
    prefetchPresets: () => queryClient.prefetchQuery({
      queryKey: queryKeys.presets,
      queryFn: () => apiService.getPresets(),
      staleTime: CACHE_TIMES.medium,
    }),
    prefetchExportFormats: () => queryClient.prefetchQuery({
      queryKey: queryKeys.exportFormats,
      queryFn: () => apiService.getExportFormats(),
      staleTime: CACHE_TIMES.long,
    }),
  };
};

// Cache management utilities
export const useCacheManagement = () => {
  const queryClient = useQueryClient();
  
  return {
    getCacheSize: () => {
      const cache = queryClient.getQueryCache();
      return cache.getAll().length;
    },
    clearOldCache: () => {
      const cache = queryClient.getQueryCache();
      const now = Date.now();
      const maxAge = CACHE_TIMES.long;
      
      cache.getAll().forEach(query => {
        if (query.state.dataUpdatedAt && (now - query.state.dataUpdatedAt) > maxAge) {
          queryClient.removeQueries({ queryKey: query.queryKey });
        }
      });
    },
    getQueryStats: () => {
      const cache = queryClient.getQueryCache();
      const queries = cache.getAll();
      
      return {
        total: queries.length,
        fresh: queries.filter(q => q.state.status === 'success' && !q.isStale()).length,
        stale: queries.filter(q => q.isStale()).length,
        loading: queries.filter(q => q.state.status === 'pending').length,
        error: queries.filter(q => q.state.status === 'error').length,
      };
    },
  };
};