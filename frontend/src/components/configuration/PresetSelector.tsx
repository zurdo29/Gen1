import React, { useState, useCallback } from 'react';
import {
  Box,
  Button,
  Menu,
  MenuItem,
  ListItemText,
  ListItemIcon,
  Divider,
  Typography,
  Chip,
  Stack
} from '@mui/material';
import {
  Bookmark as BookmarkIcon,
  BookmarkBorder as BookmarkBorderIcon,
  Settings as SettingsIcon,
  KeyboardArrowDown as ArrowDownIcon
} from '@mui/icons-material';
import { GenerationConfig } from '../../types';
import { createPresetConfigs } from '../../utils/configDefaults';

interface PresetSelectorProps {
  currentConfig: GenerationConfig;
  onLoadPreset: (config: GenerationConfig) => void;
  onOpenPresetManager: () => void;
}

export const PresetSelector: React.FC<PresetSelectorProps> = ({
  currentConfig,
  onLoadPreset,
  onOpenPresetManager
}) => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [builtInPresets] = useState(() => createPresetConfigs());
  const open = Boolean(anchorEl);

  const handleClick = useCallback((event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget);
  }, []);

  const handleClose = useCallback(() => {
    setAnchorEl(null);
  }, []);

  const handleLoadPreset = useCallback((config: GenerationConfig) => {
    onLoadPreset(config);
    handleClose();
  }, [onLoadPreset, handleClose]);

  const handleOpenManager = useCallback(() => {
    onOpenPresetManager();
    handleClose();
  }, [onOpenPresetManager, handleClose]);

  const getCurrentPresetName = useCallback(() => {
    // Try to match current config with built-in presets
    for (const [name, preset] of Object.entries(builtInPresets)) {
      if (
        preset.generationAlgorithm === currentConfig.generationAlgorithm &&
        preset.width === currentConfig.width &&
        preset.height === currentConfig.height &&
        preset.gameplay.difficulty === currentConfig.gameplay.difficulty
      ) {
        return name.charAt(0).toUpperCase() + name.slice(1);
      }
    }
    return 'Custom';
  }, [currentConfig, builtInPresets]);

  const getPresetDescription = (config: GenerationConfig) => {
    return `${config.width}×${config.height} • ${config.generationAlgorithm}`;
  };

  return (
    <>
      <Button
        variant="outlined"
        onClick={handleClick}
        endIcon={<ArrowDownIcon />}
        startIcon={getCurrentPresetName() === 'Custom' ? <BookmarkBorderIcon /> : <BookmarkIcon />}
        sx={{ minWidth: 140 }}
      >
        {getCurrentPresetName()}
      </Button>

      <Menu
        anchorEl={anchorEl}
        open={open}
        onClose={handleClose}
        PaperProps={{
          sx: { minWidth: 280 }
        }}
      >
        <Box sx={{ px: 2, py: 1 }}>
          <Typography variant="subtitle2" color="text.secondary">
            Quick Load Presets
          </Typography>
        </Box>
        
        <Divider />

        {Object.entries(builtInPresets).map(([key, config]) => (
          <MenuItem
            key={key}
            onClick={() => handleLoadPreset(config)}
            selected={getCurrentPresetName().toLowerCase() === key}
          >
            <ListItemIcon>
              <BookmarkIcon color={getCurrentPresetName().toLowerCase() === key ? 'primary' : 'inherit'} />
            </ListItemIcon>
            <ListItemText
              primary={key.charAt(0).toUpperCase() + key.slice(1)}
              secondary={
                <Stack spacing={0.5}>
                  <Typography variant="caption" color="text.secondary">
                    {getPresetDescription(config)}
                  </Typography>
                  <Stack direction="row" spacing={0.5}>
                    <Chip 
                      label={config.gameplay.difficulty} 
                      size="small" 
                      variant="outlined"
                      sx={{ height: 16, fontSize: '0.6rem' }}
                    />
                    <Chip 
                      label={`${config.entities.length} entities`} 
                      size="small" 
                      variant="outlined"
                      sx={{ height: 16, fontSize: '0.6rem' }}
                    />
                  </Stack>
                </Stack>
              }
            />
          </MenuItem>
        ))}

        <Divider />

        <MenuItem onClick={handleOpenManager}>
          <ListItemIcon>
            <SettingsIcon />
          </ListItemIcon>
          <ListItemText
            primary="Manage Presets"
            secondary="Save, load, and share configurations"
          />
        </MenuItem>
      </Menu>
    </>
  );
};