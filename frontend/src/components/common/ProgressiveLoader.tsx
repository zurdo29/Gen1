// Progressive loader component that integrates all offline and loading features
import React from 'react';
import {
  Box,
  Typography,
  LinearProgress,
  Button,
  Card,
  CardContent,
  CardActions,
  Chip,
  Stack,
  IconButton,
  Collapse,
  Alert
} from '@mui/material';
import {
  Pause,
  PlayArrow,
  Stop,
  ExpandMore,
  ExpandLess,
  CloudDownload,
  Storage,
  Speed
} from '@mui/icons-material';
import { useProgressiveLoading } from '../../services/progressiveLoading';
import { useOfflineData } from '../../services/localStorage';
import { OfflineIndicator, LoadingWithOfflineFallback } from './OfflineFallback';

interface ProgressiveLoaderProps {
  id: string;
  dataUrl: string | string[];
  onDataLoaded?: (data: any) => void;
  onError?: (error: Error) => void;
  title?: string;
  description?: string;
  showDetails?: boolean;
  autoStart?: boolean;
  cacheKey?: string;
  cacheTTL?: number;
}

export const ProgressiveLoader: React.FC<ProgressiveLoaderProps> = ({
  id,
  dataUrl,
  onDataLoaded,
  onError,
  title = 'Loading Data',
  description,
  showDetails = false,
  autoStart = true,
  cacheKey,
  cacheTTL
}) => {
  const [showDetailsExpanded, setShowDetailsExpanded] = React.useState(false);
  const [startTime, setStartTime] = React.useState<number | null>(null);
  const [estimatedTimeRemaining, setEstimatedTimeRemaining] = React.useState<number | null>(null);

  // Progressive loading hook
  const {
    data: progressiveData,
    progress,
    isLoading: isProgressiveLoading,
    error: progressiveError,
    startLoading,
    pauseLoading,
    resumeLoading,
    cancelLoading
  } = useProgressiveLoading(id, dataUrl);

  // Offline data hook for caching
  const {
    data: cachedData,
    loading: isCacheLoading,
    error: cacheError,
    isOffline,
    refresh: refreshCache
  } = useOfflineData(
    cacheKey || id,
    async () => {
      // This will be called when we need to fetch fresh data
      if (!progressiveData) {
        await startLoading();
        return progressiveData;
      }
      return progressiveData;
    },
    { ttl: cacheTTL }
  );

  // Calculate estimated time remaining
  React.useEffect(() => {
    if (isProgressiveLoading && progress.percentage > 0 && startTime) {
      const elapsed = Date.now() - startTime;
      const rate = progress.percentage / elapsed;
      const remaining = (100 - progress.percentage) / rate;
      setEstimatedTimeRemaining(remaining);
    }
  }, [progress.percentage, isProgressiveLoading, startTime]);

  // Start loading automatically if requested
  React.useEffect(() => {
    if (autoStart && !isProgressiveLoading && !progressiveData && !cachedData) {
      setStartTime(Date.now());
      startLoading();
    }
  }, [autoStart, isProgressiveLoading, progressiveData, cachedData, startLoading]);

  // Handle data loaded
  React.useEffect(() => {
    const finalData = progressiveData || cachedData;
    if (finalData && onDataLoaded) {
      onDataLoaded(finalData);
    }
  }, [progressiveData, cachedData, onDataLoaded]);

  // Handle errors
  React.useEffect(() => {
    const error = progressiveError || cacheError;
    if (error && onError) {
      onError(error);
    }
  }, [progressiveError, cacheError, onError]);

  const handleStart = () => {
    setStartTime(Date.now());
    startLoading();
  };

  const handlePause = () => {
    pauseLoading();
  };

  const handleResume = () => {
    resumeLoading();
  };

  const handleCancel = () => {
    cancelLoading();
    setStartTime(null);
    setEstimatedTimeRemaining(null);
  };

  const isLoading = isProgressiveLoading || isCacheLoading;
  const hasData = !!(progressiveData || cachedData);
  const hasError = !!(progressiveError || cacheError);
  const finalData = progressiveData || cachedData;

  const formatTime = (ms: number) => {
    const seconds = Math.ceil(ms / 1000);
    if (seconds < 60) return `${seconds}s`;
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}m ${remainingSeconds}s`;
  };

  const getDataSource = () => {
    if (progressiveData) return 'network';
    if (cachedData) return 'cache';
    return 'none';
  };

  return (
    <Box>
      <OfflineIndicator />
      
      <Card>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="h6">{title}</Typography>
            
            <Stack direction="row" spacing={1}>
              {hasData && (
                <Chip
                  icon={getDataSource() === 'network' ? <CloudDownload /> : <Storage />}
                  label={getDataSource() === 'network' ? 'Live' : 'Cached'}
                  color={getDataSource() === 'network' ? 'success' : 'default'}
                  size="small"
                />
              )}
              
              {isOffline && (
                <Chip
                  label="Offline"
                  color="warning"
                  size="small"
                />
              )}
            </Stack>
          </Box>

          {description && (
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              {description}
            </Typography>
          )}

          <LoadingWithOfflineFallback
            loading={isLoading}
            error={hasError ? (progressiveError || cacheError) : null}
            data={finalData}
            onRetry={isOffline ? refreshCache : handleStart}
          >
            {/* Loading Progress */}
            {isLoading && (
              <Box sx={{ mb: 2 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
                  <Typography variant="body2">
                    {progress.currentChunk ? `Loading ${progress.currentChunk}...` : 'Preparing...'}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {progress.percentage.toFixed(1)}%
                  </Typography>
                </Box>
                
                <LinearProgress 
                  variant="determinate" 
                  value={progress.percentage} 
                  sx={{ mb: 1 }}
                />
                
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <Typography variant="caption" color="text.secondary">
                    {progress.loaded} / {progress.total} chunks
                  </Typography>
                  
                  {estimatedTimeRemaining && (
                    <Typography variant="caption" color="text.secondary">
                      ~{formatTime(estimatedTimeRemaining)} remaining
                    </Typography>
                  )}
                </Box>
              </Box>
            )}

            {/* Success State */}
            {hasData && !isLoading && (
              <Alert severity="success" sx={{ mb: 2 }}>
                Data loaded successfully from {getDataSource()}
                {Array.isArray(finalData) && ` (${finalData.length} items)`}
              </Alert>
            )}

            {/* Details Section */}
            {showDetails && (
              <Box>
                <Button
                  startIcon={showDetailsExpanded ? <ExpandLess /> : <ExpandMore />}
                  onClick={() => setShowDetailsExpanded(!showDetailsExpanded)}
                  size="small"
                >
                  Details
                </Button>
                
                <Collapse in={showDetailsExpanded}>
                  <Box sx={{ mt: 2, p: 2, bgcolor: 'grey.50', borderRadius: 1 }}>
                    <Stack spacing={1}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Typography variant="caption">Data Source:</Typography>
                        <Typography variant="caption">{Array.isArray(dataUrl) ? 'Batch' : 'Single'}</Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Typography variant="caption">URLs:</Typography>
                        <Typography variant="caption">
                          {Array.isArray(dataUrl) ? dataUrl.length : 1}
                        </Typography>
                      </Box>
                      
                      {progress.total > 0 && (
                        <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                          <Typography variant="caption">Chunks:</Typography>
                          <Typography variant="caption">{progress.total}</Typography>
                        </Box>
                      )}
                      
                      {startTime && (
                        <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                          <Typography variant="caption">Elapsed:</Typography>
                          <Typography variant="caption">
                            {formatTime(Date.now() - startTime)}
                          </Typography>
                        </Box>
                      )}
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Typography variant="caption">Connection:</Typography>
                        <Typography variant="caption">
                          {isOffline ? 'Offline' : 'Online'}
                        </Typography>
                      </Box>
                    </Stack>
                  </Box>
                </Collapse>
              </Box>
            )}
          </LoadingWithOfflineFallback>
        </CardContent>

        {/* Action Buttons */}
        <CardActions>
          {!isLoading && !hasData && (
            <Button
              variant="contained"
              startIcon={<PlayArrow />}
              onClick={handleStart}
              disabled={isOffline && !cachedData}
            >
              Start Loading
            </Button>
          )}

          {isLoading && (
            <>
              <IconButton onClick={handlePause} title="Pause">
                <Pause />
              </IconButton>
              
              <IconButton onClick={handleResume} title="Resume">
                <PlayArrow />
              </IconButton>
              
              <IconButton onClick={handleCancel} title="Cancel">
                <Stop />
              </IconButton>
            </>
          )}

          {hasData && !isLoading && (
            <Button
              startIcon={<Speed />}
              onClick={isOffline ? refreshCache : handleStart}
              disabled={isOffline && getDataSource() === 'cache'}
            >
              {isOffline ? 'Refresh from Cache' : 'Reload'}
            </Button>
          )}
        </CardActions>
      </Card>
    </Box>
  );
};

// Specialized components for different data types
interface LevelProgressiveLoaderProps extends Omit<ProgressiveLoaderProps, 'dataUrl'> {
  levelId: string;
  onLevelLoaded?: (level: any) => void;
}

export const LevelProgressiveLoader: React.FC<LevelProgressiveLoaderProps> = ({
  levelId,
  onLevelLoaded,
  ...props
}) => {
  return (
    <ProgressiveLoader
      {...props}
      id={`level-${levelId}`}
      dataUrl={`/api/levels/${levelId}`}
      title={`Loading Level ${levelId}`}
      description="Loading level data with terrain, entities, and configuration"
      cacheKey={`level-${levelId}`}
      cacheTTL={1000 * 60 * 30} // 30 minutes
      onDataLoaded={onLevelLoaded}
    />
  );
};

interface BatchProgressiveLoaderProps extends Omit<ProgressiveLoaderProps, 'dataUrl'> {
  levelIds: string[];
  onBatchLoaded?: (levels: any[]) => void;
}

export const BatchProgressiveLoader: React.FC<BatchProgressiveLoaderProps> = ({
  levelIds,
  onBatchLoaded,
  ...props
}) => {
  const urls = levelIds.map(id => `/api/levels/${id}`);
  
  return (
    <ProgressiveLoader
      {...props}
      id={`batch-${levelIds.join('-')}`}
      dataUrl={urls}
      title={`Loading ${levelIds.length} Levels`}
      description="Loading multiple levels in parallel with progressive chunks"
      cacheKey={`batch-${levelIds.join('-')}`}
      cacheTTL={1000 * 60 * 15} // 15 minutes
      onDataLoaded={onBatchLoaded}
      showDetails
    />
  );
};

export default ProgressiveLoader;