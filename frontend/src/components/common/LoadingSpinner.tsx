import React from 'react';
import {
  Box,
  CircularProgress,
  Typography,
  LinearProgress,
} from '@mui/material';

interface LoadingSpinnerProps {
  message?: string;
  progress?: number;
  variant?: 'circular' | 'linear';
  size?: 'small' | 'medium' | 'large';
}

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  message = 'Loading...',
  progress,
  variant = 'circular',
  size = 'medium',
}) => {
  const getSize = () => {
    switch (size) {
      case 'small': return 24;
      case 'large': return 60;
      default: return 40;
    }
  };

  if (variant === 'linear') {
    return (
      <Box sx={{ width: '100%', p: 2 }}>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          {message}
        </Typography>
        <LinearProgress 
          variant={progress !== undefined ? 'determinate' : 'indeterminate'}
          value={progress}
        />
        {progress !== undefined && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            {Math.round(progress)}%
          </Typography>
        )}
      </Box>
    );
  }

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        p: 3,
        gap: 2,
      }}
    >
      <CircularProgress 
        size={getSize()}
        variant={progress !== undefined ? 'determinate' : 'indeterminate'}
        value={progress}
      />
      <Typography variant="body2" color="text.secondary">
        {message}
      </Typography>
      {progress !== undefined && (
        <Typography variant="body2" color="text.secondary">
          {Math.round(progress)}%
        </Typography>
      )}
    </Box>
  );
};