import { useState, useCallback, useEffect, useRef } from 'react';
import { Level, GenerationConfig, ValidationResult, PreviewStatus } from '../types';
import { apiService } from '../services/api';
import { signalRService } from '../services/signalr';
import { useNotifications } from './useNotifications';

interface UseRealTimeLevelOptions {
  sessionId: string;
  debounceMs?: number;
  autoConnect?: boolean;
}

interface UseRealTimeLevelReturn {
  level: Level | null;
  isGenerating: boolean;
  progress: number;
  progressMessage: string;
  error: string | null;
  validationResult: ValidationResult | null;
  previewStatus: PreviewStatus | null;
  generatePreview: (config: GenerationConfig) => Promise<void>;
  cancelPreview: () => Promise<void>;
  connect: () => Promise<void>;
  disconnect: () => Promise<void>;
  isConnected: boolean;
}

export const useRealTimeLevel = (options: UseRealTimeLevelOptions): UseRealTimeLevelReturn => {
  const { sessionId, debounceMs = 500, autoConnect = true } = options;
  
  const [level, setLevel] = useState<Level | null>(null);
  const [isGenerating, setIsGenerating] = useState(false);
  const [progress, setProgress] = useState(0);
  const [progressMessage, setProgressMessage] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [validationResult, setValidationResult] = useState<ValidationResult | null>(null);
  const [previewStatus, setPreviewStatus] = useState<PreviewStatus | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  
  const { showError, showSuccess } = useNotifications();
  const debounceTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const lastConfigRef = useRef<GenerationConfig | null>(null);

  // Initialize SignalR connection
  const connect = useCallback(async () => {
    try {
      await signalRService.connect();
      await signalRService.joinSession(sessionId);
      setIsConnected(true);
    } catch (error) {
      console.error('Failed to connect to real-time service:', error);
      showError('Connection Error', 'Failed to connect to real-time preview service');
      setIsConnected(false);
    }
  }, [sessionId, showError]);

  const disconnect = useCallback(async () => {
    try {
      await signalRService.disconnect();
      setIsConnected(false);
    } catch (error) {
      console.error('Failed to disconnect from real-time service:', error);
    }
  }, []);

  // Set up SignalR callbacks
  useEffect(() => {
    signalRService.setCallbacks({
      onGenerationProgress: (receivedSessionId: string, receivedProgress: number, message: string) => {
        if (receivedSessionId === sessionId) {
          setProgress(receivedProgress);
          setProgressMessage(message);
          setIsGenerating(receivedProgress < 100);
        }
      },
      onPreviewGenerated: (receivedSessionId: string, generatedLevel: Level) => {
        if (receivedSessionId === sessionId) {
          setLevel(generatedLevel);
          setIsGenerating(false);
          setProgress(100);
          setProgressMessage('Preview generated successfully');
          setError(null);
          showSuccess('Preview Generated', 'Level preview updated successfully');
        }
      },
      onGenerationError: (receivedSessionId: string, errorMessage: string) => {
        if (receivedSessionId === sessionId) {
          setError(errorMessage);
          setIsGenerating(false);
          setProgress(0);
          setProgressMessage('');
          showError('Generation Error', errorMessage);
        }
      },
      onValidationResult: (receivedSessionId: string, result: ValidationResult) => {
        if (receivedSessionId === sessionId) {
          setValidationResult(result);
          if (!result.isValid) {
            setError('Configuration validation failed');
            showError('Validation Error', 'Please fix configuration errors');
          }
        }
      }
    });

    return () => {
      signalRService.clearCallbacks();
    };
  }, [sessionId, showError, showSuccess]);

  // Auto-connect on mount
  useEffect(() => {
    if (autoConnect) {
      connect();
    }

    return () => {
      if (debounceTimeoutRef.current) {
        clearTimeout(debounceTimeoutRef.current);
      }
    };
  }, [autoConnect, connect]);

  // Generate preview with debouncing
  const generatePreview = useCallback(async (config: GenerationConfig) => {
    // Clear any existing timeout
    if (debounceTimeoutRef.current) {
      clearTimeout(debounceTimeoutRef.current);
    }

    // Store the latest config
    lastConfigRef.current = config;

    // Clear previous errors
    setError(null);
    setValidationResult(null);

    // Set up debounced execution
    debounceTimeoutRef.current = setTimeout(async () => {
      try {
        // Ensure we're connected
        if (!isConnected) {
          await connect();
        }

        // Use the latest config (in case it changed during debounce)
        const latestConfig = lastConfigRef.current;
        if (!latestConfig) return;

        setIsGenerating(true);
        setProgress(0);
        setProgressMessage('Requesting preview...');

        // Request preview from API
        await apiService.requestPreview(sessionId, latestConfig, debounceMs);
        
        // The actual progress and results will come through SignalR callbacks
      } catch (error) {
        console.error('Failed to request preview:', error);
        setError(error instanceof Error ? error.message : 'Failed to request preview');
        setIsGenerating(false);
        setProgress(0);
        setProgressMessage('');
        showError('Preview Error', 'Failed to request level preview');
      }
    }, debounceMs);
  }, [sessionId, debounceMs, isConnected, connect, showError]);

  // Cancel preview
  const cancelPreview = useCallback(async () => {
    try {
      // Clear debounce timeout
      if (debounceTimeoutRef.current) {
        clearTimeout(debounceTimeoutRef.current);
        debounceTimeoutRef.current = null;
      }

      // Cancel on server
      await apiService.cancelPreview(sessionId);
      
      // Reset local state
      setIsGenerating(false);
      setProgress(0);
      setProgressMessage('');
      setError(null);
    } catch (error) {
      console.error('Failed to cancel preview:', error);
      showError('Cancel Error', 'Failed to cancel preview generation');
    }
  }, [sessionId, showError]);

  // Periodically fetch preview status (fallback for SignalR)
  useEffect(() => {
    if (!isGenerating) return;

    const statusInterval = setInterval(async () => {
      try {
        const status = await apiService.getPreviewStatus(sessionId);
        setPreviewStatus(status);
        
        // Update local state if SignalR missed updates
        if (status.status === 'completed' && status.lastResult && isGenerating) {
          setLevel(status.lastResult);
          setIsGenerating(false);
          setProgress(100);
          setProgressMessage('Preview completed');
        } else if (status.status === 'error' && status.errorMessage) {
          setError(status.errorMessage);
          setIsGenerating(false);
          setProgress(0);
          setProgressMessage('');
        }
      } catch (error) {
        // Ignore status fetch errors to avoid spam
        console.debug('Failed to fetch preview status:', error);
      }
    }, 2000); // Check every 2 seconds

    return () => clearInterval(statusInterval);
  }, [sessionId, isGenerating]);

  return {
    level,
    isGenerating,
    progress,
    progressMessage,
    error,
    validationResult,
    previewStatus,
    generatePreview,
    cancelPreview,
    connect,
    disconnect,
    isConnected
  };
};