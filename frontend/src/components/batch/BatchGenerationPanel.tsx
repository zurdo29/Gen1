import React, { useState, useCallback, useMemo } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  Stack,
  Grid,
  Alert,
  LinearProgress,
  IconButton,
  Tooltip,
  Divider
} from '@mui/material';
import {
  Add as AddIcon,
  Remove as RemoveIcon,
  PlayArrow as PlayIcon,
  Stop as StopIcon
} from '@mui/icons-material';
import { GenerationConfig, ConfigVariation, BatchGenerationRequest } from '../../types';

interface BatchGenerationPanelProps {
  baseConfig: GenerationConfig;
  onStartBatch: (request: BatchGenerationRequest) => Promise<void>;
  onCancelBatch: () => Promise<void>;
  isGenerating: boolean;
  progress: number;
  currentBatch?: number;
  totalBatches?: number;
  disabled?: boolean;
}

interface VariationConfig {
  id: string;
  parameter: string;
  parameterLabel: string;
  values: any[];
  valueType: 'number' | 'string' | 'boolean';
}

const AVAILABLE_PARAMETERS = [
  { key: 'seed', label: 'Seed', type: 'number' as const },
  { key: 'width', label: 'Width', type: 'number' as const },
  { key: 'height', label: 'Height', type: 'number' as const },
  { key: 'generationAlgorithm', label: 'Algorithm', type: 'string' as const },
  { key: 'visualTheme.themeName', label: 'Visual Theme', type: 'string' as const },
  { key: 'gameplay.difficulty', label: 'Difficulty', type: 'string' as const },
  { key: 'gameplay.playerSpeed', label: 'Player Speed', type: 'number' as const },
  { key: 'gameplay.timeLimit', label: 'Time Limit', type: 'number' as const }
];

const ALGORITHM_OPTIONS = ['perlin', 'cellular', 'maze', 'rooms'];
const THEME_OPTIONS = ['default', 'dark', 'forest', 'desert', 'ice'];
const DIFFICULTY_OPTIONS = ['easy', 'normal', 'hard', 'expert'];

export const BatchGenerationPanel: React.FC<BatchGenerationPanelProps> = ({
  baseConfig,
  onStartBatch,
  onCancelBatch,
  isGenerating,
  progress,
  currentBatch,
  totalBatches,
  disabled = false
}) => {
  const [variations, setVariations] = useState<VariationConfig[]>([]);
  const [batchCount, setBatchCount] = useState<number>(5);
  const [maxBatchCount, setMaxBatchCount] = useState<number>(50);

  // Calculate total combinations
  const totalCombinations = useMemo(() => {
    if (variations.length === 0) return batchCount;
    
    const variationCombinations = variations.reduce((total, variation) => {
      return total * Math.max(variation.values.length, 1);
    }, 1);
    
    return Math.min(variationCombinations * batchCount, maxBatchCount);
  }, [variations, batchCount, maxBatchCount]);

  const addVariation = useCallback(() => {
    const newVariation: VariationConfig = {
      id: `variation-${Date.now()}`,
      parameter: 'seed',
      parameterLabel: 'Seed',
      values: [],
      valueType: 'number'
    };
    setVariations(prev => [...prev, newVariation]);
  }, []);

  const removeVariation = useCallback((id: string) => {
    setVariations(prev => prev.filter(v => v.id !== id));
  }, []);

  const updateVariation = useCallback((id: string, updates: Partial<VariationConfig>) => {
    setVariations(prev => prev.map(v => 
      v.id === id ? { ...v, ...updates } : v
    ));
  }, []);

  const updateVariationParameter = useCallback((id: string, parameter: string) => {
    const paramConfig = AVAILABLE_PARAMETERS.find(p => p.key === parameter);
    if (!paramConfig) return;

    updateVariation(id, {
      parameter,
      parameterLabel: paramConfig.label,
      valueType: paramConfig.type,
      values: [] // Reset values when parameter changes
    });
  }, [updateVariation]);

  const addVariationValue = useCallback((id: string, value: any) => {
    setVariations(prev => prev.map(v => 
      v.id === id ? { ...v, values: [...v.values, value] } : v
    ));
  }, []);

  const removeVariationValue = useCallback((id: string, index: number) => {
    setVariations(prev => prev.map(v => 
      v.id === id ? { ...v, values: v.values.filter((_, i) => i !== index) } : v
    ));
  }, []);

  const getDefaultValueForParameter = useCallback((parameter: string, type: string) => {
    switch (parameter) {
      case 'seed':
        return Math.floor(Math.random() * 100000);
      case 'width':
      case 'height':
        return 20;
      case 'generationAlgorithm':
        return ALGORITHM_OPTIONS[0];
      case 'visualTheme.themeName':
        return THEME_OPTIONS[0];
      case 'gameplay.difficulty':
        return DIFFICULTY_OPTIONS[0];
      case 'gameplay.playerSpeed':
        return 5;
      case 'gameplay.timeLimit':
        return 300;
      default:
        return type === 'number' ? 0 : '';
    }
  }, []);

  const getOptionsForParameter = useCallback((parameter: string) => {
    switch (parameter) {
      case 'generationAlgorithm':
        return ALGORITHM_OPTIONS;
      case 'visualTheme.themeName':
        return THEME_OPTIONS;
      case 'gameplay.difficulty':
        return DIFFICULTY_OPTIONS;
      default:
        return [];
    }
  }, []);

  const handleStartBatch = useCallback(async () => {
    const configVariations: ConfigVariation[] = variations.map(v => ({
      parameter: v.parameter,
      values: v.values
    }));

    const request: BatchGenerationRequest = {
      baseConfig,
      variations: configVariations,
      count: batchCount
    };

    await onStartBatch(request);
  }, [baseConfig, variations, batchCount, onStartBatch]);

  const canStartBatch = useMemo(() => {
    return !disabled && !isGenerating && batchCount > 0 && batchCount <= maxBatchCount;
  }, [disabled, isGenerating, batchCount, maxBatchCount]);

  return (
    <Card>
      <CardContent>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">
            Batch Generation
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button
              variant="outlined"
              startIcon={<AddIcon />}
              onClick={addVariation}
              disabled={disabled || isGenerating}
              size="small"
            >
              Add Variation
            </Button>
            {isGenerating ? (
              <Button
                variant="outlined"
                color="error"
                startIcon={<StopIcon />}
                onClick={onCancelBatch}
                size="small"
              >
                Cancel
              </Button>
            ) : (
              <Button
                variant="contained"
                startIcon={<PlayIcon />}
                onClick={handleStartBatch}
                disabled={!canStartBatch}
                size="small"
              >
                Start Batch
              </Button>
            )}
          </Stack>
        </Box>

        {/* Batch Configuration */}
        <Grid container spacing={2} sx={{ mb: 2 }}>
          <Grid item xs={6}>
            <TextField
              label="Batch Count"
              type="number"
              value={batchCount}
              onChange={(e) => setBatchCount(Math.max(1, parseInt(e.target.value) || 1))}
              disabled={disabled || isGenerating}
              fullWidth
              size="small"
              inputProps={{ min: 1, max: maxBatchCount }}
              helperText={`Generate ${batchCount} level${batchCount !== 1 ? 's' : ''} per variation`}
            />
          </Grid>
          <Grid item xs={6}>
            <TextField
              label="Max Total Levels"
              type="number"
              value={maxBatchCount}
              onChange={(e) => setMaxBatchCount(Math.max(1, parseInt(e.target.value) || 1))}
              disabled={disabled || isGenerating}
              fullWidth
              size="small"
              inputProps={{ min: 1, max: 1000 }}
              helperText={`Maximum ${maxBatchCount} levels total`}
            />
          </Grid>
        </Grid>

        {/* Total Combinations Display */}
        <Alert severity="info" sx={{ mb: 2 }}>
          <Typography variant="body2">
            <strong>Total levels to generate:</strong> {totalCombinations}
            {variations.length > 0 && (
              <>
                <br />
                <strong>Combinations:</strong> {variations.length} variation{variations.length !== 1 ? 's' : ''} × {batchCount} level{batchCount !== 1 ? 's' : ''} each
              </>
            )}
          </Typography>
        </Alert>

        {/* Progress Display */}
        {isGenerating && (
          <Box sx={{ mb: 2 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="body2">
                Generating batch...
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {currentBatch && totalBatches ? `${currentBatch}/${totalBatches}` : `${progress}%`}
              </Typography>
            </Box>
            <LinearProgress variant="determinate" value={progress} />
          </Box>
        )}

        <Divider sx={{ my: 2 }} />

        {/* Parameter Variations */}
        <Typography variant="subtitle1" sx={{ mb: 2 }}>
          Parameter Variations
        </Typography>

        {variations.length === 0 ? (
          <Alert severity="info">
            No parameter variations configured. Add variations to generate multiple levels with different settings.
          </Alert>
        ) : (
          <Stack spacing={2}>
            {variations.map((variation) => (
              <VariationEditor
                key={variation.id}
                variation={variation}
                onUpdate={(updates) => updateVariation(variation.id, updates)}
                onUpdateParameter={(parameter) => updateVariationParameter(variation.id, parameter)}
                onAddValue={(value) => addVariationValue(variation.id, value)}
                onRemoveValue={(index) => removeVariationValue(variation.id, index)}
                onRemove={() => removeVariation(variation.id)}
                getDefaultValue={getDefaultValueForParameter}
                getOptions={getOptionsForParameter}
                disabled={disabled || isGenerating}
              />
            ))}
          </Stack>
        )}
      </CardContent>
    </Card>
  );
};

interface VariationEditorProps {
  variation: VariationConfig;
  onUpdate: (updates: Partial<VariationConfig>) => void;
  onUpdateParameter: (parameter: string) => void;
  onAddValue: (value: any) => void;
  onRemoveValue: (index: number) => void;
  onRemove: () => void;
  getDefaultValue: (parameter: string, type: string) => any;
  getOptions: (parameter: string) => string[];
  disabled: boolean;
}

const VariationEditor: React.FC<VariationEditorProps> = ({
  variation,
  onUpdateParameter,
  onAddValue,
  onRemoveValue,
  onRemove,
  getDefaultValue,
  getOptions,
  disabled
}) => {
  const [newValue, setNewValue] = useState<string>('');

  const handleAddValue = useCallback(() => {
    if (!newValue.trim()) return;

    let parsedValue: any = newValue;
    if (variation.valueType === 'number') {
      parsedValue = parseFloat(newValue);
      if (isNaN(parsedValue)) return;
    } else if (variation.valueType === 'boolean') {
      parsedValue = newValue.toLowerCase() === 'true';
    }

    onAddValue(parsedValue);
    setNewValue('');
  }, [newValue, variation.valueType, onAddValue]);

  const handleAddDefaultValue = useCallback(() => {
    const defaultValue = getDefaultValue(variation.parameter, variation.valueType);
    onAddValue(defaultValue);
  }, [variation.parameter, variation.valueType, getDefaultValue, onAddValue]);

  const options = getOptions(variation.parameter);

  return (
    <Card variant="outlined">
      <CardContent>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="subtitle2">
            {variation.parameterLabel} Variation
          </Typography>
          <Tooltip title="Remove variation">
            <IconButton
              size="small"
              onClick={onRemove}
              disabled={disabled}
              color="error"
            >
              <RemoveIcon />
            </IconButton>
          </Tooltip>
        </Box>

        <Grid container spacing={2} sx={{ mb: 2 }}>
          <Grid item xs={6}>
            <FormControl fullWidth size="small">
              <InputLabel>Parameter</InputLabel>
              <Select
                value={variation.parameter}
                onChange={(e) => onUpdateParameter(e.target.value)}
                disabled={disabled}
                label="Parameter"
              >
                {AVAILABLE_PARAMETERS.map((param) => (
                  <MenuItem key={param.key} value={param.key}>
                    {param.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={6}>
            <Box sx={{ display: 'flex', gap: 1 }}>
              {options.length > 0 ? (
                <FormControl fullWidth size="small">
                  <InputLabel>Add Value</InputLabel>
                  <Select
                    value=""
                    onChange={(e) => {
                      onAddValue(e.target.value);
                    }}
                    disabled={disabled}
                    label="Add Value"
                  >
                    {options.map((option) => (
                      <MenuItem key={option} value={option}>
                        {option}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              ) : (
                <TextField
                  label="Add Value"
                  value={newValue}
                  onChange={(e) => setNewValue(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && handleAddValue()}
                  disabled={disabled}
                  size="small"
                  fullWidth
                  type={variation.valueType === 'number' ? 'number' : 'text'}
                />
              )}
              <Button
                variant="outlined"
                onClick={options.length > 0 ? handleAddDefaultValue : handleAddValue}
                disabled={disabled || (!newValue.trim() && options.length === 0)}
                size="small"
              >
                <AddIcon />
              </Button>
            </Box>
          </Grid>
        </Grid>

        {/* Values Display */}
        <Box>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            Values ({variation.values.length}):
          </Typography>
          {variation.values.length === 0 ? (
            <Typography variant="body2" color="text.secondary" fontStyle="italic">
              No values added yet
            </Typography>
          ) : (
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
              {variation.values.map((value, index) => (
                <Chip
                  key={index}
                  label={String(value)}
                  onDelete={disabled ? undefined : () => onRemoveValue(index)}
                  size="small"
                  variant="outlined"
                />
              ))}
            </Box>
          )}
        </Box>
      </CardContent>
    </Card>
  );
};