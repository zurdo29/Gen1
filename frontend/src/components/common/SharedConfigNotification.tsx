import React from 'react';
import {
  Alert,
  AlertTitle,
  Button,
  Box,
  Typography,
  Chip
} from '@mui/material';
import { Download, Close } from '@mui/icons-material';
import { GenerationConfig } from '../../types';

interface SharedConfigNotificationProps {
  config: GenerationConfig;
  onAccept: () => void;
  onDismiss: () => void;
}

export const SharedConfigNotification: React.FC<SharedConfigNotificationProps> = ({
  config,
  onAccept,
  onDismiss
}) => {
  const getConfigSummary = () => {
    return `${config.width}×${config.height} level using ${config.generationAlgorithm}`;
  };

  const getConfigDetails = () => {
    const details = [];
    
    if (config.entities?.length > 0) {
      details.push(`${config.entities.length} entity types`);
    }
    
    if (config.terrainTypes?.length > 0) {
      details.push(`${config.terrainTypes.length} terrain types`);
    }
    
    if (config.visualTheme?.themeName) {
      details.push(`${config.visualTheme.themeName} theme`);
    }
    
    return details;
  };

  return (
    <Alert
      severity="info"
      sx={{ mb: 2 }}
      action={
        <Box display="flex" gap={1}>
          <Button
            color="inherit"
            size="small"
            onClick={onAccept}
            startIcon={<Download />}
          >
            Load Configuration
          </Button>
          <Button
            color="inherit"
            size="small"
            onClick={onDismiss}
            startIcon={<Close />}
          >
            Dismiss
          </Button>
        </Box>
      }
    >
      <AlertTitle>Shared Configuration Available</AlertTitle>
      <Typography variant="body2" gutterBottom>
        <strong>{getConfigSummary()}</strong>
      </Typography>
      
      <Box display="flex" gap={1} flexWrap="wrap" sx={{ mt: 1 }}>
        {getConfigDetails().map((detail, index) => (
          <Chip
            key={index}
            label={detail}
            size="small"
            variant="outlined"
          />
        ))}
      </Box>
      
      <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
        This will replace your current configuration. Make sure to save any changes first.
      </Typography>
    </Alert>
  );
};

export default SharedConfigNotification;