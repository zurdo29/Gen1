import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { useRealTimeLevel } from './useRealTimeLevel';
import { apiService } from '../services/api';
import { signalRService } from '../services/signalr';
import { GenerationConfig, Level, ValidationResult } from '../types';

// Mock dependencies
vi.mock('../services/api');
vi.mock('../services/signalr');
vi.mock('./useNotifications', () => ({
  useNotifications: () => ({
    showError: vi.fn(),
    showSuccess: vi.fn(),
    showInfo: vi.fn()
  })
}));

const mockApiService = vi.mocked(apiService);
const mockSignalRService = vi.mocked(signalRService);

describe('useRealTimeLevel', () => {
  const defaultOptions = {
    sessionId: 'test-session-123',
    debounceMs: 100,
    autoConnect: true
  };

  const mockConfig: GenerationConfig = {
    width: 20,
    height: 20,
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
      difficulty: 'normal',
      timeLimit: 300,
      victoryConditions: ['reach_exit'],
      mechanics: {}
    }
  };

  const mockLevel: Level = {
    id: 'test-level',
    config: mockConfig,
    terrain: {
      width: 20,
      height: 20,
      tiles: []
    },
    entities: [],
    metadata: {
      generatedAt: new Date(),
      generationTime: 1000,
      version: '1.0'
    }
  };

  beforeEach(() => {
    vi.clearAllMocks();
    
    // Setup default mocks
    mockSignalRService.connect.mockResolvedValue(undefined);
    mockSignalRService.joinSession.mockResolvedValue(undefined);
    mockSignalRService.disconnect.mockResolvedValue(undefined);
    mockSignalRService.isConnected.mockReturnValue(true);
    mockSignalRService.setCallbacks.mockImplementation(() => {
      // Mock setCallbacks implementation
    });
    mockSignalRService.clearCallbacks.mockImplementation(() => {
      // Mock clearCallbacks implementation
    });
    
    mockApiService.requestPreview.mockResolvedValue({
      sessionId: defaultOptions.sessionId,
      status: 'requested',
      message: 'Preview requested'
    });
    
    mockApiService.cancelPreview.mockResolvedValue(undefined);
    mockApiService.getPreviewStatus.mockResolvedValue({
      sessionId: defaultOptions.sessionId,
      status: 'idle',
      progress: 0
    });
  });

  afterEach(() => {
    vi.clearAllTimers();
  });

  describe('Initialization', () => {
    it('should initialize with default state', () => {
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      expect(result.current.level).toBeNull();
      expect(result.current.isGenerating).toBe(false);
      expect(result.current.progress).toBe(0);
      expect(result.current.progressMessage).toBe('');
      expect(result.current.error).toBeNull();
      expect(result.current.validationResult).toBeNull();
      expect(result.current.previewStatus).toBeNull();
    });

    it('should auto-connect when autoConnect is true', async () => {
      renderHook(() => useRealTimeLevel(defaultOptions));

      await waitFor(() => {
        expect(mockSignalRService.connect).toHaveBeenCalledOnce();
        expect(mockSignalRService.joinSession).toHaveBeenCalledWith(defaultOptions.sessionId);
      });
    });

    it('should not auto-connect when autoConnect is false', () => {
      renderHook(() => useRealTimeLevel({ ...defaultOptions, autoConnect: false }));

      expect(mockSignalRService.connect).not.toHaveBeenCalled();
      expect(mockSignalRService.joinSession).not.toHaveBeenCalled();
    });

    it('should set up SignalR callbacks', () => {
      renderHook(() => useRealTimeLevel(defaultOptions));

      expect(mockSignalRService.setCallbacks).toHaveBeenCalledWith({
        onGenerationProgress: expect.any(Function),
        onPreviewGenerated: expect.any(Function),
        onGenerationError: expect.any(Function),
        onValidationResult: expect.any(Function)
      });
    });
  });

  describe('Connection Management', () => {
    it('should connect successfully', async () => {
      const { result } = renderHook(() => useRealTimeLevel({ ...defaultOptions, autoConnect: false }));

      await act(async () => {
        await result.current.connect();
      });

      expect(mockSignalRService.connect).toHaveBeenCalledOnce();
      expect(mockSignalRService.joinSession).toHaveBeenCalledWith(defaultOptions.sessionId);
      expect(result.current.isConnected).toBe(true);
    });

    it('should handle connection failure', async () => {
      const error = new Error('Connection failed');
      mockSignalRService.connect.mockRejectedValue(error);

      const { result } = renderHook(() => useRealTimeLevel({ ...defaultOptions, autoConnect: false }));

      await act(async () => {
        await result.current.connect();
      });

      expect(result.current.isConnected).toBe(false);
    });

    it('should disconnect successfully', async () => {
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      await act(async () => {
        await result.current.disconnect();
      });

      expect(mockSignalRService.disconnect).toHaveBeenCalledOnce();
      expect(result.current.isConnected).toBe(false);
    });
  });

  describe('Preview Generation', () => {
    it('should generate preview with debouncing', async () => {
      vi.useFakeTimers();
      
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      act(() => {
        result.current.generatePreview(mockConfig);
      });

      // Should not call API immediately
      expect(mockApiService.requestPreview).not.toHaveBeenCalled();

      // Fast-forward past debounce time
      act(() => {
        vi.advanceTimersByTime(defaultOptions.debounceMs + 10);
      });

      await waitFor(() => {
        expect(mockApiService.requestPreview).toHaveBeenCalledWith(
          defaultOptions.sessionId,
          mockConfig,
          defaultOptions.debounceMs
        );
      });

      vi.useRealTimers();
    });

    it('should cancel previous request when new one is made', async () => {
      vi.useFakeTimers();
      
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      // Make first request
      act(() => {
        result.current.generatePreview(mockConfig);
      });

      // Make second request before debounce completes
      act(() => {
        vi.advanceTimersByTime(50);
        result.current.generatePreview({ ...mockConfig, width: 30 });
      });

      // Fast-forward past debounce time
      act(() => {
        vi.advanceTimersByTime(defaultOptions.debounceMs + 10);
      });

      await waitFor(() => {
        // Should only call API once with the latest config
        expect(mockApiService.requestPreview).toHaveBeenCalledTimes(1);
        expect(mockApiService.requestPreview).toHaveBeenCalledWith(
          defaultOptions.sessionId,
          expect.objectContaining({ width: 30 }),
          defaultOptions.debounceMs
        );
      });

      vi.useRealTimers();
    });

    it('should handle API request failure', async () => {
      const error = new Error('API request failed');
      mockApiService.requestPreview.mockRejectedValue(error);

      vi.useFakeTimers();
      
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      act(() => {
        result.current.generatePreview(mockConfig);
      });

      act(() => {
        vi.advanceTimersByTime(defaultOptions.debounceMs + 10);
      });

      await waitFor(() => {
        expect(result.current.error).toBe('API request failed');
        expect(result.current.isGenerating).toBe(false);
      });

      vi.useRealTimers();
    });
  });

  describe('SignalR Event Handling', () => {
    it('should handle generation progress events', async () => {
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      // Get the callbacks that were set
      const setCallbacksCall = mockSignalRService.setCallbacks.mock.calls[0][0];
      const { onGenerationProgress } = setCallbacksCall;

      act(() => {
        onGenerationProgress(defaultOptions.sessionId, 50, 'Generating terrain...');
      });

      expect(result.current.progress).toBe(50);
      expect(result.current.progressMessage).toBe('Generating terrain...');
      expect(result.current.isGenerating).toBe(true);
    });

    it('should handle preview generated events', async () => {
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      const setCallbacksCall = mockSignalRService.setCallbacks.mock.calls[0][0];
      const { onPreviewGenerated } = setCallbacksCall;

      act(() => {
        onPreviewGenerated(defaultOptions.sessionId, mockLevel);
      });

      expect(result.current.level).toEqual(mockLevel);
      expect(result.current.isGenerating).toBe(false);
      expect(result.current.progress).toBe(100);
      expect(result.current.error).toBeNull();
    });

    it('should handle generation error events', async () => {
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      const setCallbacksCall = mockSignalRService.setCallbacks.mock.calls[0][0];
      const { onGenerationError } = setCallbacksCall;

      act(() => {
        onGenerationError(defaultOptions.sessionId, 'Generation failed');
      });

      expect(result.current.error).toBe('Generation failed');
      expect(result.current.isGenerating).toBe(false);
      expect(result.current.progress).toBe(0);
    });

    it('should handle validation result events', async () => {
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      const setCallbacksCall = mockSignalRService.setCallbacks.mock.calls[0][0];
      const { onValidationResult } = setCallbacksCall;

      const validationResult: ValidationResult = {
        isValid: false,
        errors: [{ field: 'width', message: 'Invalid width', code: 'INVALID_WIDTH' }],
        warnings: []
      };

      act(() => {
        onValidationResult(defaultOptions.sessionId, validationResult);
      });

      expect(result.current.validationResult).toEqual(validationResult);
      expect(result.current.error).toBe('Configuration validation failed');
    });

    it('should ignore events from other sessions', async () => {
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      const setCallbacksCall = mockSignalRService.setCallbacks.mock.calls[0][0];
      const { onGenerationProgress } = setCallbacksCall;

      act(() => {
        onGenerationProgress('other-session', 50, 'Other session progress');
      });

      expect(result.current.progress).toBe(0);
      expect(result.current.progressMessage).toBe('');
    });
  });

  describe('Preview Cancellation', () => {
    it('should cancel preview successfully', async () => {
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      await act(async () => {
        await result.current.cancelPreview();
      });

      expect(mockApiService.cancelPreview).toHaveBeenCalledWith(defaultOptions.sessionId);
      expect(result.current.isGenerating).toBe(false);
      expect(result.current.progress).toBe(0);
      expect(result.current.error).toBeNull();
    });

    it('should handle cancel failure', async () => {
      const error = new Error('Cancel failed');
      mockApiService.cancelPreview.mockRejectedValue(error);

      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      await act(async () => {
        await result.current.cancelPreview();
      });

      // Should not throw, but should handle gracefully
      expect(mockApiService.cancelPreview).toHaveBeenCalledWith(defaultOptions.sessionId);
    });

    it('should cancel debounced request', async () => {
      vi.useFakeTimers();
      
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      // Start generation
      act(() => {
        result.current.generatePreview(mockConfig);
      });

      // Cancel before debounce completes
      await act(async () => {
        await result.current.cancelPreview();
      });

      // Fast-forward past debounce time
      act(() => {
        vi.advanceTimersByTime(defaultOptions.debounceMs + 10);
      });

      // Should not have called API
      expect(mockApiService.requestPreview).not.toHaveBeenCalled();

      vi.useRealTimers();
    });
  });

  describe('Status Polling', () => {
    it('should poll status when generating', async () => {
      vi.useFakeTimers();
      
      const { result } = renderHook(() => useRealTimeLevel(defaultOptions));

      // Set generating state
      act(() => {
        const setCallbacksCall = mockSignalRService.setCallbacks.mock.calls[0][0];
        const { onGenerationProgress } = setCallbacksCall;
        onGenerationProgress(defaultOptions.sessionId, 25, 'Generating...');
      });

      expect(result.current.isGenerating).toBe(true);

      // Fast-forward to trigger status polling
      act(() => {
        vi.advanceTimersByTime(2000);
      });

      await waitFor(() => {
        expect(mockApiService.getPreviewStatus).toHaveBeenCalledWith(defaultOptions.sessionId);
      });

      vi.useRealTimers();
    });

    it('should not poll status when not generating', () => {
      vi.useFakeTimers();
      
      renderHook(() => useRealTimeLevel(defaultOptions));

      // Fast-forward
      act(() => {
        vi.advanceTimersByTime(5000);
      });

      expect(mockApiService.getPreviewStatus).not.toHaveBeenCalled();

      vi.useRealTimers();
    });
  });

  describe('Cleanup', () => {
    it('should clear callbacks on unmount', () => {
      const { unmount } = renderHook(() => useRealTimeLevel(defaultOptions));

      unmount();

      expect(mockSignalRService.clearCallbacks).toHaveBeenCalledOnce();
    });

    it('should clear debounce timeout on unmount', () => {
      vi.useFakeTimers();
      
      const { result, unmount } = renderHook(() => useRealTimeLevel(defaultOptions));

      act(() => {
        result.current.generatePreview(mockConfig);
      });

      unmount();

      // Fast-forward past debounce time
      act(() => {
        vi.advanceTimersByTime(defaultOptions.debounceMs + 10);
      });

      // Should not call API after unmount
      expect(mockApiService.requestPreview).not.toHaveBeenCalled();

      vi.useRealTimers();
    });
  });
});