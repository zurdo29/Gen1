import React, { useState, useEffect } from 'react';
import {
  Paper,
  Typography,
  TextField,
  Button,
  Box,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Slider,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  Alert,
  Switch,
  FormControlLabel,
  Divider,
} from '@mui/material';
import {
  Save as SaveIcon,
  Refresh as RefreshIcon,
  ExpandMore as ExpandMoreIcon,
  Shuffle as ShuffleIcon,
  Settings as SettingsIcon,
} from '@mui/icons-material';

interface EntityConfig {
  type: string;
  count: number;
  enabled: boolean;
}

interface AdvancedConfig {
  // Basic settings
  width: number;
  height: number;
  seed: number;
  algorithm: string;
  
  // Terrain settings
  terrainDensity: number;
  terrainTypes: string[];
  
  // Entity settings
  entities: EntityConfig[];
  
  // Advanced settings
  smoothingPasses: number;
  borderWalls: boolean;
  ensureConnectivity: boolean;
  
  // Visual settings
  theme: string;
}

interface ValidationError {
  field: string;
  message: string;
}

interface AdvancedConfigurationPanelProps {
  config: AdvancedConfig;
  onChange: (config: AdvancedConfig) => void;
  onGenerate: () => void;
  isGenerating?: boolean;
}

const defaultEntityTypes = [
  { type: 'player', label: 'Player Spawn', defaultCount: 1 },
  { type: 'enemy', label: 'Enemies', defaultCount: 5 },
  { type: 'item', label: 'Items', defaultCount: 8 },
  { type: 'exit', label: 'Exit Points', defaultCount: 1 },
  { type: 'checkpoint', label: 'Checkpoints', defaultCount: 2 },
];

const terrainTypeOptions = [
  'floor', 'wall', 'water', 'grass', 'stone', 'sand', 'lava'
];

const algorithmOptions = [
  { value: 'perlin-noise', label: 'Perlin Noise', description: 'Smooth, natural-looking terrain' },
  { value: 'cellular-automata', label: 'Cellular Automata', description: 'Cave-like structures' },
  { value: 'maze', label: 'Maze Generator', description: 'Structured maze patterns' },
  { value: 'random', label: 'Random', description: 'Completely random placement' },
  { value: 'rooms', label: 'Room-based', description: 'Connected rooms and corridors' },
];

export const AdvancedConfigurationPanel: React.FC<AdvancedConfigurationPanelProps> = ({
  config,
  onChange,
  onGenerate,
  isGenerating = false
}) => {
  const [validationErrors, setValidationErrors] = useState<ValidationError[]>([]);
  const [expandedSections, setExpandedSections] = useState<string[]>(['basic']);

  const handleChange = (field: keyof AdvancedConfig, value: any) => {
    const newConfig = {
      ...config,
      [field]: value
    };
    onChange(newConfig);
    validateConfig(newConfig);
  };

  const handleEntityChange = (index: number, field: keyof EntityConfig, value: any) => {
    const newEntities = [...config.entities];
    newEntities[index] = { ...newEntities[index], [field]: value };
    handleChange('entities', newEntities);
  };

  const handleTerrainTypeToggle = (terrainType: string) => {
    const newTerrainTypes = config.terrainTypes.includes(terrainType)
      ? config.terrainTypes.filter(t => t !== terrainType)
      : [...config.terrainTypes, terrainType];
    handleChange('terrainTypes', newTerrainTypes);
  };

  const validateConfig = (configToValidate: AdvancedConfig) => {
    const errors: ValidationError[] = [];

    // Basic validation
    if (configToValidate.width < 10 || configToValidate.width > 200) {
      errors.push({ field: 'width', message: 'Width must be between 10 and 200' });
    }
    if (configToValidate.height < 10 || configToValidate.height > 200) {
      errors.push({ field: 'height', message: 'Height must be between 10 and 200' });
    }
    if (configToValidate.terrainTypes.length === 0) {
      errors.push({ field: 'terrainTypes', message: 'At least one terrain type must be selected' });
    }

    // Entity validation
    const playerSpawns = configToValidate.entities.find(e => e.type === 'player');
    if (!playerSpawns || !playerSpawns.enabled || playerSpawns.count === 0) {
      errors.push({ field: 'entities', message: 'At least one player spawn is required' });
    }

    // Size validation for performance
    const totalCells = configToValidate.width * configToValidate.height;
    if (totalCells > 10000) {
      errors.push({ field: 'size', message: 'Level size too large (max 10,000 cells for performance)' });
    }

    setValidationErrors(errors);
  };

  const handleRandomSeed = () => {
    handleChange('seed', Math.floor(Math.random() * 1000000));
  };

  const handleAccordionChange = (section: string) => (event: React.SyntheticEvent, isExpanded: boolean) => {
    setExpandedSections(prev => 
      isExpanded 
        ? [...prev, section]
        : prev.filter(s => s !== section)
    );
  };

  const resetToDefaults = () => {
    const defaultConfig: AdvancedConfig = {
      width: 50,
      height: 50,
      seed: Math.floor(Math.random() * 1000000),
      algorithm: 'perlin-noise',
      terrainDensity: 40,
      terrainTypes: ['floor', 'wall', 'water'],
      entities: defaultEntityTypes.map(et => ({
        type: et.type,
        count: et.defaultCount,
        enabled: true
      })),
      smoothingPasses: 1,
      borderWalls: true,
      ensureConnectivity: true,
      theme: 'default'
    };
    onChange(defaultConfig);
  };

  useEffect(() => {
    validateConfig(config);
  }, [config]);

  const isValid = validationErrors.length === 0;

  return (
    <Paper sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
        <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <SettingsIcon />
          Level Configuration
        </Typography>
      </Box>

      <Box sx={{ flex: 1, overflow: 'auto', p: 2 }}>
        {/* Validation Errors */}
        {validationErrors.length > 0 && (
          <Alert severity="error" sx={{ mb: 2 }}>
            <Typography variant="subtitle2" gutterBottom>
              Configuration Errors:
            </Typography>
            {validationErrors.map((error, index) => (
              <Typography key={index} variant="body2">
                • {error.message}
              </Typography>
            ))}
          </Alert>
        )}

        {/* Basic Settings */}
        <Accordion 
          expanded={expandedSections.includes('basic')}
          onChange={handleAccordionChange('basic')}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="subtitle1">Basic Settings</Typography>
          </AccordionSummary>
          <AccordionDetails>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <TextField
                    label="Width"
                    type="number"
                    value={config.width}
                    onChange={(e) => handleChange('width', parseInt(e.target.value) || 50)}
                    fullWidth
                    inputProps={{ min: 10, max: 200 }}
                    error={validationErrors.some(e => e.field === 'width')}
                  />
                </Grid>
                <Grid item xs={6}>
                  <TextField
                    label="Height"
                    type="number"
                    value={config.height}
                    onChange={(e) => handleChange('height', parseInt(e.target.value) || 50)}
                    fullWidth
                    inputProps={{ min: 10, max: 200 }}
                    error={validationErrors.some(e => e.field === 'height')}
                  />
                </Grid>
              </Grid>

              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <TextField
                  label="Seed"
                  type="number"
                  value={config.seed}
                  onChange={(e) => handleChange('seed', parseInt(e.target.value) || 0)}
                  sx={{ flexGrow: 1 }}
                />
                <Button
                  variant="outlined"
                  onClick={handleRandomSeed}
                  disabled={isGenerating}
                  startIcon={<ShuffleIcon />}
                >
                  Random
                </Button>
              </Box>

              <FormControl fullWidth>
                <InputLabel>Generation Algorithm</InputLabel>
                <Select
                  value={config.algorithm}
                  label="Generation Algorithm"
                  onChange={(e) => handleChange('algorithm', e.target.value)}
                >
                  {algorithmOptions.map(option => (
                    <MenuItem key={option.value} value={option.value}>
                      <Box>
                        <Typography variant="body2">{option.label}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {option.description}
                        </Typography>
                      </Box>
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Box>
          </AccordionDetails>
        </Accordion>

        {/* Terrain Settings */}
        <Accordion 
          expanded={expandedSections.includes('terrain')}
          onChange={handleAccordionChange('terrain')}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="subtitle1">Terrain Settings</Typography>
          </AccordionSummary>
          <AccordionDetails>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box>
                <Typography gutterBottom>
                  Terrain Density: {config.terrainDensity}%
                </Typography>
                <Slider
                  value={config.terrainDensity}
                  onChange={(_, value) => handleChange('terrainDensity', value as number)}
                  min={10}
                  max={90}
                  step={5}
                  marks
                  valueLabelDisplay="auto"
                  disabled={isGenerating}
                />
              </Box>

              <Box>
                <Typography variant="subtitle2" gutterBottom>
                  Terrain Types
                </Typography>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                  {terrainTypeOptions.map(terrainType => (
                    <Chip
                      key={terrainType}
                      label={terrainType}
                      onClick={() => handleTerrainTypeToggle(terrainType)}
                      color={config.terrainTypes.includes(terrainType) ? 'primary' : 'default'}
                      variant={config.terrainTypes.includes(terrainType) ? 'filled' : 'outlined'}
                    />
                  ))}
                </Box>
                {validationErrors.some(e => e.field === 'terrainTypes') && (
                  <Typography variant="caption" color="error" sx={{ mt: 1, display: 'block' }}>
                    At least one terrain type must be selected
                  </Typography>
                )}
              </Box>
            </Box>
          </AccordionDetails>
        </Accordion>

        {/* Entity Settings */}
        <Accordion 
          expanded={expandedSections.includes('entities')}
          onChange={handleAccordionChange('entities')}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="subtitle1">Entity Settings</Typography>
          </AccordionSummary>
          <AccordionDetails>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              {config.entities.map((entity, index) => {
                const entityType = defaultEntityTypes.find(et => et.type === entity.type);
                return (
                  <Box key={entity.type} sx={{ p: 2, border: 1, borderColor: 'divider', borderRadius: 1 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                      <Typography variant="subtitle2">
                        {entityType?.label || entity.type}
                      </Typography>
                      <FormControlLabel
                        control={
                          <Switch
                            checked={entity.enabled}
                            onChange={(e) => handleEntityChange(index, 'enabled', e.target.checked)}
                          />
                        }
                        label="Enabled"
                      />
                    </Box>
                    {entity.enabled && (
                      <TextField
                        label="Count"
                        type="number"
                        value={entity.count}
                        onChange={(e) => handleEntityChange(index, 'count', parseInt(e.target.value) || 0)}
                        fullWidth
                        inputProps={{ min: 0, max: 50 }}
                        size="small"
                      />
                    )}
                  </Box>
                );
              })}
            </Box>
          </AccordionDetails>
        </Accordion>

        {/* Advanced Settings */}
        <Accordion 
          expanded={expandedSections.includes('advanced')}
          onChange={handleAccordionChange('advanced')}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="subtitle1">Advanced Settings</Typography>
          </AccordionSummary>
          <AccordionDetails>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <TextField
                label="Smoothing Passes"
                type="number"
                value={config.smoothingPasses}
                onChange={(e) => handleChange('smoothingPasses', parseInt(e.target.value) || 0)}
                fullWidth
                inputProps={{ min: 0, max: 5 }}
                helperText="Number of smoothing passes to apply to terrain"
              />

              <FormControlLabel
                control={
                  <Switch
                    checked={config.borderWalls}
                    onChange={(e) => handleChange('borderWalls', e.target.checked)}
                  />
                }
                label="Border Walls"
              />

              <FormControlLabel
                control={
                  <Switch
                    checked={config.ensureConnectivity}
                    onChange={(e) => handleChange('ensureConnectivity', e.target.checked)}
                  />
                }
                label="Ensure Connectivity"
              />

              <FormControl fullWidth>
                <InputLabel>Visual Theme</InputLabel>
                <Select
                  value={config.theme}
                  label="Visual Theme"
                  onChange={(e) => handleChange('theme', e.target.value)}
                >
                  <MenuItem value="default">Default</MenuItem>
                  <MenuItem value="dark">Dark</MenuItem>
                  <MenuItem value="nature">Nature</MenuItem>
                  <MenuItem value="sci-fi">Sci-Fi</MenuItem>
                </Select>
              </FormControl>
            </Box>
          </AccordionDetails>
        </Accordion>
      </Box>

      <Divider />
      
      {/* Action Buttons */}
      <Box sx={{ p: 2, display: 'flex', gap: 2 }}>
        <Button
          variant="contained"
          startIcon={<RefreshIcon />}
          onClick={onGenerate}
          disabled={isGenerating || !isValid}
          fullWidth
        >
          {isGenerating ? 'Generating...' : 'Generate Level'}
        </Button>
        <Button
          variant="outlined"
          startIcon={<SaveIcon />}
          disabled={isGenerating}
        >
          Save
        </Button>
        <Button
          variant="text"
          onClick={resetToDefaults}
          disabled={isGenerating}
        >
          Reset
        </Button>
      </Box>
    </Paper>
  );
};