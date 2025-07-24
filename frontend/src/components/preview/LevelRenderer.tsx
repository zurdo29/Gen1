import React, { useRef, useEffect, useCallback, useState } from 'react';
import { Box, Paper, IconButton, Tooltip, Typography, Stack } from '@mui/material';
import { ZoomIn, ZoomOut, CenterFocusStrong, GridOn, GridOff, Undo, Redo } from '@mui/icons-material';
import { Level, ViewportState, RenderOptions, Position, Entity } from '../../types/level';
import { TerrainEditMenu } from './TerrainEditMenu';
import { EntityEditToolbar, EditMode, EntityType } from './EntityEditToolbar';
import { EditValidationResult } from '../../hooks/useLevel';

interface LevelRendererProps {
  level: Level | null;
  isLoading?: boolean;
  canUndo?: boolean;
  canRedo?: boolean;
  onTileClick?: (x: number, y: number, terrainType?: string) => EditValidationResult;
  onEntityClick?: (entity: Entity) => void;
  onEntityDrag?: (entityId: string, newPosition: Position) => EditValidationResult;
  onEntityAdd?: (entityType: EntityType, position: Position) => EditValidationResult;
  onEntityRemove?: (entityId: string) => EditValidationResult;
  onUndo?: () => void;
  onRedo?: () => void;
  className?: string;
  editingEnabled?: boolean;
}

const DEFAULT_TILE_SIZE = 32;
const MIN_SCALE = 0.25;
const MAX_SCALE = 4;
const GRID_COLOR = '#cccccc';
const HOVER_COLOR = 'rgba(255, 255, 0, 0.3)';

// Terrain type colors
const TERRAIN_COLORS: Record<string, string> = {
  ground: '#8B4513',
  wall: '#696969',
  water: '#4169E1',
  grass: '#228B22',
  stone: '#708090',
  sand: '#F4A460',
  lava: '#FF4500',
  ice: '#B0E0E6'
};

// Entity type colors (fallback when no sprites available)
const ENTITY_COLORS: Record<string, string> = {
  Player: '#00FF00',
  Enemy: '#FF0000',
  Item: '#FFD700',
  PowerUp: '#FF69B4',
  NPC: '#9370DB',
  Exit: '#00CED1',
  Checkpoint: '#32CD32',
  Obstacle: '#8B4513',
  Trigger: '#FF6347'
};

export const LevelRenderer: React.FC<LevelRendererProps> = ({
  level,
  isLoading = false,
  canUndo = false,
  canRedo = false,
  onTileClick,
  onEntityClick,
  onEntityAdd,
  onEntityRemove,
  onUndo,
  onRedo,
  editingEnabled = true,
  onEntityDrag,
  className
}) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  
  const [viewport, setViewport] = useState<ViewportState>({
    offsetX: 0,
    offsetY: 0,
    scale: 1,
    tileSize: DEFAULT_TILE_SIZE
  });
  
  const [renderOptions, setRenderOptions] = useState<RenderOptions>({
    showGrid: true,
    showCoordinates: false,
    showSpawnPoints: true,
    highlightHoveredTile: true
  });
  
  const [hoveredTile, setHoveredTile] = useState<Position | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState<Position | null>(null);
  const [draggedEntity, setDraggedEntity] = useState<Entity | null>(null);
  const [editMode, setEditMode] = useState<EditMode>('select');
  const [selectedEntityType, setSelectedEntityType] = useState<EntityType>('Player');
  const [terrainMenuOpen, setTerrainMenuOpen] = useState(false);
  const [terrainMenuPosition, setTerrainMenuPosition] = useState<{ top: number; left: number } | null>(null);
  const [selectedTilePosition, setSelectedTilePosition] = useState<Position | null>(null);

  // Calculate canvas dimensions
  const getCanvasDimensions = useCallback(() => {
    if (!containerRef.current) return { width: 800, height: 600 };
    
    const container = containerRef.current;
    const rect = container.getBoundingClientRect();
    return {
      width: Math.max(400, rect.width - 32), // Account for padding
      height: Math.max(300, rect.height - 100) // Account for controls
    };
  }, []);

  // Convert screen coordinates to world coordinates
  const screenToWorld = useCallback((screenX: number, screenY: number): Position => {
    const canvas = canvasRef.current;
    if (!canvas) return { x: 0, y: 0 };
    
    const rect = canvas.getBoundingClientRect();
    const x = (screenX - rect.left - viewport.offsetX) / (viewport.tileSize * viewport.scale);
    const y = (screenY - rect.top - viewport.offsetY) / (viewport.tileSize * viewport.scale);
    
    return { x: Math.floor(x), y: Math.floor(y) };
  }, [viewport]);

  // Convert world coordinates to screen coordinates
  const worldToScreen = useCallback((worldX: number, worldY: number): Position => {
    const x = worldX * viewport.tileSize * viewport.scale + viewport.offsetX;
    const y = worldY * viewport.tileSize * viewport.scale + viewport.offsetY;
    return { x, y };
  }, [viewport]);

  // Render terrain tiles
  const renderTerrain = useCallback((ctx: CanvasRenderingContext2D) => {
    if (!level) return;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    
    for (let y = 0; y < level.height; y++) {
      for (let x = 0; x < level.width; x++) {
        const tile = level.tiles[y]?.[x];
        if (!tile) continue;
        
        const screenPos = worldToScreen(x, y);
        
        // Skip tiles outside viewport
        if (ctx.canvas && (screenPos.x + scaledTileSize < 0 || screenPos.y + scaledTileSize < 0 ||
            screenPos.x > ctx.canvas.width || screenPos.y > ctx.canvas.height)) {
          continue;
        }
        
        // Fill tile with terrain color
        ctx.fillStyle = TERRAIN_COLORS[tile.type] || '#888888';
        ctx.fillRect(screenPos.x, screenPos.y, scaledTileSize, scaledTileSize);
        
        // Add tile border for better visibility
        ctx.strokeStyle = '#000000';
        ctx.lineWidth = 0.5;
        ctx.strokeRect(screenPos.x, screenPos.y, scaledTileSize, scaledTileSize);
      }
    }
  }, [level, viewport, worldToScreen]);

  // Render entities
  const renderEntities = useCallback((ctx: CanvasRenderingContext2D) => {
    if (!level) return;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    const entitySize = scaledTileSize * 0.8; // Slightly smaller than tile
    const offset = (scaledTileSize - entitySize) / 2;
    
    level.entities.forEach(entity => {
      const screenPos = worldToScreen(entity.position.x, entity.position.y);
      
      // Skip entities outside viewport
      if (ctx.canvas && (screenPos.x + entitySize < 0 || screenPos.y + entitySize < 0 ||
          screenPos.x > ctx.canvas.width || screenPos.y > ctx.canvas.height)) {
        return;
      }
      
      // Draw entity as colored circle
      ctx.fillStyle = ENTITY_COLORS[entity.type] || '#FF00FF';
      ctx.beginPath();
      ctx.arc(
        screenPos.x + offset + entitySize / 2,
        screenPos.y + offset + entitySize / 2,
        entitySize / 2,
        0,
        2 * Math.PI
      );
      ctx.fill();
      
      // Add entity border
      ctx.strokeStyle = '#000000';
      ctx.lineWidth = 1;
      ctx.stroke();
      
      // Add entity type label if scale is large enough
      if (viewport.scale >= 1) {
        ctx.fillStyle = '#000000';
        ctx.font = `${Math.max(8, scaledTileSize / 4)}px Arial`;
        ctx.textAlign = 'center';
        ctx.fillText(
          entity.type.charAt(0),
          screenPos.x + scaledTileSize / 2,
          screenPos.y + scaledTileSize / 2 + 3
        );
      }
    });
  }, [level, viewport, worldToScreen]);

  // Render spawn points
  const renderSpawnPoints = useCallback((ctx: CanvasRenderingContext2D) => {
    if (!level || !renderOptions.showSpawnPoints) return;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    
    level.spawnPoints.forEach(spawnPoint => {
      const screenPos = worldToScreen(spawnPoint.x, spawnPoint.y);
      
      // Draw spawn point as green star
      ctx.fillStyle = '#00FF00';
      ctx.strokeStyle = '#000000';
      ctx.lineWidth = 2;
      
      const centerX = screenPos.x + scaledTileSize / 2;
      const centerY = screenPos.y + scaledTileSize / 2;
      const radius = scaledTileSize / 4;
      
      // Draw star shape
      ctx.beginPath();
      for (let i = 0; i < 5; i++) {
        const angle = (i * 4 * Math.PI) / 5;
        const x = centerX + Math.cos(angle) * radius;
        const y = centerY + Math.sin(angle) * radius;
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    });
  }, [level, viewport, renderOptions.showSpawnPoints, worldToScreen]);

  // Render grid overlay
  const renderGrid = useCallback((ctx: CanvasRenderingContext2D) => {
    if (!renderOptions.showGrid || !level) return;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    
    ctx.strokeStyle = GRID_COLOR;
    ctx.lineWidth = 0.5;
    
    // Vertical lines
    for (let x = 0; x <= level.width; x++) {
      const screenX = x * scaledTileSize + viewport.offsetX;
      if (ctx.canvas && screenX >= 0 && screenX <= ctx.canvas.width) {
        ctx.beginPath();
        ctx.moveTo(screenX, Math.max(0, viewport.offsetY));
        ctx.lineTo(screenX, Math.min(ctx.canvas.height, level.height * scaledTileSize + viewport.offsetY));
        ctx.stroke();
      }
    }
    
    // Horizontal lines
    for (let y = 0; y <= level.height; y++) {
      const screenY = y * scaledTileSize + viewport.offsetY;
      if (ctx.canvas && screenY >= 0 && screenY <= ctx.canvas.height) {
        ctx.beginPath();
        ctx.moveTo(Math.max(0, viewport.offsetX), screenY);
        ctx.lineTo(Math.min(ctx.canvas.width, level.width * scaledTileSize + viewport.offsetX), screenY);
        ctx.stroke();
      }
    }
  }, [level, viewport, renderOptions.showGrid]);

  // Render hover highlight
  const renderHoverHighlight = useCallback((ctx: CanvasRenderingContext2D) => {
    if (!hoveredTile || !renderOptions.highlightHoveredTile) return;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    const screenPos = worldToScreen(hoveredTile.x, hoveredTile.y);
    
    // Different highlight colors based on edit mode
    let highlightColor = HOVER_COLOR;
    if (editingEnabled) {
      switch (editMode) {
        case 'add':
          highlightColor = 'rgba(0, 255, 0, 0.3)'; // Green for add
          break;
        case 'delete':
          highlightColor = 'rgba(255, 0, 0, 0.3)'; // Red for delete
          break;
        default:
          highlightColor = HOVER_COLOR; // Yellow for select
      }
    }
    
    ctx.fillStyle = highlightColor;
    ctx.fillRect(screenPos.x, screenPos.y, scaledTileSize, scaledTileSize);
  }, [hoveredTile, viewport, renderOptions.highlightHoveredTile, worldToScreen, editingEnabled, editMode]);

  // Render drag preview
  const renderDragPreview = useCallback((ctx: CanvasRenderingContext2D) => {
    if (!draggedEntity || !hoveredTile) return;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    const entitySize = scaledTileSize * 0.8;
    const offset = (scaledTileSize - entitySize) / 2;
    const screenPos = worldToScreen(hoveredTile.x, hoveredTile.y);
    
    // Draw semi-transparent preview of dragged entity
    ctx.globalAlpha = 0.6;
    ctx.fillStyle = ENTITY_COLORS[draggedEntity.type] || '#FF00FF';
    ctx.beginPath();
    ctx.arc(
      screenPos.x + offset + entitySize / 2,
      screenPos.y + offset + entitySize / 2,
      entitySize / 2,
      0,
      2 * Math.PI
    );
    ctx.fill();
    
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 2;
    ctx.stroke();
    ctx.globalAlpha = 1.0;
  }, [draggedEntity, hoveredTile, viewport, worldToScreen]);

  // Main render function
  const render = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    
    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    
    if (isLoading) {
      // Show loading state
      ctx.fillStyle = '#f0f0f0';
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      ctx.fillStyle = '#666666';
      ctx.font = '16px Arial';
      ctx.textAlign = 'center';
      ctx.fillText('Generating level...', canvas.width / 2, canvas.height / 2);
      return;
    }
    
    if (!level) {
      // Show empty state
      ctx.fillStyle = '#f8f8f8';
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      ctx.fillStyle = '#999999';
      ctx.font = '16px Arial';
      ctx.textAlign = 'center';
      ctx.fillText('No level to display', canvas.width / 2, canvas.height / 2);
      return;
    }
    
    // Render level components
    renderTerrain(ctx);
    renderGrid(ctx);
    renderHoverHighlight(ctx);
    renderEntities(ctx);
    renderSpawnPoints(ctx);
    renderDragPreview(ctx);
  }, [level, isLoading, viewport, renderOptions, hoveredTile, renderTerrain, renderGrid, renderHoverHighlight, renderEntities, renderSpawnPoints]);

  // Handle canvas resize
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    
    const dimensions = getCanvasDimensions();
    canvas.width = dimensions.width;
    canvas.height = dimensions.height;
    
    render();
  }, [getCanvasDimensions, render]);

  // Handle level changes
  useEffect(() => {
    render();
  }, [render]);

  // Center level in viewport
  const centerLevel = useCallback(() => {
    if (!level) return;
    
    const canvas = canvasRef.current;
    if (!canvas) return;
    
    const levelWidth = level.width * viewport.tileSize * viewport.scale;
    const levelHeight = level.height * viewport.tileSize * viewport.scale;
    
    setViewport(prev => ({
      ...prev,
      offsetX: (canvas.width - levelWidth) / 2,
      offsetY: (canvas.height - levelHeight) / 2
    }));
  }, [level, viewport.tileSize, viewport.scale]);

  // Zoom functions
  const zoomIn = useCallback(() => {
    setViewport(prev => ({
      ...prev,
      scale: Math.min(MAX_SCALE, prev.scale * 1.2)
    }));
  }, []);

  const zoomOut = useCallback(() => {
    setViewport(prev => ({
      ...prev,
      scale: Math.max(MIN_SCALE, prev.scale / 1.2)
    }));
  }, []);

  // Mouse event handlers
  const handleMouseMove = useCallback((event: React.MouseEvent<HTMLCanvasElement>) => {
    if (!level) return;
    
    const worldPos = screenToWorld(event.clientX, event.clientY);
    
    // Update hovered tile
    if (worldPos.x >= 0 && worldPos.x < level.width && worldPos.y >= 0 && worldPos.y < level.height) {
      setHoveredTile(worldPos);
    } else {
      setHoveredTile(null);
    }
    
    // Handle dragging
    if (isDragging && dragStart) {
      if (draggedEntity) {
        // Entity dragging - just update cursor, actual move happens on mouse up
        // Visual feedback could be added here
      } else {
        // Viewport dragging
        const deltaX = event.clientX - dragStart.x;
        const deltaY = event.clientY - dragStart.y;
        
        setViewport(prev => ({
          ...prev,
          offsetX: prev.offsetX + deltaX,
          offsetY: prev.offsetY + deltaY
        }));
        
        setDragStart({ x: event.clientX, y: event.clientY });
      }
    }
  }, [level, screenToWorld, isDragging, dragStart, draggedEntity]);

  const handleMouseDown = useCallback((event: React.MouseEvent<HTMLCanvasElement>) => {
    const worldPos = screenToWorld(event.clientX, event.clientY);
    
    if (editingEnabled && editMode === 'select' && level && 
        worldPos.x >= 0 && worldPos.x < level.width && worldPos.y >= 0 && worldPos.y < level.height) {
      
      // Check if clicking on an entity for dragging
      const clickedEntity = level.entities.find(entity => 
        entity.position.x === worldPos.x && entity.position.y === worldPos.y
      );
      
      if (clickedEntity) {
        setDraggedEntity(clickedEntity);
        setIsDragging(true);
        setDragStart({ x: event.clientX, y: event.clientY });
        return;
      }
    }
    
    // Default viewport dragging
    setIsDragging(true);
    setDragStart({ x: event.clientX, y: event.clientY });
  }, [editingEnabled, editMode, level, screenToWorld]);

  const handleMouseUp = useCallback((event: React.MouseEvent<HTMLCanvasElement>) => {
    if (isDragging && draggedEntity) {
      // Handle entity drag end
      const worldPos = screenToWorld(event.clientX, event.clientY);
      
      if (level && worldPos.x >= 0 && worldPos.x < level.width && worldPos.y >= 0 && worldPos.y < level.height) {
        if (onEntityDrag) {
          const result = onEntityDrag(draggedEntity.id, worldPos);
          if (!result.isValid) {
            console.warn('Entity drag failed:', result.errors);
          }
        }
      }
      
      setDraggedEntity(null);
      setIsDragging(false);
      setDragStart(null);
    } else if (isDragging) {
      // Handle viewport drag end
      setIsDragging(false);
      setDragStart(null);
    } else if (editingEnabled) {
      // Handle click interactions
      const worldPos = screenToWorld(event.clientX, event.clientY);
      
      if (level && worldPos.x >= 0 && worldPos.x < level.width && worldPos.y >= 0 && worldPos.y < level.height) {
        // Check if clicking on an entity
        const clickedEntity = level.entities.find(entity => 
          entity.position.x === worldPos.x && entity.position.y === worldPos.y
        );
        
        if (clickedEntity) {
          if (editMode === 'delete' && onEntityRemove) {
            const result = onEntityRemove(clickedEntity.id);
            if (!result.isValid) {
              console.warn('Entity removal failed:', result.errors);
            }
          } else if (editMode === 'select' && onEntityClick) {
            onEntityClick(clickedEntity);
          }
        } else {
          // Clicked on empty tile
          if (editMode === 'add' && onEntityAdd) {
            const result = onEntityAdd(selectedEntityType, worldPos);
            if (!result.isValid) {
              console.warn('Entity addition failed:', result.errors);
            }
          } else if (event.button === 2) { // Right click for terrain editing
            event.preventDefault();
            setSelectedTilePosition(worldPos);
            setTerrainMenuPosition({ 
              top: event.clientY, 
              left: event.clientX 
            });
            setTerrainMenuOpen(true);
          }
        }
      }
    }
  }, [level, isDragging, draggedEntity, editMode, selectedEntityType, editingEnabled, screenToWorld, onTileClick, onEntityClick, onEntityDrag, onEntityAdd, onEntityRemove]);

  const handleWheel = useCallback((event: React.WheelEvent<HTMLCanvasElement>) => {
    event.preventDefault();
    
    if (event.deltaY < 0) {
      zoomIn();
    } else {
      zoomOut();
    }
  }, [zoomIn, zoomOut]);

  const handleContextMenu = useCallback((event: React.MouseEvent<HTMLCanvasElement>) => {
    event.preventDefault(); // Prevent default context menu
  }, []);

  const handleTerrainSelect = useCallback((terrainType: string) => {
    if (selectedTilePosition && onTileClick) {
      const result = onTileClick(selectedTilePosition.x, selectedTilePosition.y, terrainType);
      if (!result.isValid) {
        console.warn('Terrain change failed:', result.errors);
      }
    }
    setTerrainMenuOpen(false);
    setSelectedTilePosition(null);
  }, [selectedTilePosition, onTileClick]);

  const handleTerrainMenuClose = useCallback(() => {
    setTerrainMenuOpen(false);
    setSelectedTilePosition(null);
  }, []);

  return (
    <Box className={className} sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      {/* Entity Editing Toolbar */}
      {editingEnabled && (
        <EntityEditToolbar
          editMode={editMode}
          selectedEntityType={selectedEntityType}
          onEditModeChange={setEditMode}
          onEntityTypeChange={setSelectedEntityType}
          disabled={isLoading}
        />
      )}

      <Paper sx={{ p: 2, flex: 1, display: 'flex', flexDirection: 'column' }}>
        {/* Controls */}
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
          <Tooltip title="Zoom In">
            <IconButton onClick={zoomIn} size="small">
              <ZoomIn />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Zoom Out">
            <IconButton onClick={zoomOut} size="small">
              <ZoomOut />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Center Level">
            <IconButton onClick={centerLevel} size="small">
              <CenterFocusStrong />
            </IconButton>
          </Tooltip>
          
          <Tooltip title={renderOptions.showGrid ? "Hide Grid" : "Show Grid"}>
            <IconButton 
              onClick={() => setRenderOptions(prev => ({ ...prev, showGrid: !prev.showGrid }))}
              size="small"
              color={renderOptions.showGrid ? "primary" : "default"}
            >
              {renderOptions.showGrid ? <GridOff /> : <GridOn />}
            </IconButton>
          </Tooltip>

          {/* Undo/Redo Controls */}
          {editingEnabled && (
            <>
              <Tooltip title="Undo">
                <span>
                  <IconButton 
                    onClick={onUndo} 
                    size="small" 
                    disabled={!canUndo}
                  >
                    <Undo />
                  </IconButton>
                </span>
              </Tooltip>
              
              <Tooltip title="Redo">
                <span>
                  <IconButton 
                    onClick={onRedo} 
                    size="small" 
                    disabled={!canRedo}
                  >
                    <Redo />
                  </IconButton>
                </span>
              </Tooltip>
            </>
          )}
          
          <Typography variant="body2" sx={{ ml: 'auto' }}>
            Scale: {Math.round(viewport.scale * 100)}%
          </Typography>
          
          {hoveredTile && (
            <Typography variant="body2">
              Tile: ({hoveredTile.x}, {hoveredTile.y})
            </Typography>
          )}

          {editingEnabled && (
            <Typography variant="body2" color="primary">
              Mode: {editMode.charAt(0).toUpperCase() + editMode.slice(1)}
            </Typography>
          )}
        </Stack>
        
        {/* Canvas */}
        <Box ref={containerRef} sx={{ flex: 1, overflow: 'hidden' }}>
          <canvas
            ref={canvasRef}
            onMouseMove={handleMouseMove}
            onMouseDown={handleMouseDown}
            onMouseUp={handleMouseUp}
            onWheel={handleWheel}
            onContextMenu={handleContextMenu}
            style={{
              border: '1px solid #ccc',
              cursor: getCursor(),
              display: 'block'
            }}
          />
        </Box>
      </Paper>

      {/* Terrain Edit Menu */}
      <TerrainEditMenu
        open={terrainMenuOpen}
        anchorPosition={terrainMenuPosition}
        currentTerrainType={selectedTilePosition && level ? 
          level.tiles[selectedTilePosition.y][selectedTilePosition.x].type : 'ground'}
        onClose={handleTerrainMenuClose}
        onTerrainSelect={handleTerrainSelect}
      />
    </Box>
  );

  function getCursor(): string {
    if (isDragging) {
      return draggedEntity ? 'grabbing' : 'grabbing';
    }
    
    if (!editingEnabled) {
      return 'grab';
    }
    
    switch (editMode) {
      case 'add':
        return 'crosshair';
      case 'delete':
        return 'not-allowed';
      case 'select':
        return draggedEntity ? 'grabbing' : 'grab';
      default:
        return 'grab';
    }
  }
};

export default LevelRenderer;