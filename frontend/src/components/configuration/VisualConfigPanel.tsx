import React, { useCallback, useState } from 'react';
import {
  Box,
  TextField,
  Typography,
  Grid,
  Paper,
  Stack,
  Button,
  IconButton,
  Card,
  CardContent,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Palette as PaletteIcon,
  Image as ImageIcon,
  ColorLens as ColorLensIcon
} from '@mui/icons-material';
import { GenerationConfig, ValidationError } from '../../types';

interface VisualConfigPanelProps {
  config: GenerationConfig;
  onChange: (updates: Partial<GenerationConfig>) => void;
  validationErrors: ValidationError[];
}

interface ColorPickerDialogProps {
  open: boolean;
  title: string;
  initialColor: string;
  onClose: () => void;
  onSave: (color: string) => void;
}

const ColorPickerDialog: React.FC<ColorPickerDialogProps> = ({
  open,
  title,
  initialColor,
  onClose,
  onSave
}) => {
  const [color, setColor] = useState(initialColor);

  const handleSave = () => {
    onSave(color);
    onClose();
  };

  const presetColors = [
    '#FF0000', '#00FF00', '#0000FF', '#FFFF00', '#FF00FF', '#00FFFF',
    '#800000', '#008000', '#000080', '#808000', '#800080', '#008080',
    '#FFA500', '#FFC0CB', '#A52A2A', '#808080', '#000000', '#FFFFFF'
  ];

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2}>
          <TextField
            fullWidth
            label="Color Value"
            value={color}
            onChange={(e) => setColor(e.target.value)}
            helperText="Enter hex color (#RRGGBB) or named color"
            InputProps={{
              startAdornment: (
                <Box
                  sx={{
                    width: 24,
                    height: 24,
                    backgroundColor: color,
                    border: '1px solid #ccc',
                    borderRadius: 1,
                    mr: 1
                  }}
                />
              )
            }}
          />
          
          <Typography variant="subtitle2">Preset Colors:</Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {presetColors.map((presetColor) => (
              <Box
                key={presetColor}
                sx={{
                  width: 32,
                  height: 32,
                  backgroundColor: presetColor,
                  border: color === presetColor ? '3px solid #1976d2' : '1px solid #ccc',
                  borderRadius: 1,
                  cursor: 'pointer'
                }}
                onClick={() => setColor(presetColor)}
              />
            ))}
          </Box>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSave} variant="contained">Save</Button>
      </DialogActions>
    </Dialog>
  );
};

export const VisualConfigPanel: React.FC<VisualConfigPanelProps> = ({
  config,
  onChange,
  validationErrors
}) => {
  const [colorDialogOpen, setColorDialogOpen] = useState(false);
  const [colorDialogTitle, setColorDialogTitle] = useState('');
  const [colorDialogInitial, setColorDialogInitial] = useState('');
  const [colorDialogCallback, setColorDialogCallback] = useState<(color: string) => void>(() => {});

  const getFieldError = useCallback((fieldName: string) => {
    return validationErrors.find(error => 
      error.field === `visualTheme.${fieldName}` || 
      error.field.startsWith(`visualTheme.${fieldName}.`)
    );
  }, [validationErrors]);

  const handleVisualThemeUpdate = useCallback((updates: Partial<typeof config.visualTheme>) => {
    onChange({
      visualTheme: {
        ...config.visualTheme,
        ...updates
      }
    });
  }, [config.visualTheme, onChange]);

  const handleColorPaletteUpdate = useCallback((colorName: string, colorValue: string) => {
    const newColorPalette = { ...config.visualTheme.colorPalette };
    if (colorValue === '' || colorValue === null || colorValue === undefined) {
      delete newColorPalette[colorName];
    } else {
      newColorPalette[colorName] = colorValue;
    }
    handleVisualThemeUpdate({ colorPalette: newColorPalette });
  }, [config.visualTheme.colorPalette, handleVisualThemeUpdate]);

  const handleTileSpriteUpdate = useCallback((tileType: string, spritePath: string) => {
    const newTileSprites = { ...config.visualTheme.tileSprites };
    if (spritePath === '' || spritePath === null || spritePath === undefined) {
      delete newTileSprites[tileType];
    } else {
      newTileSprites[tileType] = spritePath;
    }
    handleVisualThemeUpdate({ tileSprites: newTileSprites });
  }, [config.visualTheme.tileSprites, handleVisualThemeUpdate]);

  const handleEntitySpriteUpdate = useCallback((entityType: string, spritePath: string) => {
    const newEntitySprites = { ...config.visualTheme.entitySprites };
    if (spritePath === '' || spritePath === null || spritePath === undefined) {
      delete newEntitySprites[entityType];
    } else {
      newEntitySprites[entityType] = spritePath;
    }
    handleVisualThemeUpdate({ entitySprites: newEntitySprites });
  }, [config.visualTheme.entitySprites, handleVisualThemeUpdate]);

  const handleEffectSettingUpdate = useCallback((settingName: string, value: any) => {
    const newEffectSettings = { ...config.visualTheme.effectSettings };
    if (value === '' || value === null || value === undefined) {
      delete newEffectSettings[settingName];
    } else {
      newEffectSettings[settingName] = value;
    }
    handleVisualThemeUpdate({ effectSettings: newEffectSettings });
  }, [config.visualTheme.effectSettings, handleVisualThemeUpdate]);

  const openColorDialog = (title: string, initialColor: string, callback: (color: string) => void) => {
    setColorDialogTitle(title);
    setColorDialogInitial(initialColor);
    setColorDialogCallback(() => callback);
    setColorDialogOpen(true);
  };

  const addNewColorEntry = () => {
    const colorName = `color_${Date.now()}`;
    handleColorPaletteUpdate(colorName, '#FFFFFF');
  };

  const addNewTileSprite = () => {
    const tileType = `tile_${Date.now()}`;
    handleTileSpriteUpdate(tileType, '');
  };

  const addNewEntitySprite = () => {
    const entityType = `entity_${Date.now()}`;
    handleEntitySpriteUpdate(entityType, '');
  };

  const addNewEffectSetting = () => {
    const settingName = `setting_${Date.now()}`;
    handleEffectSettingUpdate(settingName, '');
  };

  return (
    <Stack spacing={3}>
      {/* Theme Name */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Typography variant="h6" gutterBottom>
          Theme Settings
        </Typography>
        <TextField
          fullWidth
          label="Theme Name"
          value={config.visualTheme.themeName}
          onChange={(e) => handleVisualThemeUpdate({ themeName: e.target.value })}
          error={!!getFieldError('themeName')}
          helperText={getFieldError('themeName')?.message || 'Name for this visual theme'}
        />
      </Paper>

      {/* Color Palette */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">
            <ColorLensIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
            Color Palette
          </Typography>
          <Button
            startIcon={<AddIcon />}
            onClick={addNewColorEntry}
            size="small"
            variant="outlined"
          >
            Add Color
          </Button>
        </Box>

        <Grid container spacing={2}>
          {Object.entries(config.visualTheme.colorPalette).map(([colorName, colorValue]) => (
            <Grid item xs={12} sm={6} md={4} key={colorName}>
              <Card variant="outlined">
                <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                  <Stack spacing={1}>
                    <TextField
                      fullWidth
                      size="small"
                      label="Color Name"
                      value={colorName}
                      onChange={(e) => {
                        const newColorPalette = { ...config.visualTheme.colorPalette };
                        delete newColorPalette[colorName];
                        if (e.target.value) {
                          newColorPalette[e.target.value] = colorValue;
                        }
                        handleVisualThemeUpdate({ colorPalette: newColorPalette });
                      }}
                    />
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Box
                        sx={{
                          width: 32,
                          height: 32,
                          backgroundColor: colorValue,
                          border: '1px solid #ccc',
                          borderRadius: 1,
                          cursor: 'pointer'
                        }}
                        onClick={() => openColorDialog(
                          `Edit ${colorName}`,
                          colorValue,
                          (newColor) => handleColorPaletteUpdate(colorName, newColor)
                        )}
                      />
                      <TextField
                        fullWidth
                        size="small"
                        label="Color Value"
                        value={colorValue}
                        onChange={(e) => handleColorPaletteUpdate(colorName, e.target.value)}
                      />
                      <IconButton
                        size="small"
                        onClick={() => handleColorPaletteUpdate(colorName, undefined as any)}
                        color="error"
                      >
                        <DeleteIcon />
                      </IconButton>
                    </Box>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>

        {Object.keys(config.visualTheme.colorPalette).length === 0 && (
          <Typography color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
            No colors defined. Add colors to customize the visual appearance.
          </Typography>
        )}
      </Paper>

      {/* Tile Sprites */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">
            <ImageIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
            Tile Sprites
          </Typography>
          <Button
            startIcon={<AddIcon />}
            onClick={addNewTileSprite}
            size="small"
            variant="outlined"
          >
            Add Tile Sprite
          </Button>
        </Box>

        <Grid container spacing={2}>
          {Object.entries(config.visualTheme.tileSprites).map(([tileType, spritePath]) => (
            <Grid item xs={12} sm={6} key={tileType}>
              <Card variant="outlined">
                <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                  <Stack spacing={1}>
                    <TextField
                      fullWidth
                      size="small"
                      label="Tile Type"
                      value={tileType}
                      onChange={(e) => {
                        const newTileSprites = { ...config.visualTheme.tileSprites };
                        delete newTileSprites[tileType];
                        if (e.target.value) {
                          newTileSprites[e.target.value] = spritePath;
                        }
                        handleVisualThemeUpdate({ tileSprites: newTileSprites });
                      }}
                    />
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <TextField
                        fullWidth
                        size="small"
                        label="Sprite Path"
                        value={spritePath}
                        onChange={(e) => handleTileSpriteUpdate(tileType, e.target.value)}
                        placeholder="path/to/sprite.png"
                      />
                      <IconButton
                        size="small"
                        onClick={() => handleTileSpriteUpdate(tileType, undefined as any)}
                        color="error"
                      >
                        <DeleteIcon />
                      </IconButton>
                    </Box>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>

        {Object.keys(config.visualTheme.tileSprites).length === 0 && (
          <Typography color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
            No tile sprites defined. Add sprite paths to customize tile appearance.
          </Typography>
        )}
      </Paper>

      {/* Entity Sprites */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">
            <ImageIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
            Entity Sprites
          </Typography>
          <Button
            startIcon={<AddIcon />}
            onClick={addNewEntitySprite}
            size="small"
            variant="outlined"
          >
            Add Entity Sprite
          </Button>
        </Box>

        <Grid container spacing={2}>
          {Object.entries(config.visualTheme.entitySprites).map(([entityType, spritePath]) => (
            <Grid item xs={12} sm={6} key={entityType}>
              <Card variant="outlined">
                <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                  <Stack spacing={1}>
                    <TextField
                      fullWidth
                      size="small"
                      label="Entity Type"
                      value={entityType}
                      onChange={(e) => {
                        const newEntitySprites = { ...config.visualTheme.entitySprites };
                        delete newEntitySprites[entityType];
                        if (e.target.value) {
                          newEntitySprites[e.target.value] = spritePath;
                        }
                        handleVisualThemeUpdate({ entitySprites: newEntitySprites });
                      }}
                    />
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <TextField
                        fullWidth
                        size="small"
                        label="Sprite Path"
                        value={spritePath}
                        onChange={(e) => handleEntitySpriteUpdate(entityType, e.target.value)}
                        placeholder="path/to/sprite.png"
                      />
                      <IconButton
                        size="small"
                        onClick={() => handleEntitySpriteUpdate(entityType, undefined as any)}
                        color="error"
                      >
                        <DeleteIcon />
                      </IconButton>
                    </Box>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>

        {Object.keys(config.visualTheme.entitySprites).length === 0 && (
          <Typography color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
            No entity sprites defined. Add sprite paths to customize entity appearance.
          </Typography>
        )}
      </Paper>

      {/* Effect Settings */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">
            <PaletteIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
            Effect Settings
          </Typography>
          <Button
            startIcon={<AddIcon />}
            onClick={addNewEffectSetting}
            size="small"
            variant="outlined"
          >
            Add Effect
          </Button>
        </Box>

        <Grid container spacing={2}>
          {Object.entries(config.visualTheme.effectSettings).map(([settingName, value]) => (
            <Grid item xs={12} sm={6} key={settingName}>
              <Card variant="outlined">
                <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                  <Stack spacing={1}>
                    <TextField
                      fullWidth
                      size="small"
                      label="Setting Name"
                      value={settingName}
                      onChange={(e) => {
                        const newEffectSettings = { ...config.visualTheme.effectSettings };
                        delete newEffectSettings[settingName];
                        if (e.target.value) {
                          newEffectSettings[e.target.value] = value;
                        }
                        handleVisualThemeUpdate({ effectSettings: newEffectSettings });
                      }}
                    />
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <TextField
                        fullWidth
                        size="small"
                        label="Value"
                        value={typeof value === 'object' ? JSON.stringify(value) : value}
                        onChange={(e) => {
                          let parsedValue: any = e.target.value;
                          try {
                            if (e.target.value.startsWith('{') || e.target.value.startsWith('[')) {
                              parsedValue = JSON.parse(e.target.value);
                            } else if (!isNaN(Number(e.target.value)) && e.target.value !== '') {
                              parsedValue = Number(e.target.value);
                            } else if (e.target.value === 'true' || e.target.value === 'false') {
                              parsedValue = e.target.value === 'true';
                            }
                          } catch {
                            // Keep as string if parsing fails
                          }
                          handleEffectSettingUpdate(settingName, parsedValue);
                        }}
                      />
                      <IconButton
                        size="small"
                        onClick={() => handleEffectSettingUpdate(settingName, undefined)}
                        color="error"
                      >
                        <DeleteIcon />
                      </IconButton>
                    </Box>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>

        {Object.keys(config.visualTheme.effectSettings).length === 0 && (
          <Typography color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
            No effect settings defined. Add settings to customize visual effects.
          </Typography>
        )}
      </Paper>

      <ColorPickerDialog
        open={colorDialogOpen}
        title={colorDialogTitle}
        initialColor={colorDialogInitial}
        onClose={() => setColorDialogOpen(false)}
        onSave={colorDialogCallback}
      />
    </Stack>
  );
};