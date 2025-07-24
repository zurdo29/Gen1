import React, { useState, useCallback, useEffect } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  IconButton,
  Typography,
  Chip,
  Stack,
  Alert,
  Divider,
  Card,
  CardContent,
  CardActions,
  Grid
} from '@mui/material';
import {
  Save as SaveIcon,
  Delete as DeleteIcon,
  Download as DownloadIcon,
  Upload as UploadIcon,
  Share as ShareIcon,

  Refresh as RefreshIcon
} from '@mui/icons-material';
import { GenerationConfig, ConfigPreset } from '../../types';
import { createPresetConfigs } from '../../utils/configDefaults';
import { useNotifications } from '../../hooks/useNotifications';
import apiService from '../../services/api';

interface PresetManagerProps {
  currentConfig: GenerationConfig;
  onLoadPreset: (config: GenerationConfig) => void;
  onClose: () => void;
  open: boolean;
}

interface SavePresetDialogProps {
  open: boolean;
  onClose: () => void;
  onSave: (name: string, description: string) => void;
  initialName?: string;
  initialDescription?: string;
}

const SavePresetDialog: React.FC<SavePresetDialogProps> = ({
  open,
  onClose,
  onSave,
  initialName = '',
  initialDescription = ''
}) => {
  const [name, setName] = useState(initialName);
  const [description, setDescription] = useState(initialDescription);

  useEffect(() => {
    if (open) {
      setName(initialName);
      setDescription(initialDescription);
    }
  }, [open, initialName, initialDescription]);

  const handleSave = () => {
    if (name.trim()) {
      onSave(name.trim(), description.trim());
      onClose();
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Save Configuration Preset</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField
            fullWidth
            label="Preset Name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Enter a name for this preset"
            required
          />
          <TextField
            fullWidth
            label="Description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Optional description"
            multiline
            rows={3}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSave} variant="contained" disabled={!name.trim()}>
          Save Preset
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export const PresetManager: React.FC<PresetManagerProps> = ({
  currentConfig,
  onLoadPreset,
  onClose,
  open
}) => {
  const [presets, setPresets] = useState<ConfigPreset[]>([]);
  const [builtInPresets] = useState(() => createPresetConfigs());
  const [loading, setLoading] = useState(false);
  const [saveDialogOpen, setSaveDialogOpen] = useState(false);
  const { showSuccess, showError, showInfo } = useNotifications();

  // Load presets from API
  const loadPresets = useCallback(async () => {
    setLoading(true);
    try {
      const loadedPresets = await apiService.getPresets();
      setPresets(loadedPresets);
    } catch (error) {
      console.error('Failed to load presets:', error);
      showError('Load Failed', 'Failed to load saved presets.');
    } finally {
      setLoading(false);
    }
  }, [showError]);

  // Load presets when dialog opens
  useEffect(() => {
    if (open) {
      loadPresets();
    }
  }, [open, loadPresets]);

  const handleSavePreset = useCallback(async (name: string, description: string) => {
    try {
      const newPreset = await apiService.savePreset({
        name,
        description,
        config: currentConfig
      });
      setPresets(prev => [...prev, newPreset]);
      showSuccess('Preset Saved', `Configuration saved as "${name}"`);
    } catch (error) {
      console.error('Failed to save preset:', error);
      showError('Save Failed', 'Failed to save preset.');
    }
  }, [currentConfig, showSuccess, showError]);

  const handleDeletePreset = useCallback(async (presetId: string) => {
    try {
      // Note: API doesn't have delete endpoint yet, so we'll just remove from local state
      setPresets(prev => prev.filter(p => p.id !== presetId));
      showInfo('Preset Deleted', 'Preset removed from your saved configurations.');
    } catch (error) {
      console.error('Failed to delete preset:', error);
      showError('Delete Failed', 'Failed to delete preset.');
    }
  }, [showInfo, showError]);

  const handleLoadPreset = useCallback((config: GenerationConfig) => {
    onLoadPreset(config);
    onClose();
    showSuccess('Preset Loaded', 'Configuration loaded successfully!');
  }, [onLoadPreset, onClose, showSuccess]);

  const handleExportPreset = useCallback((preset: ConfigPreset) => {
    const dataStr = JSON.stringify(preset, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${preset.name.replace(/[^a-z0-9]/gi, '_').toLowerCase()}_preset.json`;
    link.click();
    URL.revokeObjectURL(url);
    showSuccess('Export Complete', 'Preset exported successfully!');
  }, [showSuccess]);

  const handleImportPreset = useCallback(() => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json';
    input.onchange = (e) => {
      const file = (e.target as HTMLInputElement).files?.[0];
      if (file) {
        const reader = new FileReader();
        reader.onload = (e) => {
          try {
            const preset = JSON.parse(e.target?.result as string);
            if (preset.config && preset.name) {
              handleSavePreset(preset.name, preset.description || 'Imported preset');
            } else {
              showError('Import Failed', 'Invalid preset file format.');
            }
          } catch (error) {
            showError('Import Failed', 'Failed to parse preset file.');
          }
        };
        reader.readAsText(file);
      }
    };
    input.click();
  }, [handleSavePreset, showError]);

  const handleSharePreset = useCallback(async (preset: ConfigPreset) => {
    try {
      const shareResult = await apiService.createShareLink(preset.config);
      await navigator.clipboard.writeText(shareResult.shareUrl);
      showSuccess('Link Copied', 'Shareable link copied to clipboard!');
    } catch (error) {
      console.error('Failed to create share link:', error);
      showError('Share Failed', 'Failed to create shareable link.');
    }
  }, [showSuccess, showError]);

  const getPresetSummary = (config: GenerationConfig) => {
    return `${config.width}×${config.height} • ${config.generationAlgorithm} • ${config.entities.length} entities`;
  };

  return (
    <>
      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
        <DialogTitle>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            Configuration Presets
            <Stack direction="row" spacing={1}>
              <Button
                startIcon={<UploadIcon />}
                onClick={handleImportPreset}
                size="small"
                variant="outlined"
              >
                Import
              </Button>
              <Button
                startIcon={<SaveIcon />}
                onClick={() => setSaveDialogOpen(true)}
                size="small"
                variant="contained"
              >
                Save Current
              </Button>
            </Stack>
          </Box>
        </DialogTitle>
        
        <DialogContent>
          <Stack spacing={3}>
            {/* Built-in Presets */}
            <Box>
              <Typography variant="h6" gutterBottom>
                Built-in Presets
              </Typography>
              <Grid container spacing={2}>
                {Object.entries(builtInPresets).map(([key, config]) => (
                  <Grid item xs={12} sm={6} key={key}>
                    <Card variant="outlined">
                      <CardContent>
                        <Typography variant="h6" gutterBottom>
                          {key.charAt(0).toUpperCase() + key.slice(1)}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" gutterBottom>
                          {getPresetSummary(config)}
                        </Typography>
                        <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                          <Chip label={config.generationAlgorithm} size="small" />
                          <Chip label={config.gameplay.difficulty} size="small" color="primary" />
                        </Stack>
                      </CardContent>
                      <CardActions>
                        <Button
                          size="small"
                          onClick={() => handleLoadPreset(config)}
                        >
                          Load
                        </Button>
                        <Button
                          size="small"
                          startIcon={<ShareIcon />}
                          onClick={() => handleSharePreset({ 
                            id: key, 
                            name: key, 
                            description: `Built-in ${key} preset`, 
                            config, 
                            createdAt: new Date() 
                          })}
                        >
                          Share
                        </Button>
                      </CardActions>
                    </Card>
                  </Grid>
                ))}
              </Grid>
            </Box>

            <Divider />

            {/* User Presets */}
            <Box>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6">
                  Your Saved Presets ({presets.length})
                </Typography>
                <IconButton onClick={loadPresets} disabled={loading}>
                  <RefreshIcon />
                </IconButton>
              </Box>

              {loading ? (
                <Typography color="text.secondary">Loading presets...</Typography>
              ) : presets.length === 0 ? (
                <Alert severity="info">
                  No saved presets yet. Save your current configuration to create your first preset!
                </Alert>
              ) : (
                <List>
                  {presets.map((preset) => (
                    <ListItem key={preset.id} divider>
                      <ListItemText
                        primary={preset.name}
                        secondary={
                          <Stack spacing={1}>
                            <Typography variant="body2" color="text.secondary">
                              {preset.description || 'No description'}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {getPresetSummary(preset.config)} • Created {preset.createdAt.toLocaleDateString()}
                            </Typography>
                            <Stack direction="row" spacing={1}>
                              <Chip label={preset.config.generationAlgorithm} size="small" />
                              <Chip label={preset.config.gameplay.difficulty} size="small" color="primary" />
                            </Stack>
                          </Stack>
                        }
                      />
                      <ListItemSecondaryAction>
                        <Stack direction="row" spacing={1}>
                          <Button
                            size="small"
                            onClick={() => handleLoadPreset(preset.config)}
                          >
                            Load
                          </Button>
                          <IconButton
                            size="small"
                            onClick={() => handleSharePreset(preset)}
                            title="Share"
                          >
                            <ShareIcon />
                          </IconButton>
                          <IconButton
                            size="small"
                            onClick={() => handleExportPreset(preset)}
                            title="Export"
                          >
                            <DownloadIcon />
                          </IconButton>
                          <IconButton
                            size="small"
                            onClick={() => handleDeletePreset(preset.id)}
                            color="error"
                            title="Delete"
                          >
                            <DeleteIcon />
                          </IconButton>
                        </Stack>
                      </ListItemSecondaryAction>
                    </ListItem>
                  ))}
                </List>
              )}
            </Box>
          </Stack>
        </DialogContent>

        <DialogActions>
          <Button onClick={onClose}>Close</Button>
        </DialogActions>
      </Dialog>

      <SavePresetDialog
        open={saveDialogOpen}
        onClose={() => setSaveDialogOpen(false)}
        onSave={handleSavePreset}
      />
    </>
  );
};