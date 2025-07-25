import { useState, useCallback, useEffect, useRef } from 'react';
import { BatchGenerationRequest, _JobStatus, Level } from '../types';
import { apiService } from '../services/api';

interface BatchResult {
  id: string;
  level: Level;
  thumbnail?: string;
  variationIndex: number;
  batchIndex: number;
  generatedAt: Date;
  generationTime: number;
}

interface UseBatchGenerationReturn {
  // State
  isGenerating: boolean;
  progress: number;
  currentBatch?: number;
  totalBatches?: number;
  results: BatchResult[];
  error: string | null;
  jobId: string | null;
  
  // Actions
  startBatch: (request: BatchGenerationRequest) => Promise<void>;
  cancelBatch: () => Promise<void>;
  clearResults: () => void;
  clearError: () => void;
}

export const useBatchGeneration = (): UseBatchGenerationReturn => {
  const [isGenerating, setIsGenerating] = useState(false);
  const [progress, setProgress] = useState(0);
  const [currentBatch, setCurrentBatch] = useState<number>();
  const [totalBatches, setTotalBatches] = useState<number>();
  const [results, setResults] = useState<BatchResult[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [jobId, setJobId] = useState<string | null>(null);
  
  const pollingIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const isPollingRef = useRef(false);

  const clearPolling = useCallback(() => {
    if (pollingIntervalRef.current) {
      clearInterval(pollingIntervalRef.current);
      pollingIntervalRef.current = null;
    }
    isPollingRef.current = false;
  }, []);

  const pollJobStatus = useCallback(async (currentJobId: string) => {
    if (isPollingRef.current) return; // Prevent concurrent polling
    
    try {
      isPollingRef.current = true;
      const status = await apiService.getJobStatus(currentJobId);
      
      setProgress(status.progress);
      
      // Update batch info if available
      if (status.metadata) {
        const totalLevels = status.metadata.totalLevels as number;
        if (totalLevels) {
          const completedLevels = Math.floor((status.progress / 100) * totalLevels);
          setCurrentBatch(completedLevels);
          setTotalBatches(totalLevels);
        }
      }
      
      if (status.status === 'completed') {
        setIsGenerating(false);
        clearPolling();
        
        // Process results
        if (status.result && Array.isArray(status.result)) {
          const batchResults: BatchResult[] = status.result.map((item: any) => ({
            id: item.id,
            level: item.level,
            thumbnail: item.thumbnail,
            variationIndex: item.variationIndex,
            batchIndex: item.batchIndex,
            generatedAt: new Date(item.generatedAt),
            generationTime: item.generationTime
          }));
          
          setResults(batchResults);
        }
      } else if (status.status === 'failed') {
        setIsGenerating(false);
        clearPolling();
        setError(status.errorMessage || 'Batch generation failed');
        
        // Include partial results if available
        if (status.result && Array.isArray(status.result)) {
          const batchResults: BatchResult[] = status.result.map((item: any) => ({
            id: item.id,
            level: item.level,
            thumbnail: item.thumbnail,
            variationIndex: item.variationIndex,
            batchIndex: item.batchIndex,
            generatedAt: new Date(item.generatedAt),
            generationTime: item.generationTime
          }));
          
          setResults(batchResults);
        }
      } else if (status.status === 'cancelled') {
        setIsGenerating(false);
        clearPolling();
        setError('Batch generation was cancelled');
      }
    } catch (err) {
      console.error('Error polling job status:', err);
      setError('Failed to check batch generation status');
      setIsGenerating(false);
      clearPolling();
    } finally {
      isPollingRef.current = false;
    }
  }, [clearPolling]);

  const startPolling = useCallback((currentJobId: string) => {
    clearPolling(); // Clear any existing polling
    
    // Poll immediately
    pollJobStatus(currentJobId);
    
    // Set up interval polling
    pollingIntervalRef.current = setInterval(() => {
      pollJobStatus(currentJobId);
    }, 2000); // Poll every 2 seconds
  }, [pollJobStatus, clearPolling]);

  const startBatch = useCallback(async (request: BatchGenerationRequest) => {
    try {
      setError(null);
      setResults([]);
      setProgress(0);
      setCurrentBatch(undefined);
      setTotalBatches(undefined);
      setIsGenerating(true);
      
      const newJobId = await apiService.generateBatch(request);
      setJobId(newJobId);
      
      // Start polling for status
      startPolling(newJobId);
    } catch (err: any) {
      setIsGenerating(false);
      setError(err.response?.data?.error || err.message || 'Failed to start batch generation');
    }
  }, [startPolling]);

  const cancelBatch = useCallback(async () => {
    if (!jobId) return;
    
    try {
      await apiService.cancelBatch(jobId);
      setIsGenerating(false);
      clearPolling();
      setError('Batch generation cancelled');
    } catch (err: any) {
      console.error('Error cancelling batch:', err);
      setError(err.response?.data?.error || err.message || 'Failed to cancel batch generation');
    }
  }, [jobId, clearPolling]);

  const clearResults = useCallback(() => {
    setResults([]);
    setError(null);
    setProgress(0);
    setCurrentBatch(undefined);
    setTotalBatches(undefined);
    setJobId(null);
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      clearPolling();
    };
  }, [clearPolling]);

  return {
    isGenerating,
    progress,
    currentBatch,
    totalBatches,
    results,
    error,
    jobId,
    startBatch,
    cancelBatch,
    clearResults,
    clearError
  };
};