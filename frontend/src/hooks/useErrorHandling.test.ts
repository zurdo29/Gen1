import { renderHook, act } from '@testing-library/react';
import { AxiosError } from 'axios';
import { vi, type MockedFunction } from 'vitest';
import { useErrorHandling } from './useErrorHandling';
import { useNotifications } from './useNotifications';

// Mock the useNotifications hook
vi.mock('./useNotifications');
const mockUseNotifications = useNotifications as MockedFunction<typeof useNotifications>;

// Mock the error handler
vi.mock('../utils/errorHandling', () => ({
  errorHandler: {
    handleApiError: vi.fn(),
    handleGenerationError: vi.fn(),
    handleExportError: vi.fn(),
  }
}));

import { errorHandler } from '../utils/errorHandling';

const mockShowError = vi.fn();
const mockShowWarning = vi.fn();

describe('useErrorHandling', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    
    mockUseNotifications.mockReturnValue({
      notifications: [],
      addNotification: vi.fn(),
      removeNotification: vi.fn(),
      clearAllNotifications: vi.fn(),
      showSuccess: vi.fn(),
      showError: mockShowError,
      showWarning: mockShowWarning,
      showInfo: vi.fn(),
    });

    (errorHandler.handleApiError as any).mockReturnValue({
      title: 'Test Error',
      message: 'Test error message',
      code: 'TEST_ERROR',
      recoveryActions: []
    });
  });

  it('should initialize with no error state', () => {
    const { result } = renderHook(() => useErrorHandling());

    expect(result.current.error).toBeNull();
    expect(result.current.isErrorDialogOpen).toBe(false);
    expect(result.current.hasError).toBe(false);
  });

  it('should handle errors and show dialog by default', () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockError = new Error('Test error');

    act(() => {
      result.current.handleError(mockError, { operation: 'Test Operation' });
    });

    expect(errorHandler.handleApiError).toHaveBeenCalledWith(
      mockError,
      expect.objectContaining({ operation: 'Test Operation' })
    );
    expect(result.current.isErrorDialogOpen).toBe(true);
    expect(result.current.hasError).toBe(true);
  });

  it('should handle errors with notification only', () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockError = new Error('Test error');

    act(() => {
      result.current.handleError(mockError, { operation: 'Test Operation' }, {
        showDialog: false,
        showNotification: true
      });
    });

    expect(mockShowError).toHaveBeenCalledWith('Test Error', 'Test error message');
    expect(result.current.isErrorDialogOpen).toBe(false);
  });

  it('should handle generation errors specifically', () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockError = new Error('Generation failed');

    (errorHandler.handleGenerationError as any).mockReturnValue({
      title: 'Generation Error',
      message: 'Generation failed',
      code: 'GENERATION_ERROR'
    });

    act(() => {
      result.current.handleGenerationError(mockError, { component: 'Generator' });
    });

    expect(errorHandler.handleGenerationError).toHaveBeenCalledWith(
      mockError,
      expect.objectContaining({ 
        operation: 'Level Generation',
        component: 'Generator'
      })
    );
  });

  it('should handle export errors specifically', () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockError = new Error('Export failed');

    (errorHandler.handleExportError as any).mockReturnValue({
      title: 'Export Error',
      message: 'Export failed',
      code: 'EXPORT_ERROR'
    });

    act(() => {
      result.current.handleExportError(mockError, { component: 'Exporter' });
    });

    expect(errorHandler.handleExportError).toHaveBeenCalledWith(
      mockError,
      expect.objectContaining({ 
        operation: 'Level Export',
        component: 'Exporter'
      })
    );
  });

  it('should close error dialog', () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockError = new Error('Test error');

    act(() => {
      result.current.handleError(mockError);
    });

    expect(result.current.isErrorDialogOpen).toBe(true);

    act(() => {
      result.current.closeErrorDialog();
    });

    expect(result.current.isErrorDialogOpen).toBe(false);
    expect(result.current.error).not.toBeNull(); // Error should still exist
  });

  it('should clear error completely', () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockError = new Error('Test error');

    act(() => {
      result.current.handleError(mockError);
    });

    expect(result.current.hasError).toBe(true);

    act(() => {
      result.current.clearError();
    });

    expect(result.current.hasError).toBe(false);
    expect(result.current.error).toBeNull();
    expect(result.current.isErrorDialogOpen).toBe(false);
  });

  it('should retry last action successfully', async () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockRetryAction = vi.fn().mockResolvedValue('success');
    const mockError = new Error('Test error');

    act(() => {
      result.current.handleError(mockError, {}, { retryAction: mockRetryAction });
    });

    expect(result.current.hasError).toBe(true);

    await act(async () => {
      await result.current.retryLastAction();
    });

    expect(mockRetryAction).toHaveBeenCalled();
    expect(result.current.hasError).toBe(false);
  });

  it('should handle retry action failure', async () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockRetryAction = vi.fn().mockRejectedValue(new Error('Retry failed'));
    const mockError = new Error('Test error');

    act(() => {
      result.current.handleError(mockError, {}, { retryAction: mockRetryAction });
    });

    await act(async () => {
      await result.current.retryLastAction();
    });

    expect(mockRetryAction).toHaveBeenCalled();
    expect(errorHandler.handleApiError).toHaveBeenCalledTimes(2); // Original + retry failure
  });

  it('should wrap operations with error handling', async () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockOperation = vi.fn().mockResolvedValue('success');

    const wrappedOperation = result.current.withErrorHandling(
      mockOperation,
      { operation: 'Test Operation' }
    );

    const resultValue = await wrappedOperation('arg1', 'arg2');

    expect(mockOperation).toHaveBeenCalledWith('arg1', 'arg2');
    expect(resultValue).toBe('success');
  });

  it('should handle wrapped operation failures', async () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockOperation = vi.fn().mockRejectedValue(new Error('Operation failed'));
    const mockOnError = vi.fn();

    const wrappedOperation = result.current.withErrorHandling(
      mockOperation,
      { operation: 'Test Operation' },
      { onError: mockOnError }
    );

    const resultValue = await wrappedOperation('arg1');

    expect(mockOperation).toHaveBeenCalledWith('arg1');
    expect(resultValue).toBeNull();
    expect(mockOnError).toHaveBeenCalled();
    expect(errorHandler.handleApiError).toHaveBeenCalled();
  });

  it('should provide specialized wrapper functions', async () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockOperation = vi.fn().mockResolvedValue('success');

    // Test generation wrapper
    const generationWrapper = result.current.withGenerationErrorHandling(
      mockOperation,
      { component: 'Generator' }
    );

    await generationWrapper('test');
    expect(mockOperation).toHaveBeenCalledWith('test');

    // Test export wrapper
    const exportWrapper = result.current.withExportErrorHandling(
      mockOperation,
      { component: 'Exporter' }
    );

    await exportWrapper('test');
    expect(mockOperation).toHaveBeenCalledWith('test');

    // Test configuration wrapper
    const configWrapper = result.current.withConfigurationErrorHandling(
      mockOperation,
      { component: 'Config' }
    );

    await configWrapper('test');
    expect(mockOperation).toHaveBeenCalledWith('test');
  });

  it('should handle network errors with notifications', () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockError = new Error('Network error');

    act(() => {
      result.current.handleNetworkError(mockError, { component: 'API' });
    });

    expect(errorHandler.handleApiError).toHaveBeenCalledWith(
      mockError,
      expect.objectContaining({ 
        operation: 'Network Request',
        component: 'API'
      })
    );
    expect(mockShowError).toHaveBeenCalled();
    expect(result.current.isErrorDialogOpen).toBe(true);
  });

  it('should show warnings for warning-type errors', () => {
    const { result } = renderHook(() => useErrorHandling());
    const mockError = new Error('Warning error');

    (errorHandler.handleApiError as any).mockReturnValue({
      title: 'Warning Error',
      message: 'Warning message',
      code: 'WARNING_ERROR'
    });

    act(() => {
      result.current.handleError(mockError, {}, { showNotification: true });
    });

    expect(mockShowWarning).toHaveBeenCalledWith('Warning Error', 'Warning message');
  });
});