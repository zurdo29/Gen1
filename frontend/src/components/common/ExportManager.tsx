import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  Checkbox,
  FormControlLabel,
  Box,
  Typography,
  LinearProgress,
  Alert,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  Grid,
  Card,
  CardContent,
  IconButton,
  Tooltip
} from '@mui/material';
import {
  Download,
  ExpandMore,
  Preview,
  Settings,
  CheckCircle,
  Error,
  Warning,
  Info
} from '@mui/icons-material';
import { Level, ExportFormat, ExportOptions, ValidationResult } from '../../types';
import { apiService } from '../../services/api';
import { useNotifications } from '../../hooks/useNotifications';

interface ExportManagerProps {
  open: boolean;
  onClose: () => void;
  level: Level | null;
  levels?: Level[]; // For batch export
}

interface ExportProgress {
  isExporting: boolean;
  progress: number;
  message: string;
  jobId?: string;
}

export const ExportManager: React.FC<ExportManagerProps> = ({
  open,
  onClose,
  level,
  levels
}) => {
  const [availableFormats, setAvailableFormats] = useState<ExportFormat[]>([]);
  const [selectedFormat, setSelectedFormat] = useState<string>('');
  const [exportOptions, setExportOptions] = useState<ExportOptions>({
    format: '',
    includeMetadata: true,
    customSettings: {}
  });
  const [exportProgress, setExportProgress] = useState<ExportProgress>({
    isExporting: false,
    progress: 0,
    message: ''
  });
  const [validationResult, setValidationResult] = useState<ValidationResult | null>(null);
  const [showPreview, setShowPreview] = useState(false);
  const [previewData, setPreviewData] = useState<string>('');
  const [isBatchExport, setIsBatchExport] = useState(false);

  const { addNotification } = useNotifications();

  useEffect(() => {
    if (open) {
      loadAvailableFormats();
      setIsBatchExport(Boolean(levels && levels.length > 1));
    }
  }, [open, levels]);

  useEffect(() => {
    if (selectedFormat) {
      setExportOptions(prev => ({
        ...prev,
        format: selectedFormat,
        customSettings: getDefaultSettingsForFormat(selectedFormat)
      }));
      validateExportRequest();
    }
  }, [selectedFormat, level]);

  const loadAvailableFormats = async () => {
    try {
      const formats = await apiService.getExportFormats();
      setAvailableFormats(formats);
      
      // Auto-select first format if available
      if (formats.length > 0 && !selectedFormat) {
        setSelectedFormat(formats[0].id);
      }
    } catch (error) {
      console.error('Failed to load export formats:', error);
      addNotification({
        type: 'error',
        title: 'Export Error',
        message: 'Failed to load available export formats'
      });
    }
  };

  const getDefaultSettingsForFormat = (formatId: string): Record<string, any> => {
    const format = availableFormats.find(f => f.id === formatId);
    if (!format?.supportsCustomization) return {};

    // Default settings based on format
    switch (formatId.toLowerCase()) {
      case 'unity':
        return {
          coordinateSystem: 'unity',
          includeColliders: true,
          generatePrefabs: true,
          layerMapping: 'default'
        };
      case 'json':
        return {
          prettyPrint: true,
          includeComments: false,
          compression: 'none'
        };
      case 'xml':
        return {
          includeSchema: true,
          formatting: 'indented',
          encoding: 'utf-8'
        };
      case 'csv':
        return {
          delimiter: ',',
          includeHeaders: true,
          quoteStrings: true
        };
      default:
        return {};
    }
  };

  const validateExportRequest = async () => {
    if (!level || !selectedFormat) return;

    try {
      const result = await apiService.validateConfiguration({
        level,
        format: selectedFormat,
        options: exportOptions
      } as any);
      
      setValidationResult(result);
    } catch (error) {
      console.error('Validation failed:', error);
      setValidationResult({
        isValid: false,
        errors: [{ field: 'general', message: 'Validation failed', code: 'VALIDATION_ERROR' }],
        warnings: []
      });
    }
  };

  const handleExport = async () => {
    if (!level || !selectedFormat) return;

    setExportProgress({
      isExporting: true,
      progress: 0,
      message: 'Starting export...'
    });

    try {
      if (isBatchExport && levels && levels.length > 1) {
        await handleBatchExport();
      } else {
        await handleSingleExport();
      }
    } catch (error) {
      console.error('Export failed:', error);
      addNotification({
        type: 'error',
        title: 'Export Failed',
        message: error instanceof Error ? error.message : 'Unknown error occurred'
      });
      
      setExportProgress({
        isExporting: false,
        progress: 0,
        message: ''
      });
    }
  };

  const handleSingleExport = async () => {
    if (!level) return;

    setExportProgress(prev => ({ ...prev, message: 'Exporting level...', progress: 50 }));

    const blob = await apiService.exportLevel(level, selectedFormat, exportOptions);
    
    // Create download link
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    
    const format = availableFormats.find(f => f.id === selectedFormat);
    const fileName = `${level.id || 'level'}.${format?.fileExtension || 'dat'}`;
    link.download = fileName;
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);

    setExportProgress({
      isExporting: false,
      progress: 100,
      message: 'Export completed successfully!'
    });

    addNotification({
      type: 'success',
      title: 'Export Complete',
      message: `Level exported as ${fileName}`
    });

    setTimeout(() => {
      onClose();
    }, 1500);
  };

  const handleBatchExport = async () => {
    if (!levels || levels.length === 0) return;

    setExportProgress(prev => ({ ...prev, message: 'Starting batch export...', progress: 10 }));

    const jobId = await apiService.exportBatch(levels, selectedFormat, exportOptions);
    
    setExportProgress(prev => ({ ...prev, jobId, message: 'Processing batch export...', progress: 20 }));

    // Poll for progress
    const pollInterval = setInterval(async () => {
      try {
        const status = await apiService.getJobStatus(jobId);
        
        setExportProgress(prev => ({
          ...prev,
          progress: status.progress,
          message: `Processing... (${status.progress}%)`
        }));

        if (status.status === 'completed') {
          clearInterval(pollInterval);
          
          // Download the batch result
          const response = await fetch(`/api/export/batch/${jobId}/download`);
          const blob = await response.blob();
          
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `batch-export-${Date.now()}.zip`;
          
          document.body.appendChild(link);
          link.click();
          document.body.removeChild(link);
          window.URL.revokeObjectURL(url);

          setExportProgress({
            isExporting: false,
            progress: 100,
            message: 'Batch export completed!'
          });

          addNotification({
            type: 'success',
            title: 'Batch Export Complete',
            message: `${levels.length} levels exported successfully`
          });

          setTimeout(() => {
            onClose();
          }, 1500);
        } else if (status.status === 'failed') {
          clearInterval(pollInterval);
          throw new Error(status.errorMessage || 'Batch export failed');
        }
      } catch (error) {
        clearInterval(pollInterval);
        throw error;
      }
    }, 1000);
  };

  const handlePreview = async () => {
    if (!level || !selectedFormat) return;

    try {
      // For preview, we'll export to a string format and show it
      const blob = await apiService.exportLevel(level, selectedFormat, {
        ...exportOptions,
        customSettings: { ...exportOptions.customSettings, preview: true }
      });
      
      const text = await blob.text();
      setPreviewData(text);
      setShowPreview(true);
    } catch (error) {
      console.error('Preview failed:', error);
      addNotification({
        type: 'error',
        title: 'Preview Failed',
        message: 'Unable to generate export preview'
      });
    }
  };

  const handleCustomSettingChange = (key: string, value: any) => {
    setExportOptions(prev => ({
      ...prev,
      customSettings: {
        ...prev.customSettings,
        [key]: value
      }
    }));
  };

  const renderValidationResults = () => {
    if (!validationResult) return null;

    return (
      <Box sx={{ mt: 2 }}>
        {validationResult.errors.length > 0 && (
          <Alert severity="error" sx={{ mb: 1 }}>
            <Typography variant="subtitle2">Validation Errors:</Typography>
            {validationResult.errors.map((error, index) => (
              <Typography key={index} variant="body2">
                • {error.message}
              </Typography>
            ))}
          </Alert>
        )}
        
        {validationResult.warnings.length > 0 && (
          <Alert severity="warning" sx={{ mb: 1 }}>
            <Typography variant="subtitle2">Warnings:</Typography>
            {validationResult.warnings.map((warning, index) => (
              <Typography key={index} variant="body2">
                • {warning.message}
              </Typography>
            ))}
          </Alert>
        )}
        
        {validationResult.isValid && validationResult.errors.length === 0 && (
          <Alert severity="success">
            Export configuration is valid and ready to proceed.
          </Alert>
        )}
      </Box>
    );
  };

  const renderFormatCustomization = () => {
    const format = availableFormats.find(f => f.id === selectedFormat);
    if (!format?.supportsCustomization) return null;

    const settings = exportOptions.customSettings;

    return (
      <Accordion>
        <AccordionSummary expandIcon={<ExpandMore />}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Settings />
            <Typography>Format Settings</Typography>
          </Box>
        </AccordionSummary>
        <AccordionDetails>
          <Grid container spacing={2}>
            {Object.entries(settings).map(([key, value]) => (
              <Grid item xs={12} sm={6} key={key}>
                {typeof value === 'boolean' ? (
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={value}
                        onChange={(e) => handleCustomSettingChange(key, e.target.checked)}
                      />
                    }
                    label={key.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase())}
                  />
                ) : (
                  <TextField
                    fullWidth
                    label={key.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase())}
                    value={value}
                    onChange={(e) => handleCustomSettingChange(key, e.target.value)}
                    size="small"
                  />
                )}
              </Grid>
            ))}
          </Grid>
        </AccordionDetails>
      </Accordion>
    );
  };

  return (
    <>
      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Download />
            <Typography variant="h6">
              {isBatchExport ? `Export ${levels?.length} Levels` : 'Export Level'}
            </Typography>
          </Box>
        </DialogTitle>
        
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
            {/* Format Selection */}
            <FormControl fullWidth>
              <InputLabel>Export Format</InputLabel>
              <Select
                value={selectedFormat}
                onChange={(e) => setSelectedFormat(e.target.value)}
                label="Export Format"
              >
                {availableFormats.map((format) => (
                  <MenuItem key={format.id} value={format.id}>
                    <Box>
                      <Typography variant="body1">{format.name}</Typography>
                      <Typography variant="body2" color="text.secondary">
                        {format.description}
                      </Typography>
                    </Box>
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            {/* Basic Options */}
            <FormControlLabel
              control={
                <Checkbox
                  checked={exportOptions.includeMetadata}
                  onChange={(e) => setExportOptions(prev => ({
                    ...prev,
                    includeMetadata: e.target.checked
                  }))}
                />
              }
              label="Include metadata (generation info, timestamps, etc.)"
            />

            {/* Format-specific customization */}
            {renderFormatCustomization()}

            {/* Validation Results */}
            {renderValidationResults()}

            {/* Export Progress */}
            {exportProgress.isExporting && (
              <Box sx={{ mt: 2 }}>
                <Typography variant="body2" sx={{ mb: 1 }}>
                  {exportProgress.message}
                </Typography>
                <LinearProgress 
                  variant="determinate" 
                  value={exportProgress.progress} 
                />
              </Box>
            )}
          </Box>
        </DialogContent>

        <DialogActions>
          <Button onClick={onClose} disabled={exportProgress.isExporting}>
            Cancel
          </Button>
          
          <Button
            onClick={handlePreview}
            disabled={!level || !selectedFormat || exportProgress.isExporting}
            startIcon={<Preview />}
          >
            Preview
          </Button>
          
          <Button
            onClick={handleExport}
            disabled={
              !level || 
              !selectedFormat || 
              exportProgress.isExporting ||
              (validationResult && !validationResult.isValid)
            }
            variant="contained"
            startIcon={<Download />}
          >
            {isBatchExport ? 'Export Batch' : 'Export'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Preview Dialog */}
      <Dialog open={showPreview} onClose={() => setShowPreview(false)} maxWidth="lg" fullWidth>
        <DialogTitle>Export Preview</DialogTitle>
        <DialogContent>
          <TextField
            multiline
            fullWidth
            rows={20}
            value={previewData}
            InputProps={{
              readOnly: true,
              style: { fontFamily: 'monospace', fontSize: '0.875rem' }
            }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowPreview(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default ExportManager;