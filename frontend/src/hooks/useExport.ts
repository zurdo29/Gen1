import { useState, useCallback } from 'react';
import { Level, ExportFormat, ExportOptions, JobStatus } from '../types';
import { apiService } from '../services/api';
import { useNotifications } from './useNotifications';

interface ExportState {
  isExporting: boolean;
  progress: number;
  message: string;
  jobId?: string;
  error?: string;
}

interface UseExportReturn {
  exportState: ExportState;
  availableFormats: ExportFormat[];
  loadFormats: () => Promise<void>;
  exportLevel: (level: Level, format: string, options?: ExportOptions) => Promise<void>;
  exportBatch: (levels: Level[], format: string, options?: ExportOptions) => Promise<void>;
  cancelExport: () => Promise<void>;
  downloadBatchResult: (jobId: string) => Promise<void>;
  validateExport: (level: Level, format: string, options?: ExportOptions) => Promise<boolean>;
  resetExportState: () => void;
}

export const useExport = (): UseExportReturn => {
  const [exportState, setExportState] = useState<ExportState>({
    isExporting: false,
    progress: 0,
    message: ''
  });
  const [availableFormats, setAvailableFormats] = useState<ExportFormat[]>([]);
  const { addNotification } = useNotifications();

  const loadFormats = useCallback(async () => {
    try {
      const formats = await apiService.getExportFormats();
      setAvailableFormats(formats);
    } catch (error) {
      console.error('Failed to load export formats:', error);
      addNotification({
        type: 'error',
        title: 'Export Error',
        message: 'Failed to load available export formats'
      });
    }
  }, [addNotification]);

  const exportLevel = useCallback(async (
    level: Level, 
    format: string, 
    options: ExportOptions = { format, includeMetadata: true, customSettings: {} }
  ) => {
    setExportState({
      isExporting: true,
      progress: 0,
      message: 'Starting export...'
    });

    try {
      setExportState(prev => ({ ...prev, progress: 25, message: 'Validating export...' }));
      
      // Validate first
      const isValid = await validateExport(level, format, options);
      if (!isValid) {
        throw new Error('Export validation failed');
      }

      setExportState(prev => ({ ...prev, progress: 50, message: 'Exporting level...' }));

      const blob = await apiService.exportLevel(level, format, options);
      
      setExportState(prev => ({ ...prev, progress: 75, message: 'Preparing download...' }));

      // Create download
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      
      const formatInfo = availableFormats.find(f => f.id === format);
      const fileName = `${level.id || 'level'}.${formatInfo?.fileExtension || 'dat'}`;
      link.download = fileName;
      
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);

      setExportState({
        isExporting: false,
        progress: 100,
        message: 'Export completed successfully!'
      });

      addNotification({
        type: 'success',
        title: 'Export Complete',
        message: `Level exported as ${fileName}`
      });

    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Export failed';
      setExportState({
        isExporting: false,
        progress: 0,
        message: '',
        error: errorMessage
      });

      addNotification({
        type: 'error',
        title: 'Export Failed',
        message: errorMessage
      });
      
      throw error;
    }
  }, [availableFormats, addNotification]);

  const exportBatch = useCallback(async (
    levels: Level[], 
    format: string, 
    options: ExportOptions = { format, includeMetadata: true, customSettings: {} }
  ) => {
    setExportState({
      isExporting: true,
      progress: 0,
      message: 'Starting batch export...'
    });

    try {
      setExportState(prev => ({ ...prev, progress: 10, message: 'Initiating batch export...' }));

      const jobId = await apiService.exportBatch(levels, format, options);
      
      setExportState(prev => ({ 
        ...prev, 
        jobId, 
        progress: 20, 
        message: 'Processing batch export...' 
      }));

      // Poll for progress
      const pollInterval = setInterval(async () => {
        try {
          const status = await apiService.getJobStatus(jobId);
          
          setExportState(prev => ({
            ...prev,
            progress: Math.min(status.progress, 95), // Reserve 5% for download
            message: `Processing... (${status.progress}%)`
          }));

          if (status.status === 'completed') {
            clearInterval(pollInterval);
            
            setExportState(prev => ({ 
              ...prev, 
              progress: 100, 
              message: 'Downloading batch export...' 
            }));

            await downloadBatchResult(jobId);

            setExportState({
              isExporting: false,
              progress: 100,
              message: 'Batch export completed!'
            });

            addNotification({
              type: 'success',
              title: 'Batch Export Complete',
              message: `${levels.length} levels exported successfully`
            });

          } else if (status.status === 'failed') {
            clearInterval(pollInterval);
            throw new Error(status.errorMessage || 'Batch export failed');
          }
        } catch (error) {
          clearInterval(pollInterval);
          throw error;
        }
      }, 1000);

    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Batch export failed';
      setExportState({
        isExporting: false,
        progress: 0,
        message: '',
        error: errorMessage
      });

      addNotification({
        type: 'error',
        title: 'Batch Export Failed',
        message: errorMessage
      });
      
      throw error;
    }
  }, [addNotification]);

  const cancelExport = useCallback(async () => {
    if (exportState.jobId) {
      try {
        // Cancel the batch job if it exists
        await fetch(`/api/export/batch/${exportState.jobId}`, { method: 'DELETE' });
      } catch (error) {
        console.error('Failed to cancel export job:', error);
      }
    }

    setExportState({
      isExporting: false,
      progress: 0,
      message: 'Export cancelled'
    });

    addNotification({
      type: 'info',
      title: 'Export Cancelled',
      message: 'Export operation was cancelled'
    });
  }, [exportState.jobId, addNotification]);

  const downloadBatchResult = useCallback(async (jobId: string) => {
    try {
      const response = await fetch(`/api/export/batch/${jobId}/download`);
      
      if (!response.ok) {
        throw new Error(`Download failed: ${response.statusText}`);
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `batch-export-${Date.now()}.zip`;
      
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);

    } catch (error) {
      console.error('Failed to download batch result:', error);
      throw error;
    }
  }, []);

  const validateExport = useCallback(async (
    level: Level, 
    format: string, 
    options: ExportOptions = { format, includeMetadata: true, customSettings: {} }
  ): Promise<boolean> => {
    try {
      const result = await apiService.validateConfiguration({
        level,
        format,
        options
      } as any);
      
      if (!result.isValid) {
        const errorMessages = result.errors.map(e => e.message).join(', ');
        addNotification({
          type: 'error',
          title: 'Export Validation Failed',
          message: errorMessages
        });
        return false;
      }

      if (result.warnings.length > 0) {
        const warningMessages = result.warnings.map(w => w.message).join(', ');
        addNotification({
          type: 'warning',
          title: 'Export Warnings',
          message: warningMessages
        });
      }

      return true;
    } catch (error) {
      console.error('Export validation failed:', error);
      addNotification({
        type: 'error',
        title: 'Validation Error',
        message: 'Failed to validate export configuration'
      });
      return false;
    }
  }, [addNotification]);

  const resetExportState = useCallback(() => {
    setExportState({
      isExporting: false,
      progress: 0,
      message: ''
    });
  }, []);

  return {
    exportState,
    availableFormats,
    loadFormats,
    exportLevel,
    exportBatch,
    cancelExport,
    downloadBatchResult,
    validateExport,
    resetExportState
  };
};

export default useExport;