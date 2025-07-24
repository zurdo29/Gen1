import React, { useCallback, useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
  Grid,
  IconButton,
  Collapse,
  Stack,
  Chip,
  FormHelperText,
  Alert
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  ContentCopy as CopyIcon
} from '@mui/icons-material';
import { GenerationConfig, EntityConfig, ValidationError, EntityType } from '../../types';
import { ENTITY_TYPES, PLACEMENT_STRATEGIES } from '../../schemas';

interface EntityConfigPanelProps {
  config: GenerationConfig;
  onChange: (updates: Partial<GenerationConfig>) => void;
  validationErrors: ValidationError[];
}

interface EntityCardProps {
  entity: EntityConfig;
  index: number;
  onUpdate: (index: number, updates: Partial<EntityConfig>) => void;
  onDelete: (index: number) => void;
  onDuplicate: (index: number) => void;
  validationErrors: ValidationError[];
}

const EntityCard: React.FC<EntityCardProps> = ({
  entity,
  index,
  onUpdate,
  onDelete,
  onDuplicate,
  validationErrors
}) => {
  const [expanded, setExpanded] = useState(false);

  const getFieldError = useCallback((fieldName: string) => {
    return validationErrors.find(error => 
      error.field === `entities.${index}.${fieldName}` || 
      error.field.startsWith(`entities.${index}.${fieldName}.`)
    );
  }, [validationErrors, index]);

  const handleUpdate = useCallback((field: keyof EntityConfig, value: any) => {
    onUpdate(index, { [field]: value });
  }, [index, onUpdate]);

  const handlePropertyUpdate = useCallback((propertyName: string, value: any) => {
    const newProperties = { ...entity.properties };
    if (value === '' || value === null || value === undefined) {
      delete newProperties[propertyName];
    } else {
      newProperties[propertyName] = value;
    }
    onUpdate(index, { properties: newProperties });
  }, [entity.properties, index, onUpdate]);

  const getEntityTypeIcon = (type: EntityType): string => {
    const icons: Record<EntityType, string> = {
      Player: '👤',
      Enemy: '👹',
      Item: '💎',
      PowerUp: '⚡',
      NPC: '🧙',
      Exit: '🚪',
      Checkpoint: '🏁',
      Obstacle: '🧱',
      Trigger: '⚙️'
    };
    return icons[type] || '❓';
  };

  return (
    <Card elevation={2} sx={{ mb: 2 }}>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="h6">
              {getEntityTypeIcon(entity.type)} {entity.type}
            </Typography>
            <Chip 
              label={`Count: ${entity.count}`} 
              size="small" 
              color={entity.count > 0 ? 'primary' : 'default'}
            />
          </Box>
          <Box>
            <IconButton onClick={() => onDuplicate(index)} size="small" title="Duplicate">
              <CopyIcon />
            </IconButton>
            <IconButton onClick={() => setExpanded(!expanded)} size="small">
              {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
            </IconButton>
            <IconButton onClick={() => onDelete(index)} size="small" color="error" title="Delete">
              <DeleteIcon />
            </IconButton>
          </Box>
        </Box>

        <Grid container spacing={2}>
          <Grid item xs={12} sm={6}>
            <FormControl fullWidth error={!!getFieldError('type')}>
              <InputLabel>Entity Type</InputLabel>
              <Select
                value={entity.type}
                label="Entity Type"
                onChange={(e) => handleUpdate('type', e.target.value as EntityType)}
              >
                {ENTITY_TYPES.map((type) => (
                  <MenuItem key={type} value={type}>
                    {getEntityTypeIcon(type as EntityType)} {type}
                  </MenuItem>
                ))}
              </Select>
              <FormHelperText>
                {getFieldError('type')?.message || 'Type of entity to place'}
              </FormHelperText>
            </FormControl>
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Count"
              type="number"
              value={entity.count}
              onChange={(e) => handleUpdate('count', parseInt(e.target.value) || 0)}
              error={!!getFieldError('count')}
              helperText={getFieldError('count')?.message || 'Number of entities to place'}
              inputProps={{ min: 0, max: 1000 }}
            />
          </Grid>
        </Grid>

        <Collapse in={expanded}>
          <Box sx={{ mt: 2 }}>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Min Distance"
                  type="number"
                  value={entity.minDistance}
                  onChange={(e) => handleUpdate('minDistance', parseFloat(e.target.value) || 0)}
                  error={!!getFieldError('minDistance')}
                  helperText={getFieldError('minDistance')?.message || 'Minimum distance from other entities'}
                  inputProps={{ min: 0, max: 100, step: 0.1 }}
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Max Distance from Player"
                  type="number"
                  value={entity.maxDistanceFromPlayer === Number.MAX_VALUE ? '' : entity.maxDistanceFromPlayer}
                  onChange={(e) => {
                    const value = parseFloat(e.target.value);
                    handleUpdate('maxDistanceFromPlayer', isNaN(value) ? Number.MAX_VALUE : value);
                  }}
                  error={!!getFieldError('maxDistanceFromPlayer')}
                  helperText={getFieldError('maxDistanceFromPlayer')?.message || 'Maximum distance from player spawn (empty = unlimited)'}
                  inputProps={{ min: 0, step: 0.1 }}
                />
              </Grid>

              <Grid item xs={12}>
                <FormControl fullWidth error={!!getFieldError('placementStrategy')}>
                  <InputLabel>Placement Strategy</InputLabel>
                  <Select
                    value={entity.placementStrategy}
                    label="Placement Strategy"
                    onChange={(e) => handleUpdate('placementStrategy', e.target.value)}
                  >
                    {PLACEMENT_STRATEGIES.map((strategy) => (
                      <MenuItem key={strategy} value={strategy}>
                        {strategy.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase())}
                      </MenuItem>
                    ))}
                  </Select>
                  <FormHelperText>
                    {getFieldError('placementStrategy')?.message || 'Strategy for placing entities'}
                  </FormHelperText>
                </FormControl>
              </Grid>

              {/* Entity Properties */}
              <Grid item xs={12}>
                <Typography variant="subtitle2" gutterBottom>
                  Entity Properties
                </Typography>
                <Stack spacing={1}>
                  {Object.entries(entity.properties).map(([key, value]) => (
                    <Grid container spacing={1} key={key}>
                      <Grid item xs={5}>
                        <TextField
                          fullWidth
                          size="small"
                          label="Property Name"
                          value={key}
                          onChange={(e) => {
                            const newProperties = { ...entity.properties };
                            delete newProperties[key];
                            if (e.target.value) {
                              newProperties[e.target.value] = value;
                            }
                            onUpdate(index, { properties: newProperties });
                          }}
                        />
                      </Grid>
                      <Grid item xs={5}>
                        <TextField
                          fullWidth
                          size="small"
                          label="Value"
                          value={typeof value === 'object' ? JSON.stringify(value) : value}
                          onChange={(e) => {
                            let parsedValue: any = e.target.value;
                            try {
                              // Try to parse as JSON for objects/arrays
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
                            handlePropertyUpdate(key, parsedValue);
                          }}
                        />
                      </Grid>
                      <Grid item xs={2}>
                        <IconButton
                          size="small"
                          onClick={() => handlePropertyUpdate(key, undefined)}
                          color="error"
                        >
                          <DeleteIcon />
                        </IconButton>
                      </Grid>
                    </Grid>
                  ))}
                  <Button
                    startIcon={<AddIcon />}
                    onClick={() => handlePropertyUpdate(`property_${Date.now()}`, '')}
                    size="small"
                    variant="outlined"
                  >
                    Add Property
                  </Button>
                </Stack>
              </Grid>
            </Grid>
          </Box>
        </Collapse>
      </CardContent>
    </Card>
  );
};

export const EntityConfigPanel: React.FC<EntityConfigPanelProps> = ({
  config,
  onChange,
  validationErrors
}) => {
  const handleAddEntity = useCallback(() => {
    const newEntity: EntityConfig = {
      type: 'Item',
      count: 1,
      minDistance: 1.0,
      maxDistanceFromPlayer: Number.MAX_VALUE,
      properties: {},
      placementStrategy: 'random'
    };

    onChange({
      entities: [...config.entities, newEntity]
    });
  }, [config.entities, onChange]);

  const handleUpdateEntity = useCallback((index: number, updates: Partial<EntityConfig>) => {
    const newEntities = [...config.entities];
    newEntities[index] = { ...newEntities[index], ...updates };
    onChange({ entities: newEntities });
  }, [config.entities, onChange]);

  const handleDeleteEntity = useCallback((index: number) => {
    const newEntities = config.entities.filter((_, i) => i !== index);
    onChange({ entities: newEntities });
  }, [config.entities, onChange]);

  const handleDuplicateEntity = useCallback((index: number) => {
    const entityToDuplicate = config.entities[index];
    const duplicatedEntity = {
      ...entityToDuplicate,
      properties: { ...entityToDuplicate.properties }
    };
    
    const newEntities = [...config.entities];
    newEntities.splice(index + 1, 0, duplicatedEntity);
    onChange({ entities: newEntities });
  }, [config.entities, onChange]);

  const getEntitySummary = () => {
    const summary = config.entities.reduce((acc, entity) => {
      acc[entity.type] = (acc[entity.type] || 0) + entity.count;
      return acc;
    }, {} as Record<string, number>);

    return Object.entries(summary).map(([type, count]) => (
      <Chip key={type} label={`${type}: ${count}`} size="small" sx={{ mr: 1, mb: 1 }} />
    ));
  };

  return (
    <Stack spacing={3}>
      <Box>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">
            Entity Configuration ({config.entities.length} types)
          </Typography>
          <Button
            startIcon={<AddIcon />}
            onClick={handleAddEntity}
            variant="contained"
            size="small"
          >
            Add Entity
          </Button>
        </Box>

        {config.entities.length > 0 && (
          <Box sx={{ mb: 2 }}>
            <Typography variant="subtitle2" gutterBottom>
              Entity Summary:
            </Typography>
            {getEntitySummary()}
          </Box>
        )}

        {config.entities.length === 0 && (
          <Alert severity="info" sx={{ mb: 2 }}>
            No entities configured. Add entities to populate your level with interactive elements.
          </Alert>
        )}
      </Box>

      {config.entities.map((entity, index) => (
        <EntityCard
          key={index}
          entity={entity}
          index={index}
          onUpdate={handleUpdateEntity}
          onDelete={handleDeleteEntity}
          onDuplicate={handleDuplicateEntity}
          validationErrors={validationErrors}
        />
      ))}
    </Stack>
  );
};