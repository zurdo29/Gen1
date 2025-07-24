import React, { useState, useCallback, useMemo } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  IconButton,
  Tooltip,
  Chip,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Stack,
  Alert,
  CircularProgress,
  ImageList,
  ImageListItem,
  ImageListItemBar,
  Checkbox,
  FormControlLabel,
  Menu,
  MenuItem,
  Divider
} from '@mui/material';
import {
  Visibility as ViewIcon,
  Download as DownloadIcon,
  Compare as CompareIcon,
  MoreVert as MoreIcon,
  CheckBox as CheckBoxIcon,
  CheckBoxOutlineBlank as CheckBoxOutlineBlankIcon,
  SelectAll as SelectAllIcon,
  Clear as ClearIcon
} from '@mui/icons-material';
import { Level, BatchGenerationRequest } from '../../types';
import { ExportButton } from '../common/ExportButton';

interface BatchResult {
  id: string;
  level: Level;
  thumbnail?: string;
  variationIndex: number;
  batchIndex: number;
  generatedAt: Date;
  generationTime: number;
}

interface BatchResultsGridProps {
  results: BatchResult[];
  isLoading: boolean;
  onSelectResult: (result: BatchResult) => void;
  onCompareResults: (results: BatchResult[]) => void;
  onExportResults: (results: BatchResult[]) => void;
  onRegenerateResult: (result: BatchResult) => void;
  selectedResults: string[];
  onSelectionChange: (selectedIds: string[]) => void;
  batchRequest?: BatchGenerationRequest;
  error?: string;
}

export const BatchResultsGrid: React.FC<BatchResultsGridProps> = ({
  results,
  isLoading,
  onSelectResult,
  onCompareResults,
  onExportResults,
  onRegenerateResult,
  selectedResults,
  onSelectionChange,
  batchRequest,
  error
}) => {
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
  const [sortBy, setSortBy] = useState<'generated' | 'variation' | 'batch'>('generated');
  const [filterBy, setFilterBy] = useState<string>('all');
  const [previewDialog, setPreviewDialog] = useState<BatchResult | null>(null);
  const [contextMenu, setContextMenu] = useState<{
    mouseX: number;
    mouseY: number;
    result: BatchResult;
  } | null>(null);

  // Sort and filter results
  const processedResults = useMemo(() => {
    let filtered = results;

    // Apply filters
    if (filterBy !== 'all') {
      filtered = results.filter(result => {
        // Add filtering logic based on variations, batch index, etc.
        return true; // Placeholder
      });
    }

    // Apply sorting
    return filtered.sort((a, b) => {
      switch (sortBy) {
        case 'generated':
          return b.generatedAt.getTime() - a.generatedAt.getTime();
        case 'variation':
          return a.variationIndex - b.variationIndex;
        case 'batch':
          return a.batchIndex - b.batchIndex;
        default:
          return 0;
      }
    });
  }, [results, filterBy, sortBy]);

  const handleSelectAll = useCallback(() => {
    if (selectedResults.length === results.length) {
      onSelectionChange([]);
    } else {
      onSelectionChange(results.map(r => r.id));
    }
  }, [results, selectedResults, onSelectionChange]);

  const handleResultClick = useCallback((result: BatchResult) => {
    onSelectResult(result);
  }, [onSelectResult]);

  const handleResultSelect = useCallback((resultId: string, selected: boolean) => {
    if (selected) {
      onSelectionChange([...selectedResults, resultId]);
    } else {
      onSelectionChange(selectedResults.filter(id => id !== resultId));
    }
  }, [selectedResults, onSelectionChange]);

  const handleContextMenu = useCallback((event: React.MouseEvent, result: BatchResult) => {
    event.preventDefault();
    setContextMenu({
      mouseX: event.clientX - 2,
      mouseY: event.clientY - 4,
      result
    });
  }, []);

  const handleContextMenuClose = useCallback(() => {
    setContextMenu(null);
  }, []);

  const handlePreviewOpen = useCallback((result: BatchResult) => {
    setPreviewDialog(result);
    handleContextMenuClose();
  }, [handleContextMenuClose]);

  const handlePreviewClose = useCallback(() => {
    setPreviewDialog(null);
  }, []);

  const handleCompare = useCallback(() => {
    const selectedResultObjects = results.filter(r => selectedResults.includes(r.id));
    onCompareResults(selectedResultObjects);
  }, [results, selectedResults, onCompareResults]);

  const handleExport = useCallback(() => {
    const selectedResultObjects = results.filter(r => selectedResults.includes(r.id));
    onExportResults(selectedResultObjects);
  }, [results, selectedResults, onExportResults]);

  const generateThumbnail = useCallback((level: Level): string => {
    // Generate a simple thumbnail representation
    // This would typically be done on the backend or with a proper canvas rendering
    const canvas = document.createElement('canvas');
    canvas.width = 100;
    canvas.height = 100;
    const ctx = canvas.getContext('2d');
    
    if (ctx && level.terrain) {
      const tileWidth = canvas.width / level.terrain.width;
      const tileHeight = canvas.height / level.terrain.height;
      
      for (let y = 0; y < level.terrain.height; y++) {
        for (let x = 0; x < level.terrain.width; x++) {
          const tile = level.terrain.tiles[y]?.[x];
          if (tile) {
            // Simple color mapping for different terrain types
            switch (tile.type) {
              case 'wall':
                ctx.fillStyle = '#333333';
                break;
              case 'floor':
                ctx.fillStyle = '#f0f0f0';
                break;
              case 'water':
                ctx.fillStyle = '#4a90e2';
                break;
              default:
                ctx.fillStyle = '#cccccc';
            }
            ctx.fillRect(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
          }
        }
      }
      
      // Draw entities as small dots
      ctx.fillStyle = '#ff4444';
      level.entities.forEach(entity => {
        const entityX = (entity.position.x / level.terrain.width) * canvas.width;
        const entityY = (entity.position.y / level.terrain.height) * canvas.height;
        ctx.beginPath();
        ctx.arc(entityX, entityY, 2, 0, 2 * Math.PI);
        ctx.fill();
      });
    }
    
    return canvas.toDataURL();
  }, []);

  if (error) {
    return (
      <Alert severity="error" sx={{ mt: 2 }}>
        <Typography variant="h6">Batch Generation Error</Typography>
        <Typography variant="body2">{error}</Typography>
      </Alert>
    );
  }

  if (isLoading && results.length === 0) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 200 }}>
        <Stack spacing={2} alignItems="center">
          <CircularProgress />
          <Typography variant="body2" color="text.secondary">
            Generating batch results...
          </Typography>
        </Stack>
      </Box>
    );
  }

  return (
    <Card>
      <CardContent>
        {/* Header with controls */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">
            Batch Results ({results.length})
          </Typography>
          
          <Stack direction="row" spacing={1}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={selectedResults.length === results.length && results.length > 0}
                  indeterminate={selectedResults.length > 0 && selectedResults.length < results.length}
                  onChange={handleSelectAll}
                  icon={<CheckBoxOutlineBlankIcon />}
                  checkedIcon={<CheckBoxIcon />}
                />
              }
              label="Select All"
            />
            
            <Button
              variant="outlined"
              startIcon={<CompareIcon />}
              onClick={handleCompare}
              disabled={selectedResults.length < 2}
              size="small"
            >
              Compare ({selectedResults.length})
            </Button>
            
            <ExportButton
              levels={results.filter(r => selectedResults.includes(r.id)).map(r => r.level)}
              variant="outlined"
              size="small"
              disabled={selectedResults.length === 0}
            />
          </Stack>
        </Box>

        {/* Batch info */}
        {batchRequest && (
          <Alert severity="info" sx={{ mb: 2 }}>
            <Typography variant="body2">
              <strong>Batch Configuration:</strong> {batchRequest.count} levels per variation, 
              {batchRequest.variations.length} variation{batchRequest.variations.length !== 1 ? 's' : ''}
              {batchRequest.variations.length > 0 && (
                <>
                  <br />
                  <strong>Variations:</strong> {batchRequest.variations.map(v => v.parameter).join(', ')}
                </>
              )}
            </Typography>
          </Alert>
        )}

        {/* Results grid */}
        {results.length === 0 ? (
          <Alert severity="info">
            No batch results yet. Start a batch generation to see results here.
          </Alert>
        ) : (
          <ImageList cols={4} gap={8}>
            {processedResults.map((result) => (
              <ImageListItem key={result.id}>
                <Box
                  sx={{
                    position: 'relative',
                    cursor: 'pointer',
                    border: selectedResults.includes(result.id) ? 2 : 1,
                    borderColor: selectedResults.includes(result.id) ? 'primary.main' : 'divider',
                    borderRadius: 1,
                    overflow: 'hidden',
                    '&:hover': {
                      borderColor: 'primary.main'
                    }
                  }}
                  onClick={() => handleResultClick(result)}
                  onContextMenu={(e) => handleContextMenu(e, result)}
                >
                  {/* Thumbnail */}
                  <img
                    src={result.thumbnail || generateThumbnail(result.level)}
                    alt={`Level ${result.id}`}
                    style={{
                      width: '100%',
                      height: 120,
                      objectFit: 'cover',
                      display: 'block'
                    }}
                  />
                  
                  {/* Selection checkbox */}
                  <Checkbox
                    checked={selectedResults.includes(result.id)}
                    onChange={(e) => {
                      e.stopPropagation();
                      handleResultSelect(result.id, e.target.checked);
                    }}
                    sx={{
                      position: 'absolute',
                      top: 4,
                      left: 4,
                      backgroundColor: 'rgba(255, 255, 255, 0.8)',
                      '&:hover': {
                        backgroundColor: 'rgba(255, 255, 255, 0.9)'
                      }
                    }}
                  />
                  
                  {/* Action buttons */}
                  <Box
                    sx={{
                      position: 'absolute',
                      top: 4,
                      right: 4,
                      display: 'flex',
                      gap: 0.5
                    }}
                  >
                    <IconButton
                      size="small"
                      onClick={(e) => {
                        e.stopPropagation();
                        handlePreviewOpen(result);
                      }}
                      sx={{
                        backgroundColor: 'rgba(255, 255, 255, 0.8)',
                        '&:hover': {
                          backgroundColor: 'rgba(255, 255, 255, 0.9)'
                        }
                      }}
                    >
                      <ViewIcon fontSize="small" />
                    </IconButton>
                  </Box>
                  
                  {/* Info bar */}
                  <ImageListItemBar
                    title={`Level ${result.batchIndex + 1}`}
                    subtitle={
                      <Stack direction="row" spacing={1} alignItems="center">
                        <Chip
                          label={`Var ${result.variationIndex + 1}`}
                          size="small"
                          variant="outlined"
                          sx={{ height: 20, fontSize: '0.7rem' }}
                        />
                        <Typography variant="caption">
                          {result.generationTime}ms
                        </Typography>
                      </Stack>
                    }
                    position="below"
                  />
                </Box>
              </ImageListItem>
            ))}
          </ImageList>
        )}

        {/* Loading indicator for additional results */}
        {isLoading && results.length > 0 && (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
            <Stack direction="row" spacing={2} alignItems="center">
              <CircularProgress size={20} />
              <Typography variant="body2" color="text.secondary">
                Generating more results...
              </Typography>
            </Stack>
          </Box>
        )}

        {/* Context menu */}
        <Menu
          open={contextMenu !== null}
          onClose={handleContextMenuClose}
          anchorReference="anchorPosition"
          anchorPosition={
            contextMenu !== null
              ? { top: contextMenu.mouseY, left: contextMenu.mouseX }
              : undefined
          }
        >
          <MenuItem onClick={() => contextMenu && handlePreviewOpen(contextMenu.result)}>
            <ViewIcon sx={{ mr: 1 }} />
            Preview
          </MenuItem>
          <MenuItem onClick={() => contextMenu && onSelectResult(contextMenu.result)}>
            <SelectAllIcon sx={{ mr: 1 }} />
            Load in Editor
          </MenuItem>
          <Divider />
          <MenuItem onClick={() => contextMenu && onRegenerateResult(contextMenu.result)}>
            <ClearIcon sx={{ mr: 1 }} />
            Regenerate
          </MenuItem>
        </Menu>

        {/* Preview dialog */}
        <Dialog
          open={previewDialog !== null}
          onClose={handlePreviewClose}
          maxWidth="md"
          fullWidth
        >
          <DialogTitle>
            Level Preview - {previewDialog?.id}
          </DialogTitle>
          <DialogContent>
            {previewDialog && (
              <Box>
                <img
                  src={previewDialog.thumbnail || generateThumbnail(previewDialog.level)}
                  alt="Level preview"
                  style={{
                    width: '100%',
                    maxHeight: 400,
                    objectFit: 'contain',
                    border: '1px solid #ddd',
                    borderRadius: 4
                  }}
                />
                <Box sx={{ mt: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    <strong>Size:</strong> {previewDialog.level.terrain.width} × {previewDialog.level.terrain.height}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    <strong>Entities:</strong> {previewDialog.level.entities.length}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    <strong>Generation Time:</strong> {previewDialog.generationTime}ms
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    <strong>Generated:</strong> {previewDialog.generatedAt.toLocaleString()}
                  </Typography>
                </Box>
              </Box>
            )}
          </DialogContent>
          <DialogActions>
            <Button onClick={handlePreviewClose}>Close</Button>
            <Button
              variant="contained"
              onClick={() => {
                if (previewDialog) {
                  onSelectResult(previewDialog);
                  handlePreviewClose();
                }
              }}
            >
              Load in Editor
            </Button>
          </DialogActions>
        </Dialog>
      </CardContent>
    </Card>
  );
};