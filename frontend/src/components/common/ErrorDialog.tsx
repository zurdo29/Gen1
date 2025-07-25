import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Box,
  Alert,
  AlertTitle,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  _Link,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  ContentCopy as CopyIcon,
  Launch as _LaunchIcon,
  BugReport as BugReportIcon,
} from '@mui/icons-material';
interface UserFriendlyError {
  title: string;
  message: string;
  details?: string;
  code?: string;
}

interface ErrorDialogProps {
  open: boolean;
  error: UserFriendlyError | null;
  onClose: () => void;
  onRetry?: () => void;
}

export const ErrorDialog: React.FC<ErrorDialogProps> = ({
  open,
  error,
  onClose,
  onRetry,
}) => {
  const [detailsExpanded, setDetailsExpanded] = React.useState(false);

  if (!error) return null;

  const handleCopyError = () => {
    const errorText = `
Error: ${error.title}
Message: ${error.message}
${error.details ? `Details: ${error.details}` : ''}
${error.code ? `Code: ${error.code}` : ''}
Timestamp: ${new Date().toISOString()}
URL: ${window.location.href}
User Agent: ${navigator.userAgent}
    `.trim();

    navigator.clipboard.writeText(errorText).then(() => {
      // Could show a toast notification here
      console.log('Error details copied to clipboard');
    });
  };



  const getSeverityColor = (code?: string) => {
    if (!code) return 'error';
    
    if (code.includes('VALIDATION') || code.includes('CONFIGURATION')) {
      return 'warning';
    }
    if (code.includes('NETWORK') || code.includes('TIMEOUT')) {
      return 'info';
    }
    return 'error';
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="md"
      fullWidth
      PaperProps={{
        sx: { minHeight: '300px' }
      }}
    >
      <DialogTitle sx={{ pb: 1 }}>
        <Box display="flex" alignItems="center" gap={1}>
          <BugReportIcon color="error" />
          {error.title}
          {error.code && (
            <Chip
              label={error.code}
              size="small"
              color={getSeverityColor(error.code) as any}
              variant="outlined"
            />
          )}
        </Box>
      </DialogTitle>

      <DialogContent>
        <Alert severity={getSeverityColor(error.code) as any} sx={{ mb: 2 }}>
          <AlertTitle>What happened?</AlertTitle>
          {error.message}
        </Alert>

        {error.details && (
          <Box sx={{ mb: 2 }}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Additional Information:
            </Typography>
            <Typography variant="body2" sx={{ 
              backgroundColor: 'grey.50', 
              p: 2, 
              borderRadius: 1,
              fontFamily: 'monospace',
              fontSize: '0.875rem'
            }}>
              {error.details}
            </Typography>
          </Box>
        )}



        <Accordion 
          expanded={detailsExpanded} 
          onChange={(_, expanded) => setDetailsExpanded(expanded)}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="subtitle2">
              Technical Details
            </Typography>
          </AccordionSummary>
          <AccordionDetails>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">
                  Error Code: {error.code || 'N/A'}
                </Typography>
                <Tooltip title="Copy error details">
                  <IconButton size="small" onClick={handleCopyError}>
                    <CopyIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              </Box>
              
              <Typography variant="body2" color="text.secondary">
                Timestamp: {new Date().toLocaleString()}
              </Typography>
              
              <Typography variant="body2" color="text.secondary">
                URL: {window.location.href}
              </Typography>
              

            </Box>
          </AccordionDetails>
        </Accordion>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} color="inherit">
          Close
        </Button>
        
        {onRetry && (
          <Button onClick={onRetry} variant="contained" color="primary">
            Try Again
          </Button>
        )}
        

      </DialogActions>
    </Dialog>
  );
};