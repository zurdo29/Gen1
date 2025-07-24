import React, { useRef, useEffect } from 'react';
import { Paper, Typography, Box, CircularProgress } from '@mui/material';

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

interface SimpleLevelPreviewProps {
  level: SimpleLevel | null;
  isLoading?: boolean;
}

export const SimpleLevelPreview: React.FC<SimpleLevelPreviewProps> = ({
  level,
  isLoading = false
}) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    if (!level || !canvasRef.current) return;

    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Set canvas size
    const tileSize = Math.min(400 / level.width, 400 / level.height);
    canvas.width = level.width * tileSize;
    canvas.height = level.height * tileSize;

    // Clear canvas
    ctx.fillStyle = '#f5f5f5';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Draw tiles
    for (let y = 0; y < level.height; y++) {
      for (let x = 0; x < level.width; x++) {
        const tileType = level.tiles[y]?.[x] || 'empty';
        
        // Set color based on tile type
        switch (tileType) {
          case 'wall':
            ctx.fillStyle = '#666666';
            break;
          case 'floor':
            ctx.fillStyle = '#e0e0e0';
            break;
          case 'water':
            ctx.fillStyle = '#4fc3f7';
            break;
          case 'grass':
            ctx.fillStyle = '#81c784';
            break;
          default:
            ctx.fillStyle = '#f5f5f5';
        }

        ctx.fillRect(x * tileSize, y * tileSize, tileSize, tileSize);
        
        // Draw grid lines
        ctx.strokeStyle = '#cccccc';
        ctx.lineWidth = 0.5;
        ctx.strokeRect(x * tileSize, y * tileSize, tileSize, tileSize);
      }
    }

    // Draw entities
    level.entities.forEach(entity => {
      const centerX = entity.x * tileSize + tileSize / 2;
      const centerY = entity.y * tileSize + tileSize / 2;
      const radius = tileSize / 3;

      ctx.beginPath();
      ctx.arc(centerX, centerY, radius, 0, 2 * Math.PI);
      
      // Set color based on entity type
      switch (entity.type) {
        case 'player':
          ctx.fillStyle = '#2196f3';
          break;
        case 'enemy':
          ctx.fillStyle = '#f44336';
          break;
        case 'item':
          ctx.fillStyle = '#ff9800';
          break;
        case 'exit':
          ctx.fillStyle = '#4caf50';
          break;
        default:
          ctx.fillStyle = '#9c27b0';
      }
      
      ctx.fill();
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth = 2;
      ctx.stroke();
    });

  }, [level]);

  return (
    <Paper sx={{ p: 3, height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Typography variant="h6" gutterBottom>
        Level Preview
      </Typography>
      
      <Box 
        sx={{ 
          flex: 1, 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'center',
          position: 'relative'
        }}
      >
        {isLoading ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}>
            <CircularProgress />
            <Typography variant="body2" color="text.secondary">
              Generating level...
            </Typography>
          </Box>
        ) : level ? (
          <Box sx={{ textAlign: 'center' }}>
            <canvas
              ref={canvasRef}
              style={{
                border: '1px solid #ccc',
                borderRadius: '4px',
                maxWidth: '100%',
                maxHeight: '400px'
              }}
            />
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              {level.width} × {level.height} • {level.entities.length} entities
            </Typography>
          </Box>
        ) : (
          <Typography variant="body2" color="text.secondary">
            Configure parameters and click "Generate Level" to see preview
          </Typography>
        )}
      </Box>
    </Paper>
  );
};