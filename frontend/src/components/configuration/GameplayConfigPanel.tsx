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
  Paper,
  Stack,
  Slider,
  FormHelperText,
  Chip,
  FormControlLabel,
  Checkbox,
  Button,
  IconButton,
  Card,
  CardContent
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Speed as SpeedIcon,
  Favorite as HealthIcon,
  Timer as TimerIcon,
  EmojiEvents as VictoryIcon
} from '@mui/icons-material';
import { GenerationConfig, ValidationError } from '../../types';
import { DIFFICULTY_LEVELS, VICTORY_CONDITIONS } from '../../schemas';

interface GameplayConfigPanelProps {
  config: GenerationConfig;
  onChange: (updates: Partial<GenerationConfig>) => void;
  validationErrors: ValidationError[];
}

export const GameplayConfigPanel: React.FC<GameplayConfigPanelProps> = ({
  config,
  onChange,
  validationErrors
}) => {
  const getFieldError = useCallback((fieldName: string) => {
    return validationErrors.find(error => 
      error.field === `gameplay.${fieldName}` || 
      error.field.startsWith(`gameplay.${fieldName}.`)
    );
  }, [validationErrors]);

  const handleGameplayUpdate = useCallback((updates: Partial<typeof config.gameplay>) => {
    onChange({
      gameplay: {
        ...config.gameplay,
        ...updates
      }
    });
  }, [config.gameplay, onChange]);

  const handleVictoryConditionToggle = useCallback((condition: string) => {
    const currentConditions = config.gameplay.victoryConditions || [];
    const isSelected = currentConditions.includes(condition);
    
    if (isSelected) {
      // Don't allow removing the last victory condition
      if (currentConditions.length > 1) {
        handleGameplayUpdate({
          victoryConditions: currentConditions.filter(c => c !== condition)
        });
      }
    } else {
      handleGameplayUpdate({
        victoryConditions: [...currentConditions, condition]
      });
    }
  }, [config.gameplay.victoryConditions, handleGameplayUpdate]);

  const handleMechanicUpdate = useCallback((mechanicName: string, value: any) => {
    const newMechanics = { ...config.gameplay.mechanics };
    if (value === '' || value === null || value === undefined) {
      delete newMechanics[mechanicName];
    } else {
      newMechanics[mechanicName] = value;
    }
    handleGameplayUpdate({ mechanics: newMechanics });
  }, [config.gameplay.mechanics, handleGameplayUpdate]);

  const addNewMechanic = () => {
    const mechanicName = `mechanic_${Date.now()}`;
    handleMechanicUpdate(mechanicName, '');
  };

  const getDifficultyDescription = (difficulty: string): string => {
    const descriptions: Record<string, string> = {
      easy: 'Relaxed gameplay, forgiving mechanics',
      normal: 'Balanced challenge for most players',
      hard: 'Challenging gameplay requiring skill',
      extreme: 'Maximum difficulty for experts'
    };
    return descriptions[difficulty] || '';
  };

  const getVictoryConditionDescription = (condition: string): string => {
    const descriptions: Record<string, string> = {
      reach_exit: 'Player must reach the exit point',
      collect_all_items: 'Player must collect all items in the level',
      defeat_all_enemies: 'Player must defeat all enemies',
      survive_time: 'Player must survive for the specified time',
      reach_score: 'Player must reach a target score'
    };
    return descriptions[condition] || '';
  };

  return (
    <Stack spacing={3}>
      {/* Player Settings */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Typography variant="h6" gutterBottom>
          <SpeedIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
          Player Settings
        </Typography>
        <Grid container spacing={2}>
          <Grid item xs={12} sm={6}>
            <Typography gutterBottom>
              Player Speed: {config.gameplay.playerSpeed}
            </Typography>
            <Slider
              value={config.gameplay.playerSpeed}
              onChange={(_, value) => handleGameplayUpdate({ playerSpeed: value as number })}
              min={0.1}
              max={50}
              step={0.1}
              valueLabelDisplay="auto"
              marks={[
                { value: 0.1, label: 'Slow' },
                { value: 5, label: 'Normal' },
                { value: 25, label: 'Fast' },
                { value: 50, label: 'Very Fast' }
              ]}
            />
            {getFieldError('playerSpeed') && (
              <FormHelperText error>
                {getFieldError('playerSpeed')?.message}
              </FormHelperText>
            )}
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Player Health"
              type="number"
              value={config.gameplay.playerHealth}
              onChange={(e) => handleGameplayUpdate({ playerHealth: parseInt(e.target.value) || 1 })}
              error={!!getFieldError('playerHealth')}
              helperText={getFieldError('playerHealth')?.message || 'Player health points (1-10000)'}
              inputProps={{ min: 1, max: 10000 }}
              InputProps={{
                startAdornment: <HealthIcon sx={{ mr: 1, color: 'action.active' }} />
              }}
            />
          </Grid>
        </Grid>
      </Paper>

      {/* Difficulty Settings */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Typography variant="h6" gutterBottom>
          Difficulty & Timing
        </Typography>
        <Grid container spacing={2}>
          <Grid item xs={12} sm={6}>
            <FormControl fullWidth error={!!getFieldError('difficulty')}>
              <InputLabel>Difficulty Level</InputLabel>
              <Select
                value={config.gameplay.difficulty}
                label="Difficulty Level"
                onChange={(e) => handleGameplayUpdate({ difficulty: e.target.value })}
              >
                {DIFFICULTY_LEVELS.map((difficulty) => (
                  <MenuItem key={difficulty} value={difficulty}>
                    <Box>
                      <Typography variant="body1">
                        {difficulty.charAt(0).toUpperCase() + difficulty.slice(1)}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {getDifficultyDescription(difficulty)}
                      </Typography>
                    </Box>
                  </MenuItem>
                ))}
              </Select>
              <FormHelperText>
                {getFieldError('difficulty')?.message || 'Overall difficulty level'}
              </FormHelperText>
            </FormControl>
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Time Limit (seconds)"
              type="number"
              value={config.gameplay.timeLimit || ''}
              onChange={(e) => {
                const value = parseFloat(e.target.value);
                handleGameplayUpdate({ timeLimit: isNaN(value) ? 0 : value });
              }}
              error={!!getFieldError('timeLimit')}
              helperText={getFieldError('timeLimit')?.message || 'Time limit in seconds (0 = no limit)'}
              inputProps={{ min: 0, max: 3600, step: 1 }}
              InputProps={{
                startAdornment: <TimerIcon sx={{ mr: 1, color: 'action.active' }} />
              }}
            />
          </Grid>
        </Grid>
      </Paper>

      {/* Victory Conditions */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Typography variant="h6" gutterBottom>
          <VictoryIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
          Victory Conditions
        </Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Select the conditions required to win the level
        </Typography>
        
        <Grid container spacing={2}>
          {VICTORY_CONDITIONS.map((condition) => (
            <Grid item xs={12} sm={6} md={4} key={condition}>
              <Card 
                variant="outlined" 
                sx={{ 
                  cursor: 'pointer',
                  border: config.gameplay.victoryConditions?.includes(condition) 
                    ? '2px solid' 
                    : '1px solid',
                  borderColor: config.gameplay.victoryConditions?.includes(condition) 
                    ? 'primary.main' 
                    : 'divider'
                }}
                onClick={() => handleVictoryConditionToggle(condition)}
              >
                <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={config.gameplay.victoryConditions?.includes(condition) || false}
                        onChange={() => handleVictoryConditionToggle(condition)}
                      />
                    }
                    label={
                      <Box>
                        <Typography variant="body2" fontWeight="medium">
                          {condition.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase())}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {getVictoryConditionDescription(condition)}
                        </Typography>
                      </Box>
                    }
                  />
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>

        {getFieldError('victoryConditions') && (
          <FormHelperText error sx={{ mt: 1 }}>
            {getFieldError('victoryConditions')?.message}
          </FormHelperText>
        )}

        <Box sx={{ mt: 2 }}>
          <Typography variant="subtitle2" gutterBottom>
            Selected Victory Conditions:
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            {config.gameplay.victoryConditions?.map((condition) => (
              <Chip
                key={condition}
                label={condition.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase())}
                onDelete={() => handleVictoryConditionToggle(condition)}
                color="primary"
                size="small"
              />
            )) || []}
          </Stack>
        </Box>
      </Paper>

      {/* Custom Mechanics */}
      <Paper elevation={1} sx={{ p: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">
            Custom Mechanics
          </Typography>
          <Button
            startIcon={<AddIcon />}
            onClick={addNewMechanic}
            size="small"
            variant="outlined"
          >
            Add Mechanic
          </Button>
        </Box>

        <Typography variant="body2" color="text.secondary" gutterBottom>
          Define custom gameplay mechanics and their parameters
        </Typography>

        <Grid container spacing={2}>
          {Object.entries(config.gameplay.mechanics).map(([mechanicName, value]) => (
            <Grid item xs={12} sm={6} key={mechanicName}>
              <Card variant="outlined">
                <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                  <Stack spacing={1}>
                    <TextField
                      fullWidth
                      size="small"
                      label="Mechanic Name"
                      value={mechanicName}
                      onChange={(e) => {
                        const newMechanics = { ...config.gameplay.mechanics };
                        delete newMechanics[mechanicName];
                        if (e.target.value) {
                          newMechanics[e.target.value] = value;
                        }
                        handleGameplayUpdate({ mechanics: newMechanics });
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
                          handleMechanicUpdate(mechanicName, parsedValue);
                        }}
                        placeholder="Enter value (string, number, boolean, or JSON)"
                      />
                      <IconButton
                        size="small"
                        onClick={() => handleMechanicUpdate(mechanicName, undefined)}
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

        {Object.keys(config.gameplay.mechanics).length === 0 && (
          <Typography color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
            No custom mechanics defined. Add mechanics to extend gameplay functionality.
          </Typography>
        )}
      </Paper>
    </Stack>
  );
};