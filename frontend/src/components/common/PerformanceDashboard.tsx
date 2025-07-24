import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  LinearProgress,
  Chip,
  Tabs,
  Tab,
  Alert,
  IconButton,
  Tooltip
} from '@mui/material';
import {
  Speed as SpeedIcon,
  Memory as MemoryIcon,
  Timeline as TimelineIcon,
  Storage as StorageIcon,
  Refresh as RefreshIcon,
  Download as DownloadIcon,
  Clear as ClearIcon
} from '@mui/icons-material';
import { usePerformanceMonitoring } from '../../services/performance';
import { useCacheStats, levelCache, configCache, apiCache } from '../../services/cache';
import { useQueryClient } from '@tanstack/react-query';

interface PerformanceDashboardProps {
  open: boolean;
  onClose: () => void;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel({ children, value, index, ...other }: TabPanelProps) {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`performance-tabpanel-${index}`}
      aria-labelledby={`performance-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

export const PerformanceDashboard: React.FC<PerformanceDashboardProps> = ({
  open,
  onClose
}) => {
  const [activeTab, setActiveTab] = useState(0);
  const queryClient = useQueryClient();
  
  const {
    isMonitoring,
    stats: performanceStats,
    startMonitoring,
    stopMonitoring,
    getRecommendations
  } = usePerformanceMonitoring();

  const levelCacheStats = useCacheStats(levelCache);
  const configCacheStats = useCacheStats(configCache);
  const apiCacheStats = useCacheStats(apiCache);

  const [recommendations, setRecommendations] = useState<string[]>([]);

  useEffect(() => {
    if (open && !isMonitoring) {
      startMonitoring();
    }
  }, [open, isMonitoring, startMonitoring]);

  useEffect(() => {
    if (isMonitoring) {
      const interval = setInterval(() => {
        setRecommendations(getRecommendations());
      }, 5000);
      
      return () => clearInterval(interval);
    }
  }, [isMonitoring, getRecommendations]);

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleExportData = () => {
    const data = {
      timestamp: new Date().toISOString(),
      performance: performanceStats,
      cache: {
        level: levelCacheStats,
        config: configCacheStats,
        api: apiCacheStats
      },
      reactQuery: {
        queries: queryClient.getQueryCache().getAll().length,
        mutations: queryClient.getMutationCache().getAll().length
      },
      recommendations
    };

    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `performance-report-${Date.now()}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const handleClearCaches = () => {
    levelCache.clear();
    configCache.clear();
    apiCache.clear();
    queryClient.clear();
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const formatMs = (ms: number) => {
    return `${ms.toFixed(2)}ms`;
  };

  const getPerformanceColor = (value: number, threshold: number, reverse = false) => {
    const ratio = value / threshold;
    if (reverse) {
      return ratio > 1 ? 'success' : ratio > 0.7 ? 'warning' : 'error';
    }
    return ratio < 0.7 ? 'success' : ratio < 1 ? 'warning' : 'error';
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="lg" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <SpeedIcon />
          <Typography variant="h6">Performance Dashboard</Typography>
          <Box sx={{ ml: 'auto', display: 'flex', gap: 1 }}>
            <Tooltip title="Export Performance Data">
              <IconButton onClick={handleExportData} size="small">
                <DownloadIcon />
              </IconButton>
            </Tooltip>
            <Tooltip title="Clear All Caches">
              <IconButton onClick={handleClearCaches} size="small">
                <ClearIcon />
              </IconButton>
            </Tooltip>
            <Tooltip title={isMonitoring ? 'Stop Monitoring' : 'Start Monitoring'}>
              <IconButton 
                onClick={isMonitoring ? stopMonitoring : startMonitoring} 
                size="small"
                color={isMonitoring ? 'success' : 'default'}
              >
                <RefreshIcon />
              </IconButton>
            </Tooltip>
          </Box>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={activeTab} onChange={handleTabChange}>
            <Tab label="Overview" />
            <Tab label="Caching" />
            <Tab label="Recommendations" />
          </Tabs>
        </Box>

        <TabPanel value={activeTab} index={0}>
          <Grid container spacing={3}>
            {/* Performance Overview Cards */}
            <Grid item xs={12} md={3}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <TimelineIcon color="primary" />
                    <Typography variant="h6">FPS</Typography>
                  </Box>
                  <Typography variant="h4">
                    {performanceStats?.render?.average?.toFixed(0) || 'N/A'}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Frames per second
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={3}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <SpeedIcon color="primary" />
                    <Typography variant="h6">Render Time</Typography>
                  </Box>
                  <Typography variant="h4">
                    {formatMs(performanceStats?.render?.average || 0)}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Average render time
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={3}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <MemoryIcon color="primary" />
                    <Typography variant="h6">Memory</Typography>
                  </Box>
                  <Typography variant="h4">
                    {formatBytes((performanceStats?.memory?.average || 0) * 1024 * 1024)}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    JS Heap usage
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={3}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <StorageIcon color="primary" />
                    <Typography variant="h6">Cache Hit Rate</Typography>
                  </Box>
                  <Typography variant="h4">
                    {levelCacheStats.hitRate.toFixed(1)}%
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Level cache efficiency
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            {/* Status Indicators */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>System Status</Typography>
                  <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                    <Chip 
                      label={`Monitoring: ${isMonitoring ? 'Active' : 'Inactive'}`}
                      color={isMonitoring ? 'success' : 'default'}
                      variant="outlined"
                    />
                    <Chip 
                      label={`React Query: ${queryClient.getQueryCache().getAll().length} queries`}
                      color="info"
                      variant="outlined"
                    />
                    <Chip 
                      label={`Level Cache: ${levelCacheStats.entryCount} entries`}
                      color="primary"
                      variant="outlined"
                    />
                    <Chip 
                      label={`Memory: ${formatBytes((performanceStats?.memory?.average || 0) * 1024 * 1024)}`}
                      color="secondary"
                      variant="outlined"
                    />
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={activeTab} index={1}>
          <Grid container spacing={3}>
            {[
              { name: 'Level Cache', stats: levelCacheStats },
              { name: 'Config Cache', stats: configCacheStats },
              { name: 'API Cache', stats: apiCacheStats }
            ].map(({ name, stats }) => (
              <Grid item xs={12} md={4} key={name}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom>{name}</Typography>
                    <Box sx={{ mb: 2 }}>
                      <Typography variant="body2">Hit Rate: {stats.hitRate.toFixed(1)}%</Typography>
                      <LinearProgress 
                        variant="determinate" 
                        value={stats.hitRate}
                        sx={{ mt: 1 }}
                      />
                    </Box>
                    <Typography variant="body2">Entries: {stats.entryCount}</Typography>
                    <Typography variant="body2">Size: {formatBytes(stats.totalSize)}</Typography>
                    <Typography variant="body2">Hits: {stats.hits}</Typography>
                    <Typography variant="body2">Misses: {stats.misses}</Typography>
                    <Typography variant="body2">Evictions: {stats.evictions}</Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        </TabPanel>

        <TabPanel value={activeTab} index={2}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>Performance Recommendations</Typography>
                  {recommendations.length > 0 ? (
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                      {recommendations.map((recommendation, index) => (
                        <Alert key={index} severity="info">
                          {recommendation}
                        </Alert>
                      ))}
                    </Box>
                  ) : (
                    <Alert severity="success">
                      No performance issues detected. Your application is running optimally!
                    </Alert>
                  )}
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </TabPanel>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
};