import React, { useCallback } from 'react';
import {
  Box,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
  Grid,
  Slider,
  Paper,
  FormHelperText,
  Chip,
  Stack
} from '@mui/material';
import { GenerationConfig, ValidationError } from '../../types';
import { GENERATION_ALGORITHMS, TERRAIN_TYPES } from '../../schemas';

interface TerrainConfigPanelProps {
  config: GenerationConfig;
  onChange: (updates: Partial<GenerationConfig>) => void;
  validationErrors: ValidationError[];
}

export const TerrainConfigPanel: React.FC<TerrainConfigPanelProps> = ({
  config,
  onChange,
  validationErrors
}) => {
  const getFieldError = useCallback((fieldName: string) => {
    return validationErrors.find(error => error.field === fieldName || error.field.startsWith(fieldName + '.'));
  }, [validationErrors]);

  const handleDimensionChange = useCallback((field: 'width' | 'height', value: number) => {
    onChange({ [field]: value });
  }, [onChange]);

  const handleSeedChange = useCallback((value: number) => {
    onChange({ seed: value });
  }, [onChange]);

  const handleAlgorithmChange = useCallback((algorithm: string) => {
    // Reset algorithm parameters when changing algorithm
    const defaultParams: Record<string, Record<string, any>> = {
      perlin: {
        scale: 0.1,
        octaves: 4,
        persistence: 0.5,
        lacunarity: 2.0,
        threshold: 0.0
      },
      cellular: {
        initialDensity: 0.45,
        iterations: 5,
        birthLimit: 4,
        deathLimit: 3
      },
      maze: {
        wallThickness: 1,
        pathWidth: 2,
        complexity: 0.75
      },
      rooms: {
        minRoomSize: 4,
        maxRoomSize: 12,
        roomCount: 8,
        corridorWidth: 2
      }
    };

    onChange({
      generationAlgorithm: algorithm,
      algorithmParameters: defaultParams[algorithm] || {}
    });
  }, [onChange]);

  const handleAlgorithmParameterChange = useCallback((paramName: string, value: any) => {
    onChange({
      algorithmParameters: {
        ...config.algorithmParameters,
        [paramName]: value
      }
    });
  }, [config.algorithmParameters, onChange]);

  const handleTerrainTypeToggle = useCallback((terrainType: string) => {
    const currentTypes = config.terrainTypes || [];
    const isSelected = currentTypes.includes(terrainType);
    
    if (isSelected) {
      // Don't allow removing the last terrain type
      if (currentTypes.length > 1) {
        onChange({
          terrainTypes: currentTypes.filter(type => type !== terrainType)
        });
      }
    } else {
      onChange({
        terrainTypes: [...currentTypes, terrainType]
      });
    }
  }, [config.terrainTypes, onChange]);

  const renderAlgorithmParameters = () => {
    const algorithm = config.generationAlgorithm;
    const params = config.algorithmParameters || {};

    switch (algorithm) {
      case 'perlin':
        return (
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Scale: {params.scale || 0.1}</Typography>
              <Slider
                value={params.scale || 0.1}
                onChange={(_, value) => handleAlgorithmParameterChange('scale', value)}
                min={0.01}
                max={1.0}
                step={0.01}
                valueLabelDisplay="auto"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Octaves: {params.octaves || 4}</Typography>
              <Slider
                value={params.octaves || 4}
                onChange={(_, value) => handleAlgorithmParameterChange('octaves', value)}
                min={1}
                max={8}
                step={1}
                valueLabelDisplay="auto"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Persistence: {params.persistence || 0.5}</Typography>
              <Slider
                value={params.persistence || 0.5}
                onChange={(_, value) => handleAlgorithmParameterChange('persistence', value)}
                min={0.1}
                max={1.0}
                step={0.1}
                valueLabelDisplay="auto"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Lacunarity: {params.lacunarity || 2.0}</Typography>
              <Slider
                value={params.lacunarity || 2.0}
                onChange={(_, value) => handleAlgorithmParameterChange('lacunarity', value)}
                min={1.0}
                max={4.0}
                step={0.1}
                valueLabelDisplay="auto"
              />
            </Grid>
          </Grid>
        );

      case 'cellular':
        return (
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Initial Density: {params.initialDensity || 0.45}</Typography>
              <Slider
                value={params.initialDensity || 0.45}
                onChange={(_, value) => handleAlgorithmParameterChange('initialDensity', value)}
                min={0.1}
                max={0.9}
                step={0.05}
                valueLabelDisplay="auto"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Iterations: {params.iterations || 5}</Typography>
              <Slider
                value={params.iterations || 5}
                onChange={(_, value) => handleAlgorithmParameterChange('iterations', value)}
                min={1}
                max={10}
                step={1}
                valueLabelDisplay="auto"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Birth Limit: {params.birthLimit || 4}</Typography>
              <Slider
                value={params.birthLimit || 4}
                onChange={(_, value) => handleAlgorithmParameterChange('birthLimit', value)}
                min={1}
                max={8}
                step={1}
                valueLabelDisplay="auto"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Death Limit: {params.deathLimit || 3}</Typography>
              <Slider
                value={params.deathLimit || 3}
                onChange={(_, value) => handleAlgorithmParameterChange('deathLimit', value)}
                min={1}
                max={8}
                step={1}
                valueLabelDisplay="auto"
              />
            </Grid>
          </Grid>
        );

      case 'maze':
        return (
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Wall Thickness: {params.wallThickness || 1}</Typography>
              <Slider
                value={params.wallThickness || 1}
                onChange={(_, value) => handleAlgorithmParameterChange('wallThickness', value)}
                min={1}
                max={3}
                step={1}
                valueLabelDisplay="auto"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Path Width: {params.pathWidth || 2}</Typography>
              <Slider
                value={params.pathWidth || 2}
                onChange={(_, value) => handleAlgorithmParameterChange('pathWidth', value)}
                min={1}
                max={5}
                step={1}
                valueLabelDisplay="auto"
              />
            </Grid>
            <Grid item xs={12}>
              <Typography gutterBottom>Complexity: {params.complexity || 0.75}</Typography>
              <Slider
                value={params.complexity || 0.75}
                onChange={(_, value) => handleAlgorithmParameterChange('complexity', value)}
                min={0.1}
                max={1.0}
                step={0.05}
                valueLabelDisplay="auto"
              />
            </Grid>
          </Grid>
        );

      case 'rooms':
        return (
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Min Room Size: {params.minRoomSize || 4}</Typography>
              <Slider
                value={params.minRoomSize || 4}
                onChange={(_, value) => handleAlgorithmParameterChange('minRoomSize', value)}
                min={3}
                max={10}
                step={1}
                valueLabelDisplay="auto"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Max Room Size: {params.maxRoomSize || 12}</Typography>
              <Slider
                value={params.maxRoomSize || 12}
                onChange={(_, value) => handleAlgorithmParameterChange('maxRoomSize', value)}
                min={6}
                max={20}
                step={1}
                valueLabelDisplay="auto"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Room Count: {params.roomCount || 8}</Typography>
              <Slider
                value={params.roomCount || 8}
                onChange={(_, value) => handleAlgorithmParameterChange('roomCount', value)}
                min={3}
                max={20}
                step={1}
                valueLabelDisplay="auto"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography gutterBottom>Corridor Width: {params.corridorWidth || 2}</Typography>
              <Slider
                value={params.corridorWidth || 2}
                onChange={(_, value) => handleAlgorithmParameterChange('corridorWidth', value)}
                min={1}
                max={4}
                step={1}
                valueLabelDisplay="auto"
              />
            </Grid>
          </Grid>
        );

      default:
        return (
          <Typography color="text.secondary">
            Select a generation algorithm to configure parameters
          </Typography>
        );
    }
  };

  return (
    <Stack spacing={3}>
      {/* Level Dimensions */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Typography variant="h6" gutterBottom>
          Level Dimensions
        </Typography>
        <Grid container spacing={2}>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Width (tiles)"
              type="number"
              value={config.width}
              onChange={(e) => handleDimensionChange('width', parseInt(e.target.value) || 0)}
              error={!!getFieldError('width')}
              helperText={getFieldError('width')?.message || 'Level width in tiles (10-1000)'}
              inputProps={{ min: 10, max: 1000 }}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Height (tiles)"
              type="number"
              value={config.height}
              onChange={(e) => handleDimensionChange('height', parseInt(e.target.value) || 0)}
              error={!!getFieldError('height')}
              helperText={getFieldError('height')?.message || 'Level height in tiles (10-1000)'}
              inputProps={{ min: 10, max: 1000 }}
            />
          </Grid>
        </Grid>
      </Paper>

      {/* Generation Settings */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Typography variant="h6" gutterBottom>
          Generation Settings
        </Typography>
        <Grid container spacing={2}>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Random Seed"
              type="number"
              value={config.seed}
              onChange={(e) => handleSeedChange(parseInt(e.target.value) || 0)}
              error={!!getFieldError('seed')}
              helperText={getFieldError('seed')?.message || 'Seed for reproducible generation (0 = random)'}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <FormControl fullWidth error={!!getFieldError('generationAlgorithm')}>
              <InputLabel>Generation Algorithm</InputLabel>
              <Select
                value={config.generationAlgorithm}
                label="Generation Algorithm"
                onChange={(e) => handleAlgorithmChange(e.target.value)}
              >
                {GENERATION_ALGORITHMS.map((algorithm) => (
                  <MenuItem key={algorithm} value={algorithm}>
                    {algorithm.charAt(0).toUpperCase() + algorithm.slice(1)}
                  </MenuItem>
                ))}
              </Select>
              <FormHelperText>
                {getFieldError('generationAlgorithm')?.message || 'Algorithm used for terrain generation'}
              </FormHelperText>
            </FormControl>
          </Grid>
        </Grid>
      </Paper>

      {/* Algorithm Parameters */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Typography variant="h6" gutterBottom>
          Algorithm Parameters
        </Typography>
        {renderAlgorithmParameters()}
      </Paper>

      {/* Terrain Types */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Typography variant="h6" gutterBottom>
          Terrain Types
        </Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Select which terrain types to include in generation
        </Typography>
        <Box sx={{ mt: 2 }}>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            {TERRAIN_TYPES.map((terrainType) => (
              <Chip
                key={terrainType}
                label={terrainType.charAt(0).toUpperCase() + terrainType.slice(1)}
                onClick={() => handleTerrainTypeToggle(terrainType)}
                color={config.terrainTypes?.includes(terrainType) ? 'primary' : 'default'}
                variant={config.terrainTypes?.includes(terrainType) ? 'filled' : 'outlined'}
                clickable
              />
            ))}
          </Stack>
          {getFieldError('terrainTypes') && (
            <FormHelperText error sx={{ mt: 1 }}>
              {getFieldError('terrainTypes')?.message}
            </FormHelperText>
          )}
        </Box>
      </Paper>
    </Stack>
  );
};