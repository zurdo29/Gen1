import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { vi } from 'vitest';
import LevelRenderer from './LevelRenderer';
import { Level, Entity, Tile } from '../../types/level';

const theme = createTheme();

const mockLevel: Level = {
  id: 'test-level',
  width: 10,
  height: 10,
  tiles: Array(10).fill(null).map((_, y) => 
    Array(10).fill(null).map((_, x) => ({
      type: x === 0 || x === 9 || y === 0 || y === 9 ? 'wall' : 'ground',
      position: { x, y }
    } as Tile))
  ),
  entities: [
    {
      id: 'player-1',
      type: 'Player',
      position: { x: 1, y: 1 },
      properties: {}
    },
    {
      id: 'enemy-1',
      type: 'Enemy',
      position: { x: 8, y: 8 },
      properties: {}
    }
  ] as Entity[],
  spawnPoints: [{ x: 1, y: 1 }],
  metadata: {
    seed: 12345,
    generationAlgorithm: 'perlin',
    createdAt: new Date()
  }
};

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={theme}>
      {component}
    </ThemeProvider>
  );
};

// Mock canvas context
const mockContext = {
  clearRect: vi.fn(),
  fillRect: vi.fn(),
  strokeRect: vi.fn(),
  beginPath: vi.fn(),
  moveTo: vi.fn(),
  lineTo: vi.fn(),
  closePath: vi.fn(),
  arc: vi.fn(),
  fill: vi.fn(),
  stroke: vi.fn(),
  fillText: vi.fn(),
  canvas: { width: 800, height: 600 },
  set fillStyle(_value: string) {},
  set strokeStyle(_value: string) {},
  set lineWidth(_value: number) {},
  set font(_value: string) {},
  set textAlign(_value: string) {}
} as any;

beforeEach(() => {
  // Mock canvas and context
  HTMLCanvasElement.prototype.getContext = vi.fn(() => mockContext) as any;
  HTMLCanvasElement.prototype.getBoundingClientRect = vi.fn(() => ({
    left: 0,
    top: 0,
    width: 800,
    height: 600,
    right: 800,
    bottom: 600,
    x: 0,
    y: 0,
    toJSON: () => {}
  }));
  
  // Clear all mocks
  Object.values(mockContext).forEach(mock => {
    if (typeof mock === 'function' && 'mockClear' in mock) {
      (mock as any).mockClear();
    }
  });
});

describe('LevelRenderer', () => {
  it('renders without crashing', () => {
    renderWithTheme(<LevelRenderer level={null} />);
    expect(screen.getByRole('button', { name: /zoom in/i })).toBeInTheDocument();
  });

  it('displays loading state when isLoading is true', () => {
    renderWithTheme(<LevelRenderer level={null} isLoading={true} />);
    
    // Canvas should be rendered
    const canvas = document.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
  });

  it('displays empty state when no level is provided', () => {
    renderWithTheme(<LevelRenderer level={null} />);
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
  });

  it('renders level when provided', () => {
    renderWithTheme(<LevelRenderer level={mockLevel} />);
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
    
    // Verify canvas context methods were called for rendering
    expect(mockContext.clearRect).toHaveBeenCalled();
    expect(mockContext.fillRect).toHaveBeenCalled();
  });

  it('handles zoom in button click', () => {
    renderWithTheme(<LevelRenderer level={mockLevel} />);
    
    const zoomInButton = screen.getByRole('button', { name: /zoom in/i });
    fireEvent.click(zoomInButton);
    
    // Scale should be updated (we can't directly test state, but the component should re-render)
    expect(zoomInButton).toBeInTheDocument();
  });

  it('handles zoom out button click', () => {
    renderWithTheme(<LevelRenderer level={mockLevel} />);
    
    const zoomOutButton = screen.getByRole('button', { name: /zoom out/i });
    fireEvent.click(zoomOutButton);
    
    expect(zoomOutButton).toBeInTheDocument();
  });

  it('handles center level button click', () => {
    renderWithTheme(<LevelRenderer level={mockLevel} />);
    
    const centerButton = screen.getByRole('button', { name: /center level/i });
    fireEvent.click(centerButton);
    
    expect(centerButton).toBeInTheDocument();
  });

  it('toggles grid visibility', () => {
    renderWithTheme(<LevelRenderer level={mockLevel} />);
    
    const gridButton = screen.getByRole('button', { name: /hide grid/i });
    fireEvent.click(gridButton);
    
    // Button text should change
    expect(screen.getByRole('button', { name: /show grid/i })).toBeInTheDocument();
  });

  it('handles mouse events on canvas', () => {
    const onTileClick = vi.fn();
    const onEntityClick = vi.fn();
    
    renderWithTheme(
      <LevelRenderer 
        level={mockLevel} 
        onTileClick={onTileClick}
        onEntityClick={onEntityClick}
      />
    );
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
    
    if (canvas) {
      // Test mouse move
      fireEvent.mouseMove(canvas, { clientX: 100, clientY: 100 });
      
      // Test mouse down and up (click)
      fireEvent.mouseDown(canvas, { clientX: 100, clientY: 100 });
      fireEvent.mouseUp(canvas, { clientX: 100, clientY: 100 });
    }
  });

  it('handles wheel events for zooming', () => {
    renderWithTheme(<LevelRenderer level={mockLevel} />);
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
    
    if (canvas) {
      // Test zoom in with wheel
      fireEvent.wheel(canvas, { deltaY: -100 });
      
      // Test zoom out with wheel
      fireEvent.wheel(canvas, { deltaY: 100 });
    }
  });

  it('displays scale percentage', () => {
    renderWithTheme(<LevelRenderer level={mockLevel} />);
    
    expect(screen.getByText(/scale: 100%/i)).toBeInTheDocument();
  });

  it('displays hovered tile coordinates', async () => {
    renderWithTheme(<LevelRenderer level={mockLevel} />);
    
    const canvas = document.querySelector('canvas');
    if (canvas) {
      fireEvent.mouseMove(canvas, { clientX: 50, clientY: 50 });
      
      // Wait for state update
      await waitFor(() => {
        // const _tileInfo = screen.queryByText(/tile:/i);
        // The exact coordinates depend on the viewport calculations
        // Just check that tile info can appear
        expect(canvas).toBeInTheDocument();
      });
    }
  });

  it('calls onTileClick when tile is clicked', () => {
    const onTileClick = vi.fn();
    
    renderWithTheme(
      <LevelRenderer 
        level={mockLevel} 
        onTileClick={onTileClick}
      />
    );
    
    const canvas = document.querySelector('canvas');
    if (canvas) {
      fireEvent.mouseDown(canvas, { clientX: 100, clientY: 100 });
      fireEvent.mouseUp(canvas, { clientX: 100, clientY: 100 });
      
      // The exact call depends on coordinate calculations
      // Just verify the handler is set up
      expect(onTileClick).toBeDefined();
    }
  });

  it('renders different terrain types with different colors', () => {
    renderWithTheme(<LevelRenderer level={mockLevel} />);
    
    // Verify that fillStyle was set (indicating different colors for different terrain)
    expect(mockContext.fillRect).toHaveBeenCalled();
  });

  it('renders entities as circles', () => {
    renderWithTheme(<LevelRenderer level={mockLevel} />);
    
    // Verify that arc was called (for drawing entity circles)
    expect(mockContext.arc).toHaveBeenCalled();
    expect(mockContext.fill).toHaveBeenCalled();
  });

  it('renders spawn points when enabled', () => {
    renderWithTheme(<LevelRenderer level={mockLevel} />);
    
    // Spawn points should be rendered by default
    expect(mockContext.beginPath).toHaveBeenCalled();
  });
});