import { renderHook, act, waitFor } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { useExport } from './useExport';
import { apiService } from '../services/api';
import { useNotifications } from './useNotifications';
import { Level, ExportFormat } from '../types';

// Mock dependencies
vi.mock('../services/api');
vi.mock('./useNotifications');

const mockApiService = vi.mocked(apiService);
const mockUseNotifications = vi.mocked(useNotifications);

const mockLevel: Level = {
  id: 'test-level-1',
  config: {
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
  },
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

const mockFormats: ExportFormat[] = [
  {
    id: 'json',
    name: 'JSON',
    description: 'Web-friendly JSON format',
    fileExtension: 'json',
    supportsCustomization: true
  },
  {
    id: 'unity',
    name: 'Unity',
    description: 'Unity-compatible format',
    fileExtension: 'unity',
    supportsCustomization: true
  }
];

const mockAddNotification = vi.fn();

describe('useExport', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    
    mockUseNotifications.mockReturnValue({
      notifications: [],
      addNotification: mockAddNotification,
      removeNotification: vi.fn(),
      clearNotifications: vi.fn()
    });

    // Mock DOM methods
    global.URL.createObjectURL = vi.fn(() => 'mock-url');
    global.URL.revokeObjectURL = vi.fn();
    
    const mockLink = {
      href: '',
      download: '',
      click: vi.fn()
    };
    vi.spyOn(document, 'createElement').mockReturnValue(mockLink as any);
    vi.spyOn(document.body, 'appendChild').mockImplementation(() => mockLink as any);
    vi.spyOn(document.body, 'removeChild').mockImplementation(() => mockLink as any);
  });

  it('initializes with default state', () => {
    const { result } = renderHook(() => useExport());

    expect(result.current.exportState).toEqual({
      isExporting: false,
      progress: 0,
      message: ''
    });
    expect(result.current.availableFormats).toEqual([]);
  });

  it('loads available formats', async () => {
    mockApiService.getExportFormats.mockResolvedValue(mockFormats);

    const { result } = renderHook(() => useExport());

    await act(async () => {
      await result.current.loadFormats();
    });

    expect(mockApiService.getExportFormats).toHaveBeenCalled();
    expect(result.current.availableFormats).toEqual(mockFormats);
  });

  it('handles format loading error', async () => {
    mockApiService.getExportFormats.mockRejectedValue(new Error('Network error'));

    const { result } = renderHook(() => useExport());

    await act(async () => {
      await result.current.loadFormats();
    });

    expect(mockAddNotification).toHaveBeenCalledWith({
      type: 'error',
      title: 'Export Error',
      message: 'Failed to load available export formats'
    });
  });

  it('exports single level successfully', async () => {
    const mockBlob = new Blob(['test data'], { type: 'application/json' });
    mockApiService.validateConfiguration.mockResolvedValue({
      isValid: true,
      errors: [],
      warnings: []
    });
    mockApiService.exportLevel.mockResolvedValue(mockBlob);

    const { result } = renderHook(() => useExport());
    
    // Set available formats first
    act(() => {
      result.current.availableFormats.push(...mockFormats);
    });

    await act(async () => {
      await result.current.exportLevel(mockLevel, 'json');
    });

    expect(mockApiService.validateConfiguration).toHaveBeenCalled();
    expect(mockApiService.exportLevel).toHaveBeenCalledWith(
      mockLevel,
      'json',
      expect.objectContaining({
        format: 'json',
        includeMetadata: true,
        customSettings: {}
      })
    );

    expect(result.current.exportState.isExporting).toBe(false);
    expect(result.current.exportState.progress).toBe(100);
    expect(mockAddNotification).toHaveBeenCalledWith({
      type: 'success',
      title: 'Export Complete',
      message: 'Level exported as test-level-1.json'
    });
  });

  it('handles export validation failure', async () => {
    mockApiService.validateConfiguration.mockResolvedValue({
      isValid: false,
      errors: [{ field: 'level', message: 'Invalid level data', code: 'INVALID' }],
      warnings: []
    });

    const { result } = renderHook(() => useExport());

    await act(async () => {
      try {
        await result.current.exportLevel(mockLevel, 'json');
      } catch (error) {
        // Expected to throw
      }
    });

    expect(mockAddNotification).toHaveBeenCalledWith({
      type: 'error',
      title: 'Export Validation Failed',
      message: 'Invalid level data'
    });

    expect(result.current.exportState.isExporting).toBe(false);
    expect(result.current.exportState.error).toBe('Export validation failed');
  });

  it('handles export error', async () => {
    mockApiService.validateConfiguration.mockResolvedValue({
      isValid: true,
      errors: [],
      warnings: []
    });
    mockApiService.exportLevel.mockRejectedValue(new Error('Export failed'));

    const { result } = renderHook(() => useExport());

    await act(async () => {
      try {
        await result.current.exportLevel(mockLevel, 'json');
      } catch (error) {
        // Expected to throw
      }
    });

    expect(mockAddNotification).toHaveBeenCalledWith({
      type: 'error',
      title: 'Export Failed',
      message: 'Export failed'
    });

    expect(result.current.exportState.isExporting).toBe(false);
    expect(result.current.exportState.error).toBe('Export failed');
  });

  it('exports batch successfully', async () => {
    const levels = [mockLevel, { ...mockLevel, id: 'test-level-2' }];
    const mockJobId = 'job-123';
    
    mockApiService.exportBatch.mockResolvedValue(mockJobId);
    mockApiService.getJobStatus
      .mockResolvedValueOnce({
        jobId: mockJobId,
        status: 'running',
        progress: 50,
        errorMessage: undefined,
        result: undefined
      })
      .mockResolvedValueOnce({
        jobId: mockJobId,
        status: 'completed',
        progress: 100,
        errorMessage: undefined,
        result: undefined
      });

    // Mock fetch for download
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      blob: () => Promise.resolve(new Blob(['zip data']))
    });

    const { result } = renderHook(() => useExport());

    await act(async () => {
      await result.current.exportBatch(levels, 'json');
    });

    expect(mockApiService.exportBatch).toHaveBeenCalledWith(
      levels,
      'json',
      expect.objectContaining({
        format: 'json',
        includeMetadata: true,
        customSettings: {}
      })
    );

    // Wait for polling to complete
    await waitFor(() => {
      expect(result.current.exportState.isExporting).toBe(false);
      expect(result.current.exportState.progress).toBe(100);
    }, { timeout: 3000 });

    expect(mockAddNotification).toHaveBeenCalledWith({
      type: 'success',
      title: 'Batch Export Complete',
      message: '2 levels exported successfully'
    });
  });

  it('handles batch export job failure', async () => {
    const levels = [mockLevel];
    const mockJobId = 'job-123';
    
    mockApiService.exportBatch.mockResolvedValue(mockJobId);
    mockApiService.getJobStatus.mockResolvedValue({
      jobId: mockJobId,
      status: 'failed',
      progress: 50,
      errorMessage: 'Processing failed',
      result: undefined
    });

    const { result } = renderHook(() => useExport());

    await act(async () => {
      try {
        await result.current.exportBatch(levels, 'json');
      } catch (error) {
        // Expected to throw
      }
    });

    await waitFor(() => {
      expect(result.current.exportState.isExporting).toBe(false);
      expect(result.current.exportState.error).toBe('Processing failed');
    }, { timeout: 3000 });

    expect(mockAddNotification).toHaveBeenCalledWith({
      type: 'error',
      title: 'Batch Export Failed',
      message: 'Processing failed'
    });
  });

  it('validates export configuration', async () => {
    mockApiService.validateConfiguration.mockResolvedValue({
      isValid: true,
      errors: [],
      warnings: [{ field: 'entities', message: 'No entities found', suggestion: 'Add entities' }]
    });

    const { result } = renderHook(() => useExport());

    let isValid: boolean;
    await act(async () => {
      isValid = await result.current.validateExport(mockLevel, 'json');
    });

    expect(isValid!).toBe(true);
    expect(mockAddNotification).toHaveBeenCalledWith({
      type: 'warning',
      title: 'Export Warnings',
      message: 'No entities found'
    });
  });

  it('handles validation errors', async () => {
    mockApiService.validateConfiguration.mockResolvedValue({
      isValid: false,
      errors: [{ field: 'terrain', message: 'Invalid terrain', code: 'INVALID_TERRAIN' }],
      warnings: []
    });

    const { result } = renderHook(() => useExport());

    let isValid: boolean;
    await act(async () => {
      isValid = await result.current.validateExport(mockLevel, 'json');
    });

    expect(isValid!).toBe(false);
    expect(mockAddNotification).toHaveBeenCalledWith({
      type: 'error',
      title: 'Export Validation Failed',
      message: 'Invalid terrain'
    });
  });

  it('cancels export operation', async () => {
    const { result } = renderHook(() => useExport());

    // Set up export state with job ID
    act(() => {
      result.current.exportState.jobId = 'job-123';
      result.current.exportState.isExporting = true;
    });

    global.fetch = vi.fn().mockResolvedValue({ ok: true });

    await act(async () => {
      await result.current.cancelExport();
    });

    expect(global.fetch).toHaveBeenCalledWith('/api/export/batch/job-123', { method: 'DELETE' });
    expect(result.current.exportState.isExporting).toBe(false);
    expect(mockAddNotification).toHaveBeenCalledWith({
      type: 'info',
      title: 'Export Cancelled',
      message: 'Export operation was cancelled'
    });
  });

  it('downloads batch result', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      blob: () => Promise.resolve(new Blob(['zip data']))
    });

    const { result } = renderHook(() => useExport());

    await act(async () => {
      await result.current.downloadBatchResult('job-123');
    });

    expect(global.fetch).toHaveBeenCalledWith('/api/export/batch/job-123/download');
    expect(document.createElement).toHaveBeenCalledWith('a');
  });

  it('handles download failure', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      statusText: 'Not Found'
    });

    const { result } = renderHook(() => useExport());

    await act(async () => {
      try {
        await result.current.downloadBatchResult('job-123');
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
        expect((error as Error).message).toBe('Download failed: Not Found');
      }
    });
  });

  it('resets export state', () => {
    const { result } = renderHook(() => useExport());

    // Set some state
    act(() => {
      result.current.exportState.isExporting = true;
      result.current.exportState.progress = 50;
      result.current.exportState.message = 'Exporting...';
      result.current.exportState.error = 'Some error';
    });

    act(() => {
      result.current.resetExportState();
    });

    expect(result.current.exportState).toEqual({
      isExporting: false,
      progress: 0,
      message: ''
    });
  });
});