// Core application types based on design document

export interface Position {
  x: number;
  y: number;
}

// Updated to match backend C# models
export interface GenerationConfig {
  width: number;
  height: number;
  seed: number;
  generationAlgorithm: string;
  algorithmParameters: Record<string, any>;
  terrainTypes: string[];
  entities: EntityConfig[];
  visualTheme: VisualThemeConfig;
  gameplay: GameplayConfig;
}

export interface EntityConfig {
  type: EntityType;
  count: number;
  minDistance: number;
  maxDistanceFromPlayer: number;
  properties: Record<string, any>;
  placementStrategy: string;
}

export interface VisualThemeConfig {
  themeName: string;
  colorPalette: Record<string, string>;
  tileSprites: Record<string, string>;
  entitySprites: Record<string, string>;
  effectSettings: Record<string, any>;
}

export interface GameplayConfig {
  playerSpeed: number;
  playerHealth: number;
  difficulty: string;
  timeLimit: number;
  victoryConditions: string[];
  mechanics: Record<string, any>;
}

export type EntityType = 
  | 'Player' 
  | 'Enemy' 
  | 'Item' 
  | 'PowerUp' 
  | 'NPC' 
  | 'Exit' 
  | 'Checkpoint' 
  | 'Obstacle' 
  | 'Trigger';

export interface Level {
  id: string;
  config: GenerationConfig;
  terrain: TileMap;
  entities: Entity[];
  metadata: LevelMetadata;
}

export interface TileMap {
  width: number;
  height: number;
  tiles: Tile[][];
}

export interface Tile {
  type: string;
  properties: Record<string, any>;
}

export interface Entity {
  id: string;
  type: string;
  position: Position;
  properties: Record<string, any>;
}

export interface LevelMetadata {
  generatedAt: Date;
  generationTime: number;
  version: string;
}

export interface ProjectConfig {
  id: string;
  name: string;
  generationConfig: GenerationConfig;
  manualEdits: ManualEdit[];
  createdAt: Date;
  lastModified: Date;
}

export interface ManualEdit {
  id: string;
  type: 'terrain' | 'entity';
  position: Position;
  originalValue: any;
  newValue: any;
  timestamp: Date;
}

export interface ConfigPreset {
  id: string;
  name: string;
  description: string;
  config: GenerationConfig;
  createdAt: Date;
}

export interface ShareResult {
  shareId: string;
  shareUrl: string;
  expiresAt: Date;
  description?: string;
  qrCodeDataUrl?: string;
  previewImageUrl?: string;
  thumbnailUrl?: string;
  metadata: Record<string, any>;
}

export interface ExportFormat {
  id: string;
  name: string;
  description: string;
  fileExtension: string;
  supportsCustomization: boolean;
}

export interface ExportOptions {
  format: string;
  includeMetadata: boolean;
  customSettings: Record<string, any>;
}

export interface ValidationResult {
  isValid: boolean;
  errors: ValidationError[];
  warnings: ValidationWarning[];
}

export interface ValidationError {
  field: string;
  message: string;
  code: string;
}

export interface ValidationWarning {
  field: string;
  message: string;
  suggestion?: string;
}

export interface JobStatus {
  jobId: string;
  status: 'pending' | 'running' | 'completed' | 'failed';
  progress: number; // 0-100
  errorMessage?: string;
  result?: any;
}

export interface BatchGenerationRequest {
  baseConfig: GenerationConfig;
  variations: ConfigVariation[];
  count: number;
}

export interface ConfigVariation {
  parameter: string;
  values: any[];
}

export interface Notification {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message: string;
  timestamp: Date;
  autoHide?: boolean;
}

export interface AppState {
  currentProject: ProjectConfig | null;
  isGenerating: boolean;
  notifications: Notification[];
}

// Real-time preview types
export interface PreviewRequestResponse {
  sessionId: string;
  status: string;
  message: string;
}

export interface PreviewStatus {
  sessionId: string;
  status: 'idle' | 'pending' | 'generating' | 'completed' | 'error' | 'cancelled';
  progress: number;
  message?: string;
  lastUpdated?: Date;
  lastConfig?: GenerationConfig;
  lastResult?: Level;
  errorMessage?: string;
}