import React, { useRef, useEffect, useState, useCallback } from 'react';
import {
  Paper,
  Typography,
  Box,
  CircularProgress,
  IconButton,
  Tooltip,
  ButtonGroup,
  Button,
  Chip,
  Menu,
  MenuItem,
} from '@mui/material';
import {
  ZoomIn as ZoomInIcon,
  ZoomOut as ZoomOutIcon,
  CenterFocusStrong as CenterIcon,
  GridOn as GridIcon,
  GridOff as GridOffIcon,
  Palette as PaletteIcon,
  Info as InfoIcon,
} from '@mui/icons-material';

interface SimpleLevel {
  width: number;
  height: number;
  tiles: string[][];
  entities: Array<{
    type: string;
    x: number;
    y: number;
  }>;
}

interface InteractiveLevelPreviewProps {
  level: SimpleLevel | null;
  isLoading?: boolean;
  onTileClick?: (x: number, y: number, tileType: string) => void;
  onEntityClick?: (entity: any) => void;
}

const tileColors: Record<string, string> = {
  wall: '#666666',
  floor: '#e0e0e0',
  water: '#4fc3f7',
  grass: '#81c784',
  stone: '#9e9e9e',
  sand: '#ffcc80',
  lava: '#ff5722',
  empty: '#f5f5f5',
};

const entityColors: Record<string, string> = {
  player: '#2196f3',
  enemy: '#f44336',
  item: '#ff9800',
  exit: '#4caf50',
  checkpoint: '#9c27b0',
};

export const InteractiveLevelPreview: React.FC<InteractiveLevelPreviewProps> = ({
  level,
  isLoading = false,
  onTileClick,
  onEntityClick
}) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  
  const [zoom, setZoom] = useState(1);
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const [showGrid, setShowGrid] = useState(true);
  const [selectedTile, setSelectedTile] = useState<{ x: number; y: number } | null>(null);
  const [hoveredEntity, setHoveredEntity] = useState<any>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [lastMousePos, setLastMousePos] = useState({ x: 0, y: 0 });
  const [paletteAnchor, setPaletteAnchor] = useState<null | HTMLElement>(null);

  const baseTileSize = 20;
  const tileSize = baseTileSize * zoom;

  const drawLevel = useCallback(() => {
    if (!level || !canvasRef.current) return;

    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Set canvas size to container size
    const container = containerRef.current;
    if (container) {
      canvas.width = container.clientWidth;
      canvas.height = container.clientHeight;
    }

    // Clear canvas
    ctx.fillStyle = '#f5f5f5';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Save context for transformations
    ctx.save();
    ctx.translate(pan.x, pan.y);

    // Draw tiles
    for (let y = 0; y < level.height; y++) {
      for (let x = 0; x < level.width; x++) {
        const tileType = level.tiles[y]?.[x] || 'empty';
        
        // Set color based on tile type
        ctx.fillStyle = tileColors[tileType] || tileColors.empty;
        ctx.fillRect(x * tileSize, y * tileSize, tileSize, tileSize);
        
        // Highlight selected tile
        if (selectedTile && selectedTile.x === x && selectedTile.y === y) {
          ctx.strokeStyle = '#ff4444';
          ctx.lineWidth = 3;
          ctx.strokeRect(x * tileSize, y * tileSize, tileSize, tileSize);
        }
        
        // Draw grid lines
        if (showGrid && tileSize > 8) {
          ctx.strokeStyle = '#cccccc';
          ctx.lineWidth = 0.5;
          ctx.strokeRect(x * tileSize, y * tileSize, tileSize, tileSize);
        }
      }
    }

    // Draw entities
    level.entities.forEach(entity => {
      const centerX = entity.x * tileSize + tileSize / 2;
      const centerY = entity.y * tileSize + tileSize / 2;
      const radius = Math.max(3, tileSize / 3);

      ctx.beginPath();
      ctx.arc(centerX, centerY, radius, 0, 2 * Math.PI);
      
      // Set color based on entity type
      ctx.fillStyle = entityColors[entity.type] || entityColors.item;
      ctx.fill();
      
      // Highlight hovered entity
      if (hoveredEntity && hoveredEntity.x === entity.x && hoveredEntity.y === entity.y) {
        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 3;
        ctx.stroke();
      } else {
        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 1;
        ctx.stroke();
      }

      // Draw entity type label if zoomed in enough
      if (tileSize > 30) {
        ctx.fillStyle = '#000000';
        ctx.font = `${Math.max(8, tileSize / 4)}px Arial`;
        ctx.textAlign = 'center';
        ctx.fillText(entity.type[0].toUpperCase(), centerX, centerY + radius + 12);
      }
    });

    ctx.restore();
  }, [level, zoom, pan, showGrid, selectedTile, hoveredEntity, tileSize]);

  useEffect(() => {
    drawLevel();
  }, [drawLevel]);

  useEffect(() => {
    const handleResize = () => drawLevel();
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, [drawLevel]);

  const handleMouseDown = (e: React.MouseEvent) => {
    setIsDragging(true);
    setLastMousePos({ x: e.clientX, y: e.clientY });
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!level) return;

    const canvas = canvasRef.current;
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();
    const mouseX = e.clientX - rect.left - pan.x;
    const mouseY = e.clientY - rect.top - pan.y;

    if (isDragging) {
      const deltaX = e.clientX - lastMousePos.x;
      const deltaY = e.clientY - lastMousePos.y;
      setPan(prev => ({ x: prev.x + deltaX, y: prev.y + deltaY }));
      setLastMousePos({ x: e.clientX, y: e.clientY });
    } else {
      // Check for entity hover
      const hoveredEnt = level.entities.find(entity => {
        const centerX = entity.x * tileSize + tileSize / 2;
        const centerY = entity.y * tileSize + tileSize / 2;
        const radius = Math.max(3, tileSize / 3);
        const distance = Math.sqrt((mouseX - centerX) ** 2 + (mouseY - centerY) ** 2);
        return distance <= radius;
      });
      setHoveredEntity(hoveredEnt || null);
    }
  };

  const handleMouseUp = () => {
    setIsDragging(false);
  };

  const handleClick = (e: React.MouseEvent) => {
    if (!level || isDragging) return;

    const canvas = canvasRef.current;
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();
    const mouseX = e.clientX - rect.left - pan.x;
    const mouseY = e.clientY - rect.top - pan.y;

    // Check if clicking on an entity
    const clickedEntity = level.entities.find(entity => {
      const centerX = entity.x * tileSize + tileSize / 2;
      const centerY = entity.y * tileSize + tileSize / 2;
      const radius = Math.max(3, tileSize / 3);
      const distance = Math.sqrt((mouseX - centerX) ** 2 + (mouseY - centerY) ** 2);
      return distance <= radius;
    });

    if (clickedEntity) {
      onEntityClick?.(clickedEntity);
      return;
    }

    // Otherwise, handle tile click
    const tileX = Math.floor(mouseX / tileSize);
    const tileY = Math.floor(mouseY / tileSize);

    if (tileX >= 0 && tileX < level.width && tileY >= 0 && tileY < level.height) {
      setSelectedTile({ x: tileX, y: tileY });
      const currentTileType = level.tiles[tileY][tileX];
      onTileClick?.(tileX, tileY, currentTileType);
    }
  };

  const handleZoomIn = () => {
    setZoom(prev => Math.min(prev * 1.2, 5));
  };

  const handleZoomOut = () => {
    setZoom(prev => Math.max(prev / 1.2, 0.1));
  };

  const handleCenter = () => {
    if (!level || !containerRef.current) return;
    
    const container = containerRef.current;
    const levelWidth = level.width * tileSize;
    const levelHeight = level.height * tileSize;
    
    setPan({
      x: (container.clientWidth - levelWidth) / 2,
      y: (container.clientHeight - levelHeight) / 2
    });
  };

  const handleWheel = (e: React.WheelEvent) => {
    e.preventDefault();
    if (e.deltaY < 0) {
      handleZoomIn();
    } else {
      handleZoomOut();
    }
  };

  const getTileStats = () => {
    if (!level) return {};
    
    const stats: Record<string, number> = {};
    for (let y = 0; y < level.height; y++) {
      for (let x = 0; x < level.width; x++) {
        const tileType = level.tiles[y][x];
        stats[tileType] = (stats[tileType] || 0) + 1;
      }
    }
    return stats;
  };

  const getEntityStats = () => {
    if (!level) return {};
    
    const stats: Record<string, number> = {};
    level.entities.forEach(entity => {
      stats[entity.type] = (stats[entity.type] || 0) + 1;
    });
    return stats;
  };

  const tileStats = getTileStats();
  const entityStats = getEntityStats();

  return (
    <Paper sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">Level Preview</Typography>
          
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <ButtonGroup size="small">
              <Tooltip title="Zoom In">
                <IconButton onClick={handleZoomIn} disabled={isLoading}>
                  <ZoomInIcon />
                </IconButton>
              </Tooltip>
              <Tooltip title="Zoom Out">
                <IconButton onClick={handleZoomOut} disabled={isLoading}>
                  <ZoomOutIcon />
                </IconButton>
              </Tooltip>
              <Tooltip title="Center View">
                <IconButton onClick={handleCenter} disabled={isLoading}>
                  <CenterIcon />
                </IconButton>
              </Tooltip>
            </ButtonGroup>
            
            <Tooltip title="Toggle Grid">
              <IconButton onClick={() => setShowGrid(!showGrid)} disabled={isLoading}>
                {showGrid ? <GridOffIcon /> : <GridIcon />}
              </IconButton>
            </Tooltip>
            
            <Tooltip title="Color Palette">
              <IconButton 
                onClick={(e) => setPaletteAnchor(e.currentTarget)} 
                disabled={isLoading}
              >
                <PaletteIcon />
              </IconButton>
            </Tooltip>
          </Box>
        </Box>

        {level && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mt: 1 }}>
            <Typography variant="body2" color="text.secondary">
              {level.width} × {level.height} • Zoom: {Math.round(zoom * 100)}%
            </Typography>
            {selectedTile && (
              <Chip 
                size="small" 
                label={`Selected: (${selectedTile.x}, ${selectedTile.y})`}
                onDelete={() => setSelectedTile(null)}
              />
            )}
          </Box>
        )}
      </Box>

      <Box 
        ref={containerRef}
        sx={{ 
          flex: 1, 
          position: 'relative',
          overflow: 'hidden',
          cursor: isDragging ? 'grabbing' : 'grab'
        }}
      >
        {isLoading ? (
          <Box sx={{ 
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 2
          }}>
            <CircularProgress />
            <Typography variant="body2" color="text.secondary">
              Generating level...
            </Typography>
          </Box>
        ) : level ? (
          <canvas
            ref={canvasRef}
            onMouseDown={handleMouseDown}
            onMouseMove={handleMouseMove}
            onMouseUp={handleMouseUp}
            onClick={handleClick}
            onWheel={handleWheel}
            style={{ display: 'block', width: '100%', height: '100%' }}
          />
        ) : (
          <Box sx={{ 
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            textAlign: 'center'
          }}>
            <Typography variant="body2" color="text.secondary">
              Configure parameters and click "Generate Level" to see preview
            </Typography>
          </Box>
        )}
      </Box>

      {/* Statistics */}
      {level && (
        <Box sx={{ p: 2, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="subtitle2" gutterBottom>
            Statistics
          </Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 1 }}>
            {Object.entries(tileStats).map(([type, count]) => (
              <Chip
                key={type}
                size="small"
                label={`${type}: ${count}`}
                sx={{ backgroundColor: tileColors[type], color: '#000' }}
              />
            ))}
          </Box>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {Object.entries(entityStats).map(([type, count]) => (
              <Chip
                key={type}
                size="small"
                label={`${type}: ${count}`}
                sx={{ backgroundColor: entityColors[type], color: '#fff' }}
              />
            ))}
          </Box>
        </Box>
      )}

      {/* Color Palette Menu */}
      <Menu
        anchorEl={paletteAnchor}
        open={Boolean(paletteAnchor)}
        onClose={() => setPaletteAnchor(null)}
      >
        <Box sx={{ p: 2, minWidth: 200 }}>
          <Typography variant="subtitle2" gutterBottom>Terrain Colors</Typography>
          {Object.entries(tileColors).map(([type, color]) => (
            <Box key={type} sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
              <Box sx={{ width: 16, height: 16, backgroundColor: color, border: 1, borderColor: 'divider' }} />
              <Typography variant="body2">{type}</Typography>
            </Box>
          ))}
          <Typography variant="subtitle2" gutterBottom sx={{ mt: 2 }}>Entity Colors</Typography>
          {Object.entries(entityColors).map(([type, color]) => (
            <Box key={type} sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
              <Box sx={{ width: 16, height: 16, backgroundColor: color, borderRadius: '50%' }} />
              <Typography variant="body2">{type}</Typography>
            </Box>
          ))}
        </Box>
      </Menu>
    </Paper>
  );
};