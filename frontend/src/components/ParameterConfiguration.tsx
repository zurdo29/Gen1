import React from 'react';
import { Box, Typography } from '@mui/material';

// Placeholder component for ParameterConfiguration
// This component is under development
export const ParameterConfiguration: React.FC = () => {
  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h6" gutterBottom>
        Parameter Configuration
      </Typography>
      <Typography variant="body2" color="text.secondary">
        This component is under development. Please use the configuration panels in the editor for now.
      </Typography>
    </Box>
  );
};

export default ParameterConfiguration;