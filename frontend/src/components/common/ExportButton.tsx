import React, { useState } from 'react';
import { Button, Menu, MenuItem, ListItemIcon, ListItemText, Divider } from '@mui/material';
import { Download, GetApp, Archive, Code, TableChart } from '@mui/icons-material';
import { Level } from '../../types';
import ExportManager from './ExportManager';

interface ExportButtonProps {
  level: Level | null;
  levels?: Level[];
  variant?: 'contained' | 'outlined' | 'text';
  size?: 'small' | 'medium' | 'large';
  disabled?: boolean;
}

export const ExportButton: React.FC<ExportButtonProps> = ({
  level,
  levels,
  variant = 'outlined',
  size = 'medium',
  disabled = false
}) => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [exportManagerOpen, setExportManagerOpen] = useState(false);
  const [quickExportFormat, setQuickExportFormat] = useState<string>('');

  const handleClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const handleQuickExport = (format: string) => {
    setQuickExportFormat(format);
    setExportManagerOpen(true);
    handleClose();
  };

  const handleAdvancedExport = () => {
    setQuickExportFormat('');
    setExportManagerOpen(true);
    handleClose();
  };

  const isBatchMode = Boolean(levels && levels.length > 1);

  return (
    <>
      <Button
        variant={variant}
        size={size}
        disabled={disabled || (!level && !isBatchMode)}
        onClick={handleClick}
        startIcon={<Download />}
      >
        {isBatchMode ? `Export ${levels?.length} Levels` : 'Export'}
      </Button>

      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleClose}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: 'left',
        }}
        transformOrigin={{
          vertical: 'top',
          horizontal: 'left',
        }}
      >
        <MenuItem onClick={() => handleQuickExport('json')}>
          <ListItemIcon>
            <Code />
          </ListItemIcon>
          <ListItemText 
            primary="JSON" 
            secondary="Web-friendly format"
          />
        </MenuItem>
        
        <MenuItem onClick={() => handleQuickExport('unity')}>
          <ListItemIcon>
            <GetApp />
          </ListItemIcon>
          <ListItemText 
            primary="Unity" 
            secondary="Unity-compatible format"
          />
        </MenuItem>
        
        <MenuItem onClick={() => handleQuickExport('csv')}>
          <ListItemIcon>
            <TableChart />
          </ListItemIcon>
          <ListItemText 
            primary="CSV" 
            secondary="Spreadsheet format"
          />
        </MenuItem>
        
        {isBatchMode && (
          <MenuItem onClick={() => handleQuickExport('zip')}>
            <ListItemIcon>
              <Archive />
            </ListItemIcon>
            <ListItemText 
              primary="ZIP Archive" 
              secondary="All formats in one file"
            />
          </MenuItem>
        )}
        
        <Divider />
        
        <MenuItem onClick={handleAdvancedExport}>
          <ListItemIcon>
            <Download />
          </ListItemIcon>
          <ListItemText 
            primary="Advanced Export..." 
            secondary="More options and formats"
          />
        </MenuItem>
      </Menu>

      <ExportManager
        open={exportManagerOpen}
        onClose={() => setExportManagerOpen(false)}
        level={level}
        levels={levels}
      />
    </>
  );
};

export default ExportButton;