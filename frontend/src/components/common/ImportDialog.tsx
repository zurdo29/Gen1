import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Box,
  Typography,
  Alert,
  CircularProgress,
  IconButton
} from '@mui/material';
import { Close, Download } from '@mui/icons-material';
import { GenerationConfig } from '../../types';
import { useSharedConfig } from '../../hooks/useSharedConfig';

interface ImportDialogProps {
  open: boolean;
  onClose: () => void;
  onImport: (config: GenerationConfig) => void;
}

export const ImportDialog: React.FC<ImportDialogProps> = ({
  open,
  onClose,
  onImport
}) => {
  const [shareUrl, setShareUrl] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { importFromUrl } = useSharedConfig();

  const extractShareId = (url: string): string | null => {
    try {
      // Handle full URLs
      if (url.includes('share=')) {
        const urlObj = new URL(url);
        return urlObj.searchParams.get('share');
      }
      
      // Handle direct share IDs
      if (url.match(/^[a-zA-Z0-9-_]+$/)) {
        return url;
      }
      
      // Handle share URLs with path
      const shareMatch = url.match(/\/share\/([a-zA-Z0-9-_]+)/);
      if (shareMatch) {
        return shareMatch[1];
      }
      
      return null;
    } catch {
      return null;
    }
  };

  const handleImport = async () => {
    if (!shareUrl.trim()) {
      setError('Please enter a share URL or ID');
      return;
    }

    const shareId = extractShareId(shareUrl.trim());
    if (!shareId) {
      setError('Invalid share URL or ID format');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const config = await importFromUrl(shareId);
      if (config) {
        onImport(config);
        handleClose();
      } else {
        setError('Failed to import configuration');
      }
    } catch (err: any) {
      setError(err.message || 'Failed to import configuration');
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setShareUrl('');
    setError(null);
    setLoading(false);
    onClose();
  };

  const handleUrlChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setShareUrl(event.target.value);
    if (error) {
      setError(null);
    }
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Typography variant="h6">Import Shared Configuration</Typography>
          <IconButton onClick={handleClose} size="small">
            <Close />
          </IconButton>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Box sx={{ pt: 1 }}>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Enter a share URL or share ID to import a level configuration.
          </Typography>

          <TextField
            fullWidth
            label="Share URL or ID"
            value={shareUrl}
            onChange={handleUrlChange}
            placeholder="https://example.com?share=abc123 or abc123"
            margin="normal"
            disabled={loading}
            helperText="Paste the full share URL or just the share ID"
          />

          {error && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {error}
            </Alert>
          )}

          <Box sx={{ mt: 2 }}>
            <Typography variant="subtitle2" gutterBottom>
              Supported formats:
            </Typography>
            <Typography variant="body2" color="text.secondary" component="div">
              • Full URL: <code>https://example.com?share=abc123</code>
              <br />
              • Share path: <code>https://example.com/share/abc123</code>
              <br />
              • Share ID only: <code>abc123</code>
            </Typography>
          </Box>
        </Box>
      </DialogContent>

      <DialogActions>
        <Button onClick={handleClose} disabled={loading}>
          Cancel
        </Button>
        <Button
          onClick={handleImport}
          variant="contained"
          disabled={loading || !shareUrl.trim()}
          startIcon={loading ? <CircularProgress size={16} /> : <Download />}
        >
          {loading ? 'Importing...' : 'Import'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ImportDialog;