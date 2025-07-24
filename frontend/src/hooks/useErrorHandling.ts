import { useState, useCallback } from 'react';

// Simplified error handling without complex dependencies
export interface UserFriendlyError {
  title: string;
  message: string;
  details?: string;
  code?: string;
}

export interface ErrorState {
  error: UserFriendlyError | null;
  isErrorDialogOpen: boolean;
  retryAction?: () => void | Promise<void>;
}

export const useErrorHandling = () => {
  const [errorState, setErrorState] = useState<ErrorState>({
    error: null,
    isErrorDialogOpen: false,
  });

  const handleError = useCallback((error: any, message?: string) => {
    const userFriendlyError: UserFriendlyError = {
      title: 'Error',
      message: message || error?.message || 'An unexpected error occurred',
      details: error?.stack,
      code: error?.code
    };

    setErrorState({
      error: userFriendlyError,
      isErrorDialogOpen: true,
    });

    return userFriendlyError;
  }, []);

  const closeErrorDialog = useCallback(() => {
    setErrorState(prev => ({
      ...prev,
      isErrorDialogOpen: false,
    }));
  }, []);

  const clearError = useCallback(() => {
    setErrorState({
      error: null,
      isErrorDialogOpen: false,
    });
  }, []);

  const retryLastAction = useCallback(async () => {
    if (errorState.retryAction) {
      try {
        await errorState.retryAction();
        clearError();
      } catch (error) {
        handleError(error, 'Retry failed');
      }
    }
  }, [errorState.retryAction, clearError, handleError]);

  return {
    // Error state
    error: errorState.error,
    isErrorDialogOpen: errorState.isErrorDialogOpen,
    hasError: errorState.error !== null,

    // Error handling functions
    handleError,

    // Dialog management
    closeErrorDialog,
    clearError,
    retryLastAction,
  };
};