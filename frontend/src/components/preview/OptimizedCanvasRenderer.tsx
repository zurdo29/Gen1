import React, { useRef, useEffect, useCallback, useState, useMemo } from 'react';
import { Level, ViewportState, RenderOptions, Position, Entity } from '../../types/level';

interface OptimizedCanvasRendererProps {
  level: Level | null;
  viewport: ViewportState;
  renderOptions: RenderOptions;
  hoveredTile?: Position | null;
  draggedEntity?: Entity | null;
  isLoading?: boolean;
  onRenderComplete?: () => void;
}

interface RenderLayer {
  name: string;
  render: (ctx: CanvasRenderingContext2D) => void;
  priority: number;
  shouldUpdate: boolean;
}

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

// Entity type colors
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

export const OptimizedCanvasRenderer: React.FC<OptimizedCanvasRendererProps> = ({
  level,
  viewport,
  renderOptions,
  hoveredTile,
  draggedEntity,
  isLoading = false,
  onRenderComplete
}) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const animationFrameRef = useRef<number>();
  const lastRenderTime = useRef<number>(0);
  const renderQueue = useRef<Set<string>>(new Set());
  const offscreenCanvas = useRef<HTMLCanvasElement>();
  const offscreenCtx = useRef<CanvasRenderingContext2D>();
  
  // Performance tracking
  const [renderStats, setRenderStats] = useState({
    fps: 0,
    renderTime: 0,
    lastUpdate: Date.now()
  });

  // Viewport culling - only render visible tiles
  const visibleBounds = useMemo(() => {
    if (!level) return null;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    const canvas = canvasRef.current;
    if (!canvas) return null;
    
    const startX = Math.max(0, Math.floor(-viewport.offsetX / scaledTileSize));
    const startY = Math.max(0, Math.floor(-viewport.offsetY / scaledTileSize));
    const endX = Math.min(level.width, Math.ceil((canvas.width - viewport.offsetX) / scaledTileSize));
    const endY = Math.min(level.height, Math.ceil((canvas.height - viewport.offsetY) / scaledTileSize));
    
    return { startX, startY, endX, endY };
  }, [level, viewport]);

  // Convert world coordinates to screen coordinates
  const worldToScreen = useCallback((worldX: number, worldY: number): Position => {
    const x = worldX * viewport.tileSize * viewport.scale + viewport.offsetX;
    const y = worldY * viewport.tileSize * viewport.scale + viewport.offsetY;
    return { x, y };
  }, [viewport]);

  // Optimized terrain rendering with culling
  const renderTerrain = useCallback((ctx: CanvasRenderingContext2D) => {
    if (!level || !visibleBounds) return;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    const { startX, startY, endX, endY } = visibleBounds;
    
    // Batch rendering for better performance
    const tileBatches: Record<string, Position[]> = {};
    
    for (let y = startY; y < endY; y++) {
      for (let x = startX; x < endX; x++) {
        const tile = level.tiles[y]?.[x];
        if (!tile) continue;
        
        const tileType = tile.type;
        if (!tileBatches[tileType]) {
          tileBatches[tileType] = [];
        }
        tileBatches[tileType].push({ x, y });
      }
    }
    
    // Render tiles by type for better performance
    Object.entries(tileBatches).forEach(([tileType, positions]) => {
      ctx.fillStyle = TERRAIN_COLORS[tileType] || '#888888';
      
      positions.forEach(({ x, y }) => {
        const screenPos = worldToScreen(x, y);
        ctx.fillRect(screenPos.x, screenPos.y, scaledTileSize, scaledTileSize);
      });
      
      // Add borders if scale is large enough
      if (viewport.scale >= 0.5) {
        ctx.strokeStyle = '#000000';
        ctx.lineWidth = 0.5;
        
        positions.forEach(({ x, y }) => {
          const screenPos = worldToScreen(x, y);
          ctx.strokeRect(screenPos.x, screenPos.y, scaledTileSize, scaledTileSize);
        });
      }
    });
  }, [level, viewport, visibleBounds, worldToScreen]);

  // Optimized entity rendering with culling
  const renderEntities = useCallback((ctx: CanvasRenderingContext2D) => {
    if (!level || !visibleBounds) return;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    const entitySize = scaledTileSize * 0.8;
    const offset = (scaledTileSize - entitySize) / 2;
    const { startX, startY, endX, endY } = visibleBounds;
    
    // Filter entities within visible bounds
    const visibleEntities = level.entities.filter(entity => 
      entity.position.x >= startX - 1 && entity.position.x <= endX + 1 &&
      entity.position.y >= startY - 1 && entity.position.y <= endY + 1
    );
    
    // Batch render entities by type
    const entityBatches: Record<string, Entity[]> = {};
    visibleEntities.forEach(entity => {
      if (!entityBatches[entity.type]) {
        entityBatches[entity.type] = [];
      }
      entityBatches[entity.type].push(entity);
    });
    
    Object.entries(entityBatches).forEach(([entityType, entities]) => {
      ctx.fillStyle = ENTITY_COLORS[entityType] || '#FF00FF';
      
      entities.forEach(entity => {
        const screenPos = worldToScreen(entity.position.x, entity.position.y);
        
        ctx.beginPath();
        ctx.arc(
          screenPos.x + offset + entitySize / 2,
          screenPos.y + offset + entitySize / 2,
          entitySize / 2,
          0,
          2 * Math.PI
        );
        ctx.fill();
      });
      
      // Add borders and labels if scale is large enough
      if (viewport.scale >= 1) {
        ctx.strokeStyle = '#000000';
        ctx.lineWidth = 1;
        ctx.font = `${Math.max(8, scaledTileSize / 4)}px Arial`;
        ctx.textAlign = 'center';
        ctx.fillStyle = '#000000';
        
        entities.forEach(entity => {
          const screenPos = worldToScreen(entity.position.x, entity.position.y);
          
          ctx.beginPath();
          ctx.arc(
            screenPos.x + offset + entitySize / 2,
            screenPos.y + offset + entitySize / 2,
            entitySize / 2,
            0,
            2 * Math.PI
          );
          ctx.stroke();
          
          ctx.fillText(
            entity.type.charAt(0),
            screenPos.x + scaledTileSize / 2,
            screenPos.y + scaledTileSize / 2 + 4
          );
        });
      }
    });
  }, [level, viewport, visibleBounds, worldToScreen]);

  // Optimized grid rendering
  const renderGrid = useCallback((ctx: CanvasRenderingContext2D) => {
    if (!renderOptions.showGrid || !level || !visibleBounds) return;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    const { startX, startY, endX, endY } = visibleBounds;
    
    // Only render grid if tiles are large enough to see
    if (scaledTileSize < 8) return;
    
    ctx.strokeStyle = '#cccccc';
    ctx.lineWidth = 0.5;
    ctx.globalAlpha = Math.min(1, scaledTileSize / 32); // Fade grid at small scales
    
    ctx.beginPath();
    
    // Vertical lines
    for (let x = startX; x <= endX; x++) {
      const screenX = x * scaledTileSize + viewport.offsetX;
      ctx.moveTo(screenX, startY * scaledTileSize + viewport.offsetY);
      ctx.lineTo(screenX, endY * scaledTileSize + viewport.offsetY);
    }
    
    // Horizontal lines
    for (let y = startY; y <= endY; y++) {
      const screenY = y * scaledTileSize + viewport.offsetY;
      ctx.moveTo(startX * scaledTileSize + viewport.offsetX, screenY);
      ctx.lineTo(endX * scaledTileSize + viewport.offsetX, screenY);
    }
    
    ctx.stroke();
    ctx.globalAlpha = 1;
  }, [renderOptions.showGrid, level, viewport, visibleBounds]);

  // Hover highlight rendering
  const renderHoverHighlight = useCallback((ctx: CanvasRenderingContext2D) => {
    if (!hoveredTile || !renderOptions.highlightHoveredTile) return;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    const screenPos = worldToScreen(hoveredTile.x, hoveredTile.y);
    
    ctx.fillStyle = 'rgba(255, 255, 0, 0.3)';
    ctx.fillRect(screenPos.x, screenPos.y, scaledTileSize, scaledTileSize);
    
    ctx.strokeStyle = '#FFD700';
    ctx.lineWidth = 2;
    ctx.strokeRect(screenPos.x, screenPos.y, scaledTileSize, scaledTileSize);
  }, [hoveredTile, renderOptions.highlightHoveredTile, viewport, worldToScreen]);

  // Drag preview rendering
  const renderDragPreview = useCallback((ctx: CanvasRenderingContext2D) => {
    if (!draggedEntity || !hoveredTile) return;
    
    const scaledTileSize = viewport.tileSize * viewport.scale;
    const entitySize = scaledTileSize * 0.8;
    const offset = (scaledTileSize - entitySize) / 2;
    const screenPos = worldToScreen(hoveredTile.x, hoveredTile.y);
    
    ctx.globalAlpha = 0.7;
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
    ctx.globalAlpha = 1;
  }, [draggedEntity, hoveredTile, viewport, worldToScreen]);

  // Loading state rendering
  const renderLoadingState = useCallback((ctx: CanvasRenderingContext2D) => {
    const canvas = ctx.canvas;
    
    ctx.fillStyle = '#f0f0f0';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    
    ctx.fillStyle = '#666666';
    ctx.font = '16px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('Generating level...', canvas.width / 2, canvas.height / 2);
    
    // Animated loading indicator
    const time = Date.now() / 1000;
    const dots = Math.floor(time * 2) % 4;
    ctx.fillText('.'.repeat(dots), canvas.width / 2 + 80, canvas.height / 2);
  }, []);

  // Empty state rendering
  const renderEmptyState = useCallback((ctx: CanvasRenderingContext2D) => {
    const canvas = ctx.canvas;
    
    ctx.fillStyle = '#f8f8f8';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    
    ctx.fillStyle = '#999999';
    ctx.font = '16px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('No level to display', canvas.width / 2, canvas.height / 2);
  }, []);

  // Define render layers with priorities
  const renderLayers = useMemo<RenderLayer[]>(() => [
    {
      name: 'terrain',
      render: renderTerrain,
      priority: 1,
      shouldUpdate: true
    },
    {
      name: 'grid',
      render: renderGrid,
      priority: 2,
      shouldUpdate: renderOptions.showGrid
    },
    {
      name: 'entities',
      render: renderEntities,
      priority: 3,
      shouldUpdate: true
    },
    {
      name: 'hover',
      render: renderHoverHighlight,
      priority: 4,
      shouldUpdate: !!hoveredTile && renderOptions.highlightHoveredTile
    },
    {
      name: 'drag',
      render: renderDragPreview,
      priority: 5,
      shouldUpdate: !!draggedEntity && !!hoveredTile
    }
  ], [renderTerrain, renderGrid, renderEntities, renderHoverHighlight, renderDragPreview, renderOptions, hoveredTile, draggedEntity]);

  // Main render function with performance optimization
  const render = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    
    const startTime = performance.now();
    
    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    
    if (isLoading) {
      renderLoadingState(ctx);
      onRenderComplete?.();
      return;
    }
    
    if (!level) {
      renderEmptyState(ctx);
      onRenderComplete?.();
      return;
    }
    
    // Render layers in priority order
    renderLayers
      .filter(layer => layer.shouldUpdate)
      .sort((a, b) => a.priority - b.priority)
      .forEach(layer => {
        try {
          layer.render(ctx);
        } catch (error) {
          console.error(`Error rendering layer ${layer.name}:`, error);
        }
      });
    
    // Update performance stats
    const renderTime = performance.now() - startTime;
    const now = Date.now();
    const deltaTime = now - renderStats.lastUpdate;
    
    if (deltaTime >= 1000) { // Update FPS every second
      const fps = Math.round(1000 / (now - lastRenderTime.current));
      setRenderStats({
        fps: isFinite(fps) ? fps : 0,
        renderTime: Math.round(renderTime * 100) / 100,
        lastUpdate: now
      });
    }
    
    lastRenderTime.current = now;
    onRenderComplete?.();
  }, [level, isLoading, renderLayers, renderLoadingState, renderEmptyState, renderStats.lastUpdate, onRenderComplete]);

  // Optimized render loop with requestAnimationFrame
  const scheduleRender = useCallback(() => {
    if (animationFrameRef.current) {
      cancelAnimationFrame(animationFrameRef.current);
    }
    
    animationFrameRef.current = requestAnimationFrame(() => {
      render();
      animationFrameRef.current = undefined;
    });
  }, [render]);

  // Queue render updates
  const queueRender = useCallback((reason: string) => {
    renderQueue.current.add(reason);
    scheduleRender();
  }, [scheduleRender]);

  // Handle canvas resize
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    
    const resizeObserver = new ResizeObserver((entries) => {
      for (const entry of entries) {
        const { width, height } = entry.contentRect;
        canvas.width = width;
        canvas.height = height;
        queueRender('resize');
      }
    });
    
    resizeObserver.observe(canvas);
    
    return () => {
      resizeObserver.disconnect();
    };
  }, [queueRender]);

  // Handle prop changes
  useEffect(() => {
    queueRender('level-change');
  }, [level, queueRender]);

  useEffect(() => {
    queueRender('viewport-change');
  }, [viewport, queueRender]);

  useEffect(() => {
    queueRender('options-change');
  }, [renderOptions, queueRender]);

  useEffect(() => {
    queueRender('hover-change');
  }, [hoveredTile, queueRender]);

  useEffect(() => {
    queueRender('drag-change');
  }, [draggedEntity, queueRender]);

  // Cleanup
  useEffect(() => {
    return () => {
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, []);

  return (
    <div style={{ position: 'relative', width: '100%', height: '100%' }}>
      <canvas
        ref={canvasRef}
        style={{
          display: 'block',
          width: '100%',
          height: '100%',
          imageRendering: 'pixelated' // Better for pixel art
        }}
      />
      
      {/* Performance overlay (development only) */}
      {process.env.NODE_ENV === 'development' && (
        <div
          style={{
            position: 'absolute',
            top: 8,
            right: 8,
            background: 'rgba(0, 0, 0, 0.7)',
            color: 'white',
            padding: '4px 8px',
            borderRadius: 4,
            fontSize: '12px',
            fontFamily: 'monospace',
            pointerEvents: 'none'
          }}
        >
          FPS: {renderStats.fps} | Render: {renderStats.renderTime}ms
        </div>
      )}
    </div>
  );
};