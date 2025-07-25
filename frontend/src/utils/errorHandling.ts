import { AxiosError } from 'axios';

export interface UserFriendlyError {
  title: string;
  message: string;
  details?: string;
  code?: string;
  recoveryActions?: RecoveryAction[];
  troubleshootingUrl?: string;
}

export interface RecoveryAction {
  label: string;
  action: () => void | Promise<void>;
  type: 'primary' | 'secondary';
}

export interface ErrorContext {
  operation: string;
  component?: string;
  userId?: string;
  sessionId?: string;
  timestamp: Date;
  userAgent: string;
  url: string;
  additionalData?: Record<string, any>;
}

export class ErrorHandler {
  private static instance: ErrorHandler;
  private errorReportingEnabled = true;
  private troubleshootingBaseUrl = '/help/troubleshooting';

  static getInstance(): ErrorHandler {
    if (!ErrorHandler.instance) {
      ErrorHandler.instance = new ErrorHandler();
    }
    return ErrorHandler.instance;
  }

  /**
   * Handles API errors and converts them to user-friendly messages
   */
  handleApiError(error: AxiosError, context: Partial<ErrorContext> = {}): UserFriendlyError {
    const fullContext: ErrorContext = {
      operation: context.operation || 'Unknown operation',
      component: context.component,
      timestamp: new Date(),
      userAgent: navigator.userAgent,
      url: window.location.href,
      ...context
    };

    // Log error for debugging
    this.logError(error, fullContext);

    // Handle specific HTTP status codes
    if (error.response) {
      switch (error.response.status) {
        case 400:
          return this.handleValidationError(error, fullContext);
        case 401:
          return this.handleAuthenticationError(error, fullContext);
        case 403:
          return this.handleAuthorizationError(error, fullContext);
        case 404:
          return this.handleNotFoundError(error, fullContext);
        case 408:
        case 504:
          return this.handleTimeoutError(error, fullContext);
        case 429:
          return this.handleRateLimitError(error, fullContext);
        case 500:
          return this.handleServerError(error, fullContext);
        case 502:
        case 503:
          return this.handleServiceUnavailableError(error, fullContext);
        default:
          return this.handleGenericError(error, fullContext);
      }
    } else if (error.request) {
      return this.handleNetworkError(error, fullContext);
    } else {
      return this.handleGenericError(error, fullContext);
    }
  }

  /**
   * Handles generation-specific errors
   */
  handleGenerationError(error: any, context: Partial<ErrorContext> = {}): UserFriendlyError {
    const fullContext: ErrorContext = {
      operation: 'Level Generation',
      ...context,
      timestamp: new Date(),
      userAgent: navigator.userAgent,
      url: window.location.href,
    };

    this.logError(error, fullContext);

    // Check for specific generation error types
    if (error.response?.data?.code) {
      switch (error.response.data.code) {
        case 'INVALID_CONFIGURATION':
          return {
            title: 'Configuration Error',
            message: 'The generation parameters contain invalid values.',
            details: error.response.data.message,
            code: 'INVALID_CONFIGURATION',
            recoveryActions: [
              {
                label: 'Reset to Default',
                action: () => window.location.reload(),
                type: 'primary'
              },
              {
                label: 'Load Preset',
                action: () => {/* Navigate to presets */},
                type: 'secondary'
              }
            ],
            troubleshootingUrl: `${this.troubleshootingBaseUrl}#configuration-errors`
          };
        case 'GENERATION_TIMEOUT':
          return {
            title: 'Generation Timeout',
            message: 'Level generation took too long and was cancelled.',
            details: 'Try reducing the level size or complexity.',
            code: 'GENERATION_TIMEOUT',
            recoveryActions: [
              {
                label: 'Reduce Level Size',
                action: () => {/* Suggest smaller dimensions */},
                type: 'primary'
              },
              {
                label: 'Try Again',
                action: () => {/* Retry generation */},
                type: 'secondary'
              }
            ],
            troubleshootingUrl: `${this.troubleshootingBaseUrl}#timeout-errors`
          };
        case 'MEMORY_LIMIT_EXCEEDED':
          return {
            title: 'Level Too Complex',
            message: 'The requested level is too large or complex to generate.',
            details: 'Try reducing the level dimensions or entity count.',
            code: 'MEMORY_LIMIT_EXCEEDED',
            recoveryActions: [
              {
                label: 'Reduce Complexity',
                action: () => {/* Show complexity reduction tips */},
                type: 'primary'
              }
            ],
            troubleshootingUrl: `${this.troubleshootingBaseUrl}#memory-errors`
          };
        default:
          return this.handleGenericError(error, fullContext);
      }
    }

    return this.handleGenericError(error, fullContext);
  }

  /**
   * Handles export-specific errors
   */
  handleExportError(error: any, context: Partial<ErrorContext> = {}): UserFriendlyError {
    const fullContext: ErrorContext = {
      operation: 'Level Export',
      ...context,
      timestamp: new Date(),
      userAgent: navigator.userAgent,
      url: window.location.href,
    };

    this.logError(error, fullContext);

    if (error.response?.data?.code) {
      switch (error.response.data.code) {
        case 'UNSUPPORTED_FORMAT':
          return {
            title: 'Export Format Not Supported',
            message: 'The selected export format is not available.',
            details: error.response.data.message,
            code: 'UNSUPPORTED_FORMAT',
            recoveryActions: [
              {
                label: 'Choose Different Format',
                action: () => {/* Show format selector */},
                type: 'primary'
              }
            ],
            troubleshootingUrl: `${this.troubleshootingBaseUrl}#export-errors`
          };
        case 'EXPORT_SIZE_LIMIT':
          return {
            title: 'Export Too Large',
            message: 'The level is too large to export in the selected format.',
            details: 'Try exporting in a different format or reducing level size.',
            code: 'EXPORT_SIZE_LIMIT',
            recoveryActions: [
              {
                label: 'Try JSON Format',
                action: () => {/* Switch to JSON export */},
                type: 'primary'
              },
              {
                label: 'Reduce Level Size',
                action: () => {/* Show size reduction options */},
                type: 'secondary'
              }
            ],
            troubleshootingUrl: `${this.troubleshootingBaseUrl}#export-size-errors`
          };
        default:
          return this.handleGenericError(error, fullContext);
      }
    }

    return this.handleGenericError(error, fullContext);
  }

  private handleValidationError(error: AxiosError, _context: ErrorContext): UserFriendlyError {
    const validationDetails = error.response?.data as any;
    
    return {
      title: 'Validation Error',
      message: 'Please check your input and try again.',
      details: validationDetails?.message || 'Some fields contain invalid values.',
      code: 'VALIDATION_ERROR',
      recoveryActions: [
        {
          label: 'Review Input',
          action: () => {/* Focus on first invalid field */},
          type: 'primary'
        }
      ],
      troubleshootingUrl: `${this.troubleshootingBaseUrl}#validation-errors`
    };
  }

  private handleAuthenticationError(_error: AxiosError, _context: ErrorContext): UserFriendlyError {
    return {
      title: 'Authentication Required',
      message: 'Please log in to continue.',
      code: 'AUTHENTICATION_ERROR',
      recoveryActions: [
        {
          label: 'Log In',
          action: () => {/* Navigate to login */},
          type: 'primary'
        }
      ]
    };
  }

  private handleAuthorizationError(_error: AxiosError, _context: ErrorContext): UserFriendlyError {
    return {
      title: 'Access Denied',
      message: 'You do not have permission to perform this action.',
      code: 'AUTHORIZATION_ERROR',
      troubleshootingUrl: `${this.troubleshootingBaseUrl}#permission-errors`
    };
  }

  private handleNotFoundError(_error: AxiosError, _context: ErrorContext): UserFriendlyError {
    return {
      title: 'Not Found',
      message: 'The requested resource could not be found.',
      details: 'The item may have been deleted or moved.',
      code: 'NOT_FOUND_ERROR',
      recoveryActions: [
        {
          label: 'Go Back',
          action: () => window.history.back(),
          type: 'primary'
        },
        {
          label: 'Go Home',
          action: () => window.location.href = '/',
          type: 'secondary'
        }
      ]
    };
  }

  private handleTimeoutError(_error: AxiosError, _context: ErrorContext): UserFriendlyError {
    return {
      title: 'Request Timeout',
      message: 'The operation took too long to complete.',
      details: 'Please check your internet connection and try again.',
      code: 'TIMEOUT_ERROR',
      recoveryActions: [
        {
          label: 'Try Again',
          action: () => window.location.reload(),
          type: 'primary'
        }
      ],
      troubleshootingUrl: `${this.troubleshootingBaseUrl}#timeout-errors`
    };
  }

  private handleRateLimitError(error: AxiosError, _context: ErrorContext): UserFriendlyError {
    const retryAfter = error.response?.headers['retry-after'];
    const waitTime = retryAfter ? `${retryAfter} seconds` : 'a moment';
    
    return {
      title: 'Too Many Requests',
      message: `Please wait ${waitTime} before trying again.`,
      details: 'You have exceeded the rate limit for this operation.',
      code: 'RATE_LIMIT_ERROR',
      recoveryActions: [
        {
          label: 'Wait and Retry',
          action: () => {
            setTimeout(() => window.location.reload(), (parseInt(retryAfter) || 60) * 1000);
          },
          type: 'primary'
        }
      ]
    };
  }

  private handleServerError(_error: AxiosError, _context: ErrorContext): UserFriendlyError {
    return {
      title: 'Server Error',
      message: 'An unexpected error occurred on the server.',
      details: 'Please try again in a few moments.',
      code: 'SERVER_ERROR',
      recoveryActions: [
        {
          label: 'Try Again',
          action: () => window.location.reload(),
          type: 'primary'
        },
        {
          label: 'Report Issue',
          action: () => this.reportError(error, context),
          type: 'secondary'
        }
      ],
      troubleshootingUrl: `${this.troubleshootingBaseUrl}#server-errors`
    };
  }

  private handleServiceUnavailableError(_error: AxiosError, _context: ErrorContext): UserFriendlyError {
    return {
      title: 'Service Unavailable',
      message: 'The service is temporarily unavailable.',
      details: 'Please try again in a few minutes.',
      code: 'SERVICE_UNAVAILABLE',
      recoveryActions: [
        {
          label: 'Try Again Later',
          action: () => {
            setTimeout(() => window.location.reload(), 60000); // Wait 1 minute
          },
          type: 'primary'
        }
      ],
      troubleshootingUrl: `${this.troubleshootingBaseUrl}#service-unavailable`
    };
  }

  private handleNetworkError(_error: AxiosError, _context: ErrorContext): UserFriendlyError {
    return {
      title: 'Network Error',
      message: 'Unable to connect to the server.',
      details: 'Please check your internet connection and try again.',
      code: 'NETWORK_ERROR',
      recoveryActions: [
        {
          label: 'Check Connection',
          action: () => {
            // Try to ping a reliable endpoint
            fetch('/api/health').then(() => {
              window.location.reload();
            }).catch(() => {
              alert('Please check your internet connection and try again.');
            });
          },
          type: 'primary'
        },
        {
          label: 'Work Offline',
          action: () => {/* Enable offline mode */},
          type: 'secondary'
        }
      ],
      troubleshootingUrl: `${this.troubleshootingBaseUrl}#network-errors`
    };
  }

  private handleGenericError(error: any, context: ErrorContext): UserFriendlyError {
    return {
      title: 'Unexpected Error',
      message: 'An unexpected error occurred.',
      details: error.message || 'Please try again or contact support if the problem persists.',
      code: 'GENERIC_ERROR',
      recoveryActions: [
        {
          label: 'Try Again',
          action: () => window.location.reload(),
          type: 'primary'
        },
        {
          label: 'Report Issue',
          action: () => this.reportError(error, context),
          type: 'secondary'
        }
      ],
      troubleshootingUrl: `${this.troubleshootingBaseUrl}#general-errors`
    };
  }

  private logError(error: any, context: ErrorContext): void {
    console.error('Error occurred:', {
      error: {
        message: error.message,
        stack: error.stack,
        response: error.response?.data,
        status: error.response?.status,
      },
      context
    });

    // Send to error reporting service if enabled
    if (this.errorReportingEnabled) {
      this.sendErrorReport(error, context);
    }
  }

  private async sendErrorReport(error: any, context: ErrorContext): Promise<void> {
    try {
      // In a real application, this would send to an error reporting service
      // like Sentry, LogRocket, or a custom endpoint
      const errorReport = {
        error: {
          message: error.message,
          stack: error.stack,
          name: error.name,
        },
        context,
        timestamp: new Date().toISOString(),
        userAgent: navigator.userAgent,
        url: window.location.href,
        userId: context.userId,
        sessionId: context.sessionId,
      };

      // For now, just log to console in development
      if (process.env.NODE_ENV === 'development') {
        console.log('Error report:', errorReport);
      }

      // In production, send to error reporting service
      // await fetch('/api/errors/report', {
      //   method: 'POST',
      //   headers: { 'Content-Type': 'application/json' },
      //   body: JSON.stringify(errorReport)
      // });
    } catch (reportingError) {
      console.error('Failed to send error report:', reportingError);
    }
  }

  private async reportError(error: any, context: ErrorContext): Promise<void> {
    // Open error reporting dialog or navigate to support page
    const errorId = Date.now().toString();
    const supportUrl = `/support?error=${errorId}&operation=${encodeURIComponent(context.operation)}`;
    window.open(supportUrl, '_blank');
  }

  /**
   * Enable or disable error reporting
   */
  setErrorReporting(enabled: boolean): void {
    this.errorReportingEnabled = enabled;
  }

  /**
   * Set the base URL for troubleshooting guides
   */
  setTroubleshootingBaseUrl(url: string): void {
    this.troubleshootingBaseUrl = url;
  }
}

export const errorHandler = ErrorHandler.getInstance();