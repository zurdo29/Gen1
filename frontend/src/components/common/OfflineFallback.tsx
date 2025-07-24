// Fallback UI components for offline and network failure scenarios
import React from 'react';
import {
  Box,
  Typography,
  Button,
  Alert,
  AlertTitle,
  Card,
  CardContent,
  CardActions,
  IconButton,
  Snackbar,
  LinearProgress,
  Chip,
  Stack,
  Divider
} from '@mui/material';
import {
  WifiOff,
  Refresh,
  CloudOff,
  Warning,
  Info,
  CheckCircle,
  Error as ErrorIcon,
  Download,
  Cached,
  Storage
} from '@mui/icons-material';
import { useOfflineStatus, useNetworkQuality } from '../../services/serviceWorker';
import { configStorage, levelStorage, userStorage } from '../../services/localStorage';

interface OfflineIndicatorProps {
  position?: 'top' | 'bottom';
  showDetails?: boolean;
}

export const OfflineIndicator: React.FC<OfflineIndicatorProps> = ({
  position = 'top',
  showDetails = false
}) => {
  const { isOnline, isOffline, wasOffline } = useOfflineStatus();
  const { networkQuality, connectionType } = useNetworkQuality();
  const [showReconnected, setShowReconnected] = React.useState(false);

  React.useEffect(() => {
    if (isOnline && wasOffline) {
      setShowReconnected(true);
      const timer = setTimeout(() => setShowReconnected(false), 3000);
      return () => clearTimeout(timer);
    }
  }, [isOnline, wasOffline]);

  if (isOnline && !showReconnected && !showDetails) {
    return null;
  }

  return (
    <>
      {/* Offline Banner */}
      {isOffline && (
        <Alert
          severity="warning"
          icon={<WifiOff />}
          sx={{
            position: 'fixed',
            top: position === 'top' ? 0 : 'auto',
            bottom: position === 'bottom' ? 0 : 'auto',
            left: 0,
            right: 0,
            zIndex: 9999,
            borderRadius: 0
          }}
        >
          <AlertTitle>You're offline</AlertTitle>
          Some features may be limited. We'll sync your changes when you're back online.
        </Alert>
      )}

      {/* Reconnected Notification */}
      <Snackbar
        open={showReconnected}
        autoHideDuration={3000}
        onClose={() => setShowReconnected(false)}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <Alert severity="success" icon={<CheckCircle />}>
          Back online! Syncing your changes...
        </Alert>
      </Snackbar>

      {/* Network Quality Indicator */}
      {showDetails && isOnline && (
        <Box sx={{ position: 'fixed', top: 16, right: 16, zIndex: 1000 }}>
          <Chip
            icon={networkQuality === 'fast' ? <CheckCircle /> : <Warning />}
            label={`${connectionType.toUpperCase()} - ${networkQuality}`}
            color={networkQuality === 'fast' ? 'success' : 'warning'}
            size="small"
          />
        </Box>
      )}
    </>
  );
};

interface OfflineFallbackProps {
  error?: Error;
  onRetry?: () => void;
  showCachedData?: boolean;
  cachedDataCount?: number;
  children?: React.ReactNode;
}

export const OfflineFallback: React.FC<OfflineFallbackProps> = ({
  error,
  onRetry,
  showCachedData = false,
  cachedDataCount = 0,
  children
}) => {
  const { isOffline } = useOfflineStatus();
  const [storageStats, setStorageStats] = React.useState({
    configs: 0,
    levels: 0,
    user: 0
  });

  React.useEffect(() => {
    const updateStats = () => {
      setStorageStats({
        configs: configStorage.getStats().entryCount,
        levels: levelStorage.getStats().entryCount,
        user: userStorage.getStats().entryCount
      });
    };

    updateStats();
    const interval = setInterval(updateStats, 5000);
    return () => clearInterval(interval);
  }, []);

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '400px',
        p: 3,
        textAlign: 'center'
      }}
    >
      <Card sx={{ maxWidth: 500, width: '100%' }}>
        <CardContent>
          <Box sx={{ mb: 2 }}>
            {isOffline ? (
              <WifiOff sx={{ fontSize: 64, color: 'warning.main', mb: 2 }} />
            ) : (
              <CloudOff sx={{ fontSize: 64, color: 'error.main', mb: 2 }} />
            )}
          </Box>

          <Typography variant="h5" gutterBottom>
            {isOffline ? 'You\'re offline' : 'Connection failed'}
          </Typography>

          <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
            {isOffline
              ? 'Don\'t worry! You can still work with your saved configurations and levels.'
              : error?.message || 'Unable to connect to the server. Please check your connection and try again.'}
          </Typography>

          {showCachedData && (
            <Box sx={{ mb: 3 }}>
              <Typography variant="h6" gutterBottom>
                Available offline data:
              </Typography>
              
              <Stack spacing={1}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Storage fontSize="small" />
                  <Typography variant="body2">
                    {storageStats.configs} saved configurations
                  </Typography>
                </Box>
                
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Download fontSize="small" />
                  <Typography variant="body2">
                    {storageStats.levels} cached levels
                  </Typography>
                </Box>
                
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Cached fontSize="small" />
                  <Typography variant="body2">
                    {storageStats.user} user preferences
                  </Typography>
                </Box>
              </Stack>
            </Box>
          )}

          {children}
        </CardContent>

        <CardActions sx={{ justifyContent: 'center', pb: 2 }}>
          {onRetry && (
            <Button
              variant="contained"
              startIcon={<Refresh />}
              onClick={onRetry}
              disabled={isOffline}
            >
              Try again
            </Button>
          )}
        </CardActions>
      </Card>
    </Box>
  );
};

interface NetworkErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  isNetworkError: boolean;
}

interface NetworkErrorBoundaryProps {
  children: React.ReactNode;
  fallback?: React.ComponentType<{ error: Error; retry: () => void }>;
  onError?: (error: Error) => void;
}

export class NetworkErrorBoundary extends React.Component<
  NetworkErrorBoundaryProps,
  NetworkErrorBoundaryState
> {
  constructor(props: NetworkErrorBoundaryProps) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      isNetworkError: false
    };
  }

  static getDerivedStateFromError(error: Error): NetworkErrorBoundaryState {
    const isNetworkError = 
      error.message.includes('fetch') ||
      error.message.includes('network') ||
      error.message.includes('offline') ||
      error.name === 'TypeError';

    return {
      hasError: true,
      error,
      isNetworkError
    };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Network Error Boundary caught an error:', error, errorInfo);
    this.props.onError?.(error);
  }

  retry = () => {
    this.setState({
      hasError: false,
      error: null,
      isNetworkError: false
    });
  };

  render() {
    if (this.state.hasError) {
      const { fallback: Fallback } = this.props;
      
      if (Fallback && this.state.error) {
        return <Fallback error={this.state.error} retry={this.retry} />;
      }

      if (this.state.isNetworkError) {
        return (
          <OfflineFallback
            error={this.state.error || undefined}
            onRetry={this.retry}
            showCachedData
          />
        );
      }

      // Generic error fallback
      return (
        <Alert severity="error" sx={{ m: 2 }}>
          <AlertTitle>Something went wrong</AlertTitle>
          {this.state.error?.message || 'An unexpected error occurred'}
          <Button onClick={this.retry} sx={{ mt: 1 }}>
            Try again
          </Button>
        </Alert>
      );
    }

    return this.props.children;
  }
}

interface LoadingWithOfflineFallbackProps {
  loading: boolean;
  error?: Error | null;
  data?: any;
  onRetry?: () => void;
  children: React.ReactNode;
  loadingComponent?: React.ReactNode;
  emptyComponent?: React.ReactNode;
}

export const LoadingWithOfflineFallback: React.FC<LoadingWithOfflineFallbackProps> = ({
  loading,
  error,
  data,
  onRetry,
  children,
  loadingComponent,
  emptyComponent
}) => {
  const { isOffline } = useOfflineStatus();

  if (loading) {
    return (
      <Box sx={{ p: 2 }}>
        {loadingComponent || (
          <>
            <LinearProgress sx={{ mb: 2 }} />
            <Typography variant="body2" color="text.secondary" align="center">
              {isOffline ? 'Loading from cache...' : 'Loading...'}
            </Typography>
          </>
        )}
      </Box>
    );
  }

  if (error) {
    return (
      <OfflineFallback
        error={error}
        onRetry={onRetry}
        showCachedData={isOffline}
      />
    );
  }

  if (!data || (Array.isArray(data) && data.length === 0)) {
    return (
      <Box sx={{ p: 2, textAlign: 'center' }}>
        {emptyComponent || (
          <>
            <Info sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
            <Typography variant="h6" color="text.secondary">
              No data available
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {isOffline 
                ? 'Try going online to load fresh data'
                : 'There\'s nothing here yet'
              }
            </Typography>
          </>
        )}
      </Box>
    );
  }

  return <>{children}</>;
};

interface OfflineCapableButtonProps {
  onClick: () => Promise<void> | void;
  onlineOnly?: boolean;
  offlineMessage?: string;
  children: React.ReactNode;
  [key: string]: any;
}

export const OfflineCapableButton: React.FC<OfflineCapableButtonProps> = ({
  onClick,
  onlineOnly = false,
  offlineMessage = 'This feature requires an internet connection',
  children,
  ...props
}) => {
  const { isOffline } = useOfflineStatus();
  const [showOfflineMessage, setShowOfflineMessage] = React.useState(false);

  const handleClick = () => {
    if (onlineOnly && isOffline) {
      setShowOfflineMessage(true);
      setTimeout(() => setShowOfflineMessage(false), 3000);
      return;
    }
    
    onClick();
  };

  return (
    <>
      <Button
        {...props}
        onClick={handleClick}
        disabled={props.disabled || (onlineOnly && isOffline)}
      >
        {children}
      </Button>
      
      <Snackbar
        open={showOfflineMessage}
        autoHideDuration={3000}
        onClose={() => setShowOfflineMessage(false)}
      >
        <Alert severity="warning">
          {offlineMessage}
        </Alert>
      </Snackbar>
    </>
  );
};

export default OfflineFallback;