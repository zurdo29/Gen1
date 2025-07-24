import { AxiosError } from 'axios';
import { ErrorHandler } from './errorHandling';

// Mock axios error responses
const createMockAxiosError = (status: number, data?: any): AxiosError => {
  const error = new Error('Mock error') as any;
  error.isAxiosError = true;
  error.response = {
    status,
    data: data || { message: 'Mock error message' },
    headers: {},
    config: {},
    statusText: 'Mock Status'
  };
  error.config = {};
  return error;
};

const createMockNetworkError = (): AxiosError => {
  const error = new Error('Network Error') as any;
  error.isAxiosError = true;
  error.request = {};
  error.config = {};
  return error;
};

describe('ErrorHandler', () => {
  let errorHandler: ErrorHandler;

  beforeEach(() => {
    errorHandler = ErrorHandler.getInstance();
    // Reset console methods
    jest.spyOn(console, 'error').mockImplementation(() => {});
    jest.spyOn(console, 'log').mockImplementation(() => {});
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  describe('handleApiError', () => {
    it('should handle validation errors (400)', () => {
      const error = createMockAxiosError(400, {
        message: 'Validation failed',
        errors: ['Field is required']
      });

      const result = errorHandler.handleApiError(error, {
        operation: 'Test Operation'
      });

      expect(result.title).toBe('Validation Error');
      expect(result.code).toBe('VALIDATION_ERROR');
      expect(result.recoveryActions).toHaveLength(1);
      expect(result.troubleshootingUrl).toContain('validation-errors');
    });

    it('should handle authentication errors (401)', () => {
      const error = createMockAxiosError(401);

      const result = errorHandler.handleApiError(error);

      expect(result.title).toBe('Authentication Required');
      expect(result.code).toBe('AUTHENTICATION_ERROR');
      expect(result.recoveryActions).toHaveLength(1);
      expect(result.recoveryActions![0].label).toBe('Log In');
    });

    it('should handle not found errors (404)', () => {
      const error = createMockAxiosError(404);

      const result = errorHandler.handleApiError(error);

      expect(result.title).toBe('Not Found');
      expect(result.code).toBe('NOT_FOUND_ERROR');
      expect(result.recoveryActions).toHaveLength(2);
    });

    it('should handle timeout errors (408)', () => {
      const error = createMockAxiosError(408);

      const result = errorHandler.handleApiError(error);

      expect(result.title).toBe('Request Timeout');
      expect(result.code).toBe('TIMEOUT_ERROR');
      expect(result.troubleshootingUrl).toContain('timeout-errors');
    });

    it('should handle rate limit errors (429)', () => {
      const error = createMockAxiosError(429);
      error.response!.headers = { 'retry-after': '60' };

      const result = errorHandler.handleApiError(error);

      expect(result.title).toBe('Too Many Requests');
      expect(result.code).toBe('RATE_LIMIT_ERROR');
      expect(result.message).toContain('60 seconds');
    });

    it('should handle server errors (500)', () => {
      const error = createMockAxiosError(500);

      const result = errorHandler.handleApiError(error);

      expect(result.title).toBe('Server Error');
      expect(result.code).toBe('SERVER_ERROR');
      expect(result.recoveryActions).toHaveLength(2);
      expect(result.troubleshootingUrl).toContain('server-errors');
    });

    it('should handle network errors', () => {
      const error = createMockNetworkError();

      const result = errorHandler.handleApiError(error);

      expect(result.title).toBe('Network Error');
      expect(result.code).toBe('NETWORK_ERROR');
      expect(result.recoveryActions).toHaveLength(2);
      expect(result.troubleshootingUrl).toContain('network-errors');
    });
  });

  describe('handleGenerationError', () => {
    it('should handle invalid configuration errors', () => {
      const error = createMockAxiosError(400, {
        code: 'INVALID_CONFIGURATION',
        message: 'Invalid level dimensions'
      });

      const result = errorHandler.handleGenerationError(error);

      expect(result.title).toBe('Configuration Error');
      expect(result.code).toBe('INVALID_CONFIGURATION');
      expect(result.recoveryActions).toHaveLength(2);
      expect(result.troubleshootingUrl).toContain('configuration-errors');
    });

    it('should handle generation timeout errors', () => {
      const error = createMockAxiosError(408, {
        code: 'GENERATION_TIMEOUT',
        message: 'Generation took too long'
      });

      const result = errorHandler.handleGenerationError(error);

      expect(result.title).toBe('Generation Timeout');
      expect(result.code).toBe('GENERATION_TIMEOUT');
      expect(result.recoveryActions).toHaveLength(2);
      expect(result.troubleshootingUrl).toContain('timeout-errors');
    });

    it('should handle memory limit errors', () => {
      const error = createMockAxiosError(422, {
        code: 'MEMORY_LIMIT_EXCEEDED',
        message: 'Level too complex'
      });

      const result = errorHandler.handleGenerationError(error);

      expect(result.title).toBe('Level Too Complex');
      expect(result.code).toBe('MEMORY_LIMIT_EXCEEDED');
      expect(result.recoveryActions).toHaveLength(1);
      expect(result.troubleshootingUrl).toContain('memory-errors');
    });
  });

  describe('handleExportError', () => {
    it('should handle unsupported format errors', () => {
      const error = createMockAxiosError(422, {
        code: 'UNSUPPORTED_FORMAT',
        message: 'Format not supported'
      });

      const result = errorHandler.handleExportError(error);

      expect(result.title).toBe('Export Format Not Supported');
      expect(result.code).toBe('UNSUPPORTED_FORMAT');
      expect(result.recoveryActions).toHaveLength(1);
      expect(result.troubleshootingUrl).toContain('export-errors');
    });

    it('should handle export size limit errors', () => {
      const error = createMockAxiosError(422, {
        code: 'EXPORT_SIZE_LIMIT',
        message: 'Export too large'
      });

      const result = errorHandler.handleExportError(error);

      expect(result.title).toBe('Export Too Large');
      expect(result.code).toBe('EXPORT_SIZE_LIMIT');
      expect(result.recoveryActions).toHaveLength(2);
    });
  });

  describe('error logging and reporting', () => {
    it('should log errors with context', () => {
      const error = createMockAxiosError(500);
      const context = {
        operation: 'Test Operation',
        component: 'TestComponent',
        userId: 'user123'
      };

      errorHandler.handleApiError(error, context);

      expect(console.error).toHaveBeenCalledWith(
        'Error occurred:',
        expect.objectContaining({
          error: expect.objectContaining({
            message: 'Mock error',
            status: 500
          }),
          context: expect.objectContaining({
            operation: 'Test Operation',
            component: 'TestComponent',
            userId: 'user123'
          })
        })
      );
    });

    it('should enable/disable error reporting', () => {
      errorHandler.setErrorReporting(false);
      
      const error = createMockAxiosError(500);
      errorHandler.handleApiError(error);

      // Should still log but not send reports
      expect(console.error).toHaveBeenCalled();
    });

    it('should set troubleshooting base URL', () => {
      const customUrl = '/custom-help';
      errorHandler.setTroubleshootingBaseUrl(customUrl);

      const error = createMockAxiosError(400);
      const result = errorHandler.handleApiError(error);

      expect(result.troubleshootingUrl).toContain(customUrl);
    });
  });

  describe('recovery actions', () => {
    it('should provide appropriate recovery actions for different error types', () => {
      const validationError = createMockAxiosError(400);
      const validationResult = errorHandler.handleApiError(validationError);
      expect(validationResult.recoveryActions).toHaveLength(1);
      expect(validationResult.recoveryActions![0].type).toBe('primary');

      const serverError = createMockAxiosError(500);
      const serverResult = errorHandler.handleApiError(serverError);
      expect(serverResult.recoveryActions).toHaveLength(2);
      expect(serverResult.recoveryActions![0].type).toBe('primary');
      expect(serverResult.recoveryActions![1].type).toBe('secondary');
    });

    it('should handle recovery action execution', () => {
      const mockAction = jest.fn();
      const error = createMockAxiosError(500);
      const result = errorHandler.handleApiError(error);

      // Recovery actions should be functions
      expect(typeof result.recoveryActions![0].action).toBe('function');
    });
  });
});