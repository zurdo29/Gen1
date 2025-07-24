import React from 'react';
import { Menu, MenuItem, ListItemIcon, ListItemText } from '@mui/material';
import { 
  Terrain, 
  Water, 
  Grass, 
  AccountBalance,
  BeachAccess,
  Whatshot,
  AcUnit
} from '@mui/icons-material';

interface TerrainEditMenuProps {
  open: boolean;
  anchorPosition: { top: number; left: number } | null;
  currentTerrainType: string;
  onClose: () => void;
  onTerrainSelect: (terrainType: string) => void;
}

const TERRAIN_OPTIONS = [
  { type: 'ground', label: 'Ground', icon: Terrain, color: '#8B4513' },
  { type: 'wall', label: 'Wall', icon: AccountBalance, color: '#696969' },
  { type: 'water', label: 'Water', icon: Water, color: '#4169E1' },
  { type: 'grass', label: 'Grass', icon: Grass, color: '#228B22' },
  { type: 'stone', label: 'Stone', icon: AccountBalance, color: '#708090' },
  { type: 'sand', label: 'Sand', icon: BeachAccess, color: '#F4A460' },
  { type: 'lava', label: 'Lava', icon: Whatshot, color: '#FF4500' },
  { type: 'ice', label: 'Ice', icon: AcUnit, color: '#B0E0E6' }
];

export const TerrainEditMenu: React.FC<TerrainEditMenuProps> = ({
  open,
  anchorPosition,
  currentTerrainType,
  onClose,
  onTerrainSelect
}) => {
  const handleTerrainSelect = (terrainType: string) => {
    onTerrainSelect(terrainType);
    onClose();
  };

  return (
    <Menu
      open={open}
      onClose={onClose}
      anchorReference="anchorPosition"
      anchorPosition={anchorPosition || undefined}
      transformOrigin={{
        vertical: 'top',
        horizontal: 'left',
      }}
    >
      {TERRAIN_OPTIONS.map((terrain) => {
        const IconComponent = terrain.icon;
        const isSelected = terrain.type === currentTerrainType;
        
        return (
          <MenuItem
            key={terrain.type}
            onClick={() => handleTerrainSelect(terrain.type)}
            selected={isSelected}
          >
            <ListItemIcon>
              <IconComponent sx={{ color: terrain.color }} />
            </ListItemIcon>
            <ListItemText 
              primary={terrain.label}
              secondary={isSelected ? 'Current' : undefined}
            />
          </MenuItem>
        );
      })}
    </Menu>
  );
};

export default TerrainEditMenu;