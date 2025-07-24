// Level data types for the Canvas renderer

export interface Position {
  x: number;
  y: number;
}

export interface Entity {
  id: string;
  type: string;
  position: Position;
  properties: Record<string, any>;
}

export interface Tile {
  type: string;
  position: Position;
  properties?: Record<string, any>;
}

export interface Level {
  id: string;
  width: number;
  height: number;
  tiles: Tile[][];
  entities: Entity[];
  spawnPoints: Position[];
  metadata: {
    seed: number;
    generationAlgorithm: string;
    createdAt: Date;
  };
}

export interface ViewportState {
  offsetX: number;
  offsetY: number;
  scale: number;
  tileSize: number;
}

export interface RenderOptions {
  showGrid: boolean;
  showCoordinates: boolean;
  showSpawnPoints: boolean;
  highlightHoveredTile: boolean;
}

export interface CanvasInteractionEvent {
  type: 'click' | 'hover' | 'drag';
  position: Position;
  tilePosition: Position;
  entity?: Entity;
}