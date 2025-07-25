import React from 'react';
import { 
  Box, 
  Paper, 
  ToggleButtonGroup, 
  ToggleButton, 
  Tooltip, 
  Divider,
  Typography
} from '@mui/material';
import { 
  Person,
  SmartToy,
  Star,
  PowerSettingsNew,
  Groups,
  ExitToApp,
  Flag,
  Block,
  TouchApp,
  PanTool,
  Add,
  Delete
} from '@mui/icons-material';

export type EditMode = 'select' | 'add' | 'delete';
export type EntityType = 'Player' | 'Enemy' | 'Item' | 'PowerUp' | 'NPC' | 'Exit' | 'Checkpoint' | 'Obstacle' | 'Trigger';

interface EntityEditToolbarProps {
  editMode: EditMode;
  selectedEntityType: EntityType;
  onEditModeChange: (mode: EditMode) => void;
  onEntityTypeChange: (entityType: EntityType) => void;
  disabled?: boolean;
}

const ENTITY_OPTIONS: { type: EntityType; label: string; icon: React.ComponentType; color: string }[] = [
  { type: 'Player', label: 'Player', icon: Person, color: '#00FF00' },
  { type: 'Enemy', label: 'Enemy', icon: SmartToy, color: '#FF0000' },
  { type: 'Item', label: 'Item', icon: Star, color: '#FFD700' },
  { type: 'PowerUp', label: 'Power Up', icon: PowerSettingsNew, color: '#FF69B4' },
  { type: 'NPC', label: 'NPC', icon: Groups, color: '#9370DB' },
  { type: 'Exit', label: 'Exit', icon: ExitToApp, color: '#00CED1' },
  { type: 'Checkpoint', label: 'Checkpoint', icon: Flag, color: '#32CD32' },
  { type: 'Obstacle', label: 'Obstacle', icon: Block, color: '#8B4513' },
  { type: 'Trigger', label: 'Trigger', icon: TouchApp, color: '#FF6347' }
];

export const EntityEditToolbar: React.FC<EntityEditToolbarProps> = ({
  editMode,
  selectedEntityType,
  onEditModeChange,
  onEntityTypeChange,
  disabled = false
}) => {
  return (
    <Paper sx={{ p: 2, mb: 2 }}>
      <Typography variant="subtitle2" gutterBottom>
        Entity Editing
      </Typography>
      
      {/* Edit Mode Selection */}
      <Box sx={{ mb: 2 }}>
        <Typography variant="body2" sx={{ mb: 1 }}>
          Mode:
        </Typography>
        <ToggleButtonGroup
          value={editMode}
          exclusive
          onChange={(_, value) => value && onEditModeChange(value)}
          size="small"
          disabled={disabled}
        >
          <ToggleButton value="select">
            <Tooltip title="Select/Move Entities">
              <PanTool />
            </Tooltip>
          </ToggleButton>
          <ToggleButton value="add">
            <Tooltip title="Add Entities">
              <Add />
            </Tooltip>
          </ToggleButton>
          <ToggleButton value="delete">
            <Tooltip title="Delete Entities">
              <Delete />
            </Tooltip>
          </ToggleButton>
        </ToggleButtonGroup>
      </Box>

      <Divider sx={{ my: 2 }} />

      {/* Entity Type Selection */}
      <Box>
        <Typography variant="body2" sx={{ mb: 1 }}>
          Entity Type:
        </Typography>
        <ToggleButtonGroup
          value={selectedEntityType}
          exclusive
          onChange={(_, value) => value && onEntityTypeChange(value)}
          size="small"
          disabled={disabled || editMode === 'delete'}
          sx={{ flexWrap: 'wrap' }}
        >
          {ENTITY_OPTIONS.map((entity) => {
            const IconComponent = entity.icon;
            return (
              <ToggleButton key={entity.type} value={entity.type}>
                <Tooltip title={entity.label}>
                  <Box sx={{ color: entity.color }}>
                    <IconComponent />
                  </Box>
                </Tooltip>
              </ToggleButton>
            );
          })}
        </ToggleButtonGroup>
      </Box>
    </Paper>
  );
};

export default EntityEditToolbar;