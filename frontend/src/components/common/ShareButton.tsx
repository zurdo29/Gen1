import React, { useState } from 'react';
import { Button, IconButton, Tooltip } from '@mui/material';
import { Share } from '@mui/icons-material';
import { GenerationConfig } from '../../types';
import ShareDialog from './ShareDialog';

interface ShareButtonProps {
  config: GenerationConfig;
  variant?: 'button' | 'icon';
  size?: 'small' | 'medium' | 'large';
  disabled?: boolean;
  levelPreviewUrl?: string;
}

export const ShareButton: React.FC<ShareButtonProps> = ({
  config,
  variant = 'button',
  size = 'medium',
  disabled = false,
  levelPreviewUrl
}) => {
  const [dialogOpen, setDialogOpen] = useState(false);

  const handleClick = () => {
    setDialogOpen(true);
  };

  const handleClose = () => {
    setDialogOpen(false);
  };

  if (variant === 'icon') {
    return (
      <>
        <Tooltip title="Share configuration">
          <IconButton
            onClick={handleClick}
            disabled={disabled}
            size={size}
            color="primary"
          >
            <Share />
          </IconButton>
        </Tooltip>
        
        <ShareDialog
          open={dialogOpen}
          onClose={handleClose}
          config={config}
          levelPreviewUrl={levelPreviewUrl}
        />
      </>
    );
  }

  return (
    <>
      <Button
        onClick={handleClick}
        disabled={disabled}
        size={size}
        startIcon={<Share />}
        variant="outlined"
      >
        Share
      </Button>
      
      <ShareDialog
        open={dialogOpen}
        onClose={handleClose}
        config={config}
        levelPreviewUrl={levelPreviewUrl}
      />
    </>
  );
};

export default ShareButton;