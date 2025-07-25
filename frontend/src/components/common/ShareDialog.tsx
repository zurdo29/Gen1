import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Box,
  Typography,
  IconButton,
  Snackbar,
  Alert,
  Tabs,
  Tab,
  CircularProgress,
  Chip,
  Tooltip
} from '@mui/material';
import {
  ContentCopy,
  Share,
  _QrCode,
  Facebook,
  Twitter,
  LinkedIn,
  WhatsApp,
  Close
} from '@mui/icons-material';
import { GenerationConfig, ShareResult } from '../../types';
import { apiService } from '../../services/api';

interface ShareDialogProps {
  open: boolean;
  onClose: () => void;
  config: GenerationConfig;
  levelPreviewUrl?: string;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`share-tabpanel-${index}`}
      aria-labelledby={`share-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ pt: 2 }}>{children}</Box>}
    </div>
  );
}

export const ShareDialog: React.FC<ShareDialogProps> = ({
  open,
  onClose,
  config,
  levelPreviewUrl: _levelPreviewUrl
}) => {
  const [shareResult, setShareResult] = useState<ShareResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [tabValue, setTabValue] = useState(0);
  const [copySuccess, setCopySuccess] = useState(false);
  const [qrCodeUrl, setQrCodeUrl] = useState<string | null>(null);

  // Generate share link when dialog opens
  useEffect(() => {
    if (open && !shareResult) {
      generateShareLink();
    }
  }, [open]);

  // Set QR code when share result is available
  useEffect(() => {
    if (shareResult?.qrCodeDataUrl) {
      setQrCodeUrl(shareResult.qrCodeDataUrl);
    }
  }, [shareResult]);

  const generateShareLink = async () => {
    setLoading(true);
    setError(null);
    
    try {
      const result = await apiService.createShareLink(config);
      setShareResult(result);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to create share link');
    } finally {
      setLoading(false);
    }
  };

  const copyToClipboard = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopySuccess(true);
    } catch (err) {
      console.error('Failed to copy to clipboard:', err);
    }
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const getSocialShareUrl = (platform: string, url: string, text: string) => {
    const encodedUrl = encodeURIComponent(url);
    const encodedText = encodeURIComponent(text);
    
    switch (platform) {
      case 'facebook':
        return `https://www.facebook.com/sharer/sharer.php?u=${encodedUrl}`;
      case 'twitter':
        return `https://twitter.com/intent/tweet?url=${encodedUrl}&text=${encodedText}`;
      case 'linkedin':
        return `https://www.linkedin.com/sharing/share-offsite/?url=${encodedUrl}`;
      case 'whatsapp':
        return `https://wa.me/?text=${encodedText}%20${encodedUrl}`;
      default:
        return url;
    }
  };

  const openSocialShare = (platform: string) => {
    if (!shareResult) return;
    
    const shareText = `Check out this procedurally generated level configuration!`;
    const shareUrl = getSocialShareUrl(platform, shareResult.shareUrl, shareText);
    window.open(shareUrl, '_blank', 'width=600,height=400');
  };

  const getConfigSummary = () => {
    return `${config.width}×${config.height} level using ${config.generationAlgorithm}`;
  };

  return (
    <>
      <Dialog 
        open={open} 
        onClose={onClose} 
        maxWidth="md" 
        fullWidth
        PaperProps={{
          sx: { minHeight: '500px' }
        }}
      >
        <DialogTitle>
          <Box display="flex" alignItems="center" justifyContent="space-between">
            <Typography variant="h6">Share Level Configuration</Typography>
            <IconButton onClick={onClose} size="small">
              <Close />
            </IconButton>
          </Box>
        </DialogTitle>

        <DialogContent>
          {loading && (
            <Box display="flex" justifyContent="center" alignItems="center" py={4}>
              <CircularProgress />
              <Typography variant="body2" sx={{ ml: 2 }}>
                Generating share link...
              </Typography>
            </Box>
          )}

          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          {shareResult && (
            <>
              <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  Configuration: {getConfigSummary()}
                </Typography>
                <Chip 
                  label={`Expires: ${new Date(shareResult.expiresAt).toLocaleDateString()}`}
                  size="small"
                  color="info"
                />
              </Box>

              <Tabs value={tabValue} onChange={handleTabChange} sx={{ mb: 2 }}>
                <Tab label="Share Link" />
                <Tab label="Social Media" />
                <Tab label="QR Code" />
              </Tabs>

              <TabPanel value={tabValue} index={0}>
                <Box>
                  <Typography variant="subtitle2" gutterBottom>
                    Share URL
                  </Typography>
                  <Box display="flex" gap={1} mb={2}>
                    <TextField
                      fullWidth
                      value={shareResult.shareUrl}
                      InputProps={{
                        readOnly: true,
                      }}
                      size="small"
                    />
                    <Tooltip title="Copy to clipboard">
                      <IconButton 
                        onClick={() => copyToClipboard(shareResult.shareUrl)}
                        color="primary"
                      >
                        <ContentCopy />
                      </IconButton>
                    </Tooltip>
                  </Box>
                  
                  <Typography variant="body2" color="text.secondary">
                    Anyone with this link can import your level configuration. 
                    The link will expire on {new Date(shareResult.expiresAt).toLocaleDateString()}.
                  </Typography>
                </Box>
              </TabPanel>

              <TabPanel value={tabValue} index={1}>
                <Box>
                  <Typography variant="subtitle2" gutterBottom>
                    Share on Social Media
                  </Typography>
                  
                  {shareResult?.previewImageUrl && (
                    <Box sx={{ mb: 2, textAlign: 'center' }}>
                      <Typography variant="body2" color="text.secondary" gutterBottom>
                        Preview Image
                      </Typography>
                      <img 
                        src={shareResult.previewImageUrl} 
                        alt="Social media preview"
                        style={{ 
                          maxWidth: '100%', 
                          height: 'auto', 
                          borderRadius: '8px',
                          border: '1px solid #e0e0e0'
                        }}
                      />
                    </Box>
                  )}
                  
                  <Box display="flex" gap={2} flexWrap="wrap">
                    <Button
                      variant="outlined"
                      startIcon={<Facebook />}
                      onClick={() => openSocialShare('facebook')}
                      sx={{ color: '#1877F2', borderColor: '#1877F2' }}
                    >
                      Facebook
                    </Button>
                    <Button
                      variant="outlined"
                      startIcon={<Twitter />}
                      onClick={() => openSocialShare('twitter')}
                      sx={{ color: '#1DA1F2', borderColor: '#1DA1F2' }}
                    >
                      Twitter
                    </Button>
                    <Button
                      variant="outlined"
                      startIcon={<LinkedIn />}
                      onClick={() => openSocialShare('linkedin')}
                      sx={{ color: '#0A66C2', borderColor: '#0A66C2' }}
                    >
                      LinkedIn
                    </Button>
                    <Button
                      variant="outlined"
                      startIcon={<WhatsApp />}
                      onClick={() => openSocialShare('whatsapp')}
                      sx={{ color: '#25D366', borderColor: '#25D366' }}
                    >
                      WhatsApp
                    </Button>
                  </Box>
                  
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
                    Share your level configuration with your network and get feedback from other developers.
                  </Typography>
                </Box>
              </TabPanel>

              <TabPanel value={tabValue} index={2}>
                <Box display="flex" flexDirection="column" alignItems="center">
                  <Typography variant="subtitle2" gutterBottom>
                    QR Code for Mobile Sharing
                  </Typography>
                  
                  {qrCodeUrl ? (
                    <Box textAlign="center">
                      <img 
                        src={qrCodeUrl} 
                        alt="QR Code for share link"
                        style={{ maxWidth: '200px', height: 'auto' }}
                      />
                      <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                        Scan with your mobile device to open the configuration
                      </Typography>
                    </Box>
                  ) : (
                    <CircularProgress />
                  )}
                </Box>
              </TabPanel>
            </>
          )}
        </DialogContent>

        <DialogActions>
          <Button onClick={onClose}>Close</Button>
          {shareResult && (
            <Button 
              variant="contained" 
              startIcon={<Share />}
              onClick={() => {
                if (navigator.share) {
                  navigator.share({
                    title: 'Level Configuration',
                    text: `Check out this procedurally generated level configuration: ${getConfigSummary()}`,
                    url: shareResult.shareUrl
                  });
                } else {
                  copyToClipboard(shareResult.shareUrl);
                }
              }}
            >
              Share
            </Button>
          )}
        </DialogActions>
      </Dialog>

      <Snackbar
        open={copySuccess}
        autoHideDuration={3000}
        onClose={() => setCopySuccess(false)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="success" onClose={() => setCopySuccess(false)}>
          Link copied to clipboard!
        </Alert>
      </Snackbar>
    </>
  );
};

export default ShareDialog;