// Progressive loading service for large level data
import { Level, _GenerationConfig } from '../types';

interface LoadingChunk {
  id: string;
  data: any;
  size: number;
  priority: number;
}

interface ProgressiveLoadingOptions {
  chunkSize?: number;
  maxConcurrentChunks?: number;
  priorityThreshold?: number;
  onProgress?: (loaded: number, total: number) => void;
  onChunkLoaded?: (chunk: LoadingChunk) => void;
  onError?: (error: Error) => void;
}

interface LevelChunk {
  x: number;
  y: number;
  width: number;
  height: number;
  terrain: any[];
  entities: any[];
}

class ProgressiveLoader {
  private loadingQueue: LoadingChunk[] = [];
  private loadedChunks = new Map<string, LoadingChunk>();
  private activeLoads = new Set<string>();
  private options: Required<ProgressiveLoadingOptions>;

  constructor(options: ProgressiveLoadingOptions = {}) {
    this.options = {
      chunkSize: 32, // 32x32 tiles per chunk
      maxConcurrentChunks: 4,
      priorityThreshold: 0.5,
      onProgress: () => {
        // Default progress handler
      },
      onChunkLoaded: () => {
        // Default chunk loaded handler
      },
      onError: () => {
        // Default error handler
      },
      ...options
    };
  }

  async loadLevelProgressively(
    level: Level,
    viewportBounds?: { x: number; y: number; width: number; height: number }
  ): Promise<Level> {
    try {
      // Split level into chunks
      const chunks = this.splitLevelIntoChunks(level);
      
      // Prioritize chunks based on viewport
      const prioritizedChunks = this.prioritizeChunks(chunks, viewportBounds);
      
      // Load chunks progressively
      const loadedLevel = await this.loadChunksProgressively(prioritizedChunks, level);
      
      return loadedLevel;
    } catch (error) {
      this.options.onError(error instanceof Error ? error : new Error('Progressive loading failed'));
      throw error;
    }
  }

  private splitLevelIntoChunks(level: Level): LevelChunk[] {
    const chunks: LevelChunk[] = [];
    const { chunkSize } = this.options;
    
    // Assuming level has width/height properties
    const levelWidth = level.width || 100;
    const levelHeight = level.height || 100;
    
    for (let x = 0; x < levelWidth; x += chunkSize) {
      for (let y = 0; y < levelHeight; y += chunkSize) {
        const chunkWidth = Math.min(chunkSize, levelWidth - x);
        const chunkHeight = Math.min(chunkSize, levelHeight - y);
        
        chunks.push({
          x,
          y,
          width: chunkWidth,
          height: chunkHeight,
          terrain: this.extractTerrainChunk(level, x, y, chunkWidth, chunkHeight),
          entities: this.extractEntitiesChunk(level, x, y, chunkWidth, chunkHeight)
        });
      }
    }
    
    return chunks;
  }

  private extractTerrainChunk(level: Level, x: number, y: number, width: number, height: number): any[] {
    // Extract terrain data for the specified chunk
    const chunk = [];
    
    for (let cy = y; cy < y + height; cy++) {
      for (let cx = x; cx < x + width; cx++) {
        // Assuming level.terrain is a 2D array or similar structure
        if (level.terrain && level.terrain[cy] && level.terrain[cy][cx]) {
          chunk.push({
            x: cx,
            y: cy,
            type: level.terrain[cy][cx]
          });
        }
      }
    }
    
    return chunk;
  }

  private extractEntitiesChunk(level: Level, x: number, y: number, width: number, height: number): any[] {
    // Extract entities within the chunk bounds
    if (!level.entities) return [];
    
    return level.entities.filter(entity => 
      entity.x >= x && entity.x < x + width &&
      entity.y >= y && entity.y < y + height
    );
  }

  private prioritizeChunks(chunks: LevelChunk[], viewportBounds?: { x: number; y: number; width: number; height: number }): LoadingChunk[] {
    return chunks.map((chunk, _index) => {
      let priority = 1;
      
      if (viewportBounds) {
        // Calculate distance from viewport center
        const chunkCenterX = chunk.x + chunk.width / 2;
        const chunkCenterY = chunk.y + chunk.height / 2;
        const viewportCenterX = viewportBounds.x + viewportBounds.width / 2;
        const viewportCenterY = viewportBounds.y + viewportBounds.height / 2;
        
        const distance = Math.sqrt(
          Math.pow(chunkCenterX - viewportCenterX, 2) +
          Math.pow(chunkCenterY - viewportCenterY, 2)
        );
        
        // Higher priority for chunks closer to viewport
        priority = Math.max(0.1, 1 - (distance / 1000));
        
        // Boost priority if chunk intersects with viewport
        if (this.chunksIntersect(chunk, viewportBounds)) {
          priority *= 2;
        }
      }
      
      return {
        id: `chunk-${chunk.x}-${chunk.y}`,
        data: chunk,
        size: chunk.terrain.length + chunk.entities.length,
        priority
      };
    }).sort((a, b) => b.priority - a.priority);
  }

  private chunksIntersect(chunk: LevelChunk, bounds: { x: number; y: number; width: number; height: number }): boolean {
    return !(
      chunk.x + chunk.width < bounds.x ||
      bounds.x + bounds.width < chunk.x ||
      chunk.y + chunk.height < bounds.y ||
      bounds.y + bounds.height < chunk.y
    );
  }

  private async loadChunksProgressively(chunks: LoadingChunk[], originalLevel: Level): Promise<Level> {
    const totalChunks = chunks.length;
    let loadedChunks = 0;
    
    // Initialize result level
    const result: Level = {
      ...originalLevel,
      terrain: [],
      entities: [],
      isProgressivelyLoaded: true
    };
    
    // Load high-priority chunks first
    const highPriorityChunks = chunks.filter(chunk => chunk.priority >= this.options.priorityThreshold);
    const lowPriorityChunks = chunks.filter(chunk => chunk.priority < this.options.priorityThreshold);
    
    // Load high-priority chunks immediately
    for (const chunk of highPriorityChunks.slice(0, this.options.maxConcurrentChunks)) {
      await this.loadChunk(chunk, result);
      loadedChunks++;
      this.options.onProgress(loadedChunks, totalChunks);
    }
    
    // Load remaining chunks in background
    this.loadRemainingChunksInBackground([
      ...highPriorityChunks.slice(this.options.maxConcurrentChunks),
      ...lowPriorityChunks
    ], result, loadedChunks, totalChunks);
    
    return result;
  }

  private async loadChunk(chunk: LoadingChunk, level: Level): Promise<void> {
    if (this.activeLoads.has(chunk.id) || this.loadedChunks.has(chunk.id)) {
      return;
    }
    
    this.activeLoads.add(chunk.id);
    
    try {
      // Simulate loading delay for large chunks
      if (chunk.size > 100) {
        await new Promise(resolve => setTimeout(resolve, 50));
      }
      
      const chunkData = chunk.data as LevelChunk;
      
      // Merge chunk data into level
      if (!level.terrain) level.terrain = [];
      if (!level.entities) level.entities = [];
      
      // Add terrain data
      chunkData.terrain.forEach(tile => {
        if (!level.terrain![tile.y]) level.terrain![tile.y] = [];
        level.terrain![tile.y][tile.x] = tile.type;
      });
      
      // Add entity data
      level.entities!.push(...chunkData.entities);
      
      this.loadedChunks.set(chunk.id, chunk);
      this.options.onChunkLoaded(chunk);
      
    } finally {
      this.activeLoads.delete(chunk.id);
    }
  }

  private loadRemainingChunksInBackground(
    remainingChunks: LoadingChunk[],
    level: Level,
    initialLoadedCount: number,
    totalChunks: number
  ): void {
    let loadedCount = initialLoadedCount;
    
    const loadNext = async () => {
      if (remainingChunks.length === 0) return;
      
      const chunk = remainingChunks.shift()!;
      
      try {
        await this.loadChunk(chunk, level);
        loadedCount++;
        this.options.onProgress(loadedCount, totalChunks);
        
        // Continue loading if we have capacity
        if (this.activeLoads.size < this.options.maxConcurrentChunks) {
          setTimeout(loadNext, 10); // Small delay to prevent blocking
        }
      } catch (error) {
        this.options.onError(error instanceof Error ? error : new Error('Chunk loading failed'));
      }
    };
    
    // Start multiple concurrent loads
    for (let i = 0; i < Math.min(this.options.maxConcurrentChunks, remainingChunks.length); i++) {
      setTimeout(loadNext, i * 10);
    }
  }

  // Update viewport to prioritize loading of visible chunks
  updateViewport(bounds: { x: number; y: number; width: number; height: number }): void {
    // Re-prioritize remaining chunks based on new viewport
    const remainingChunks = Array.from(this.loadingQueue.values())
      .filter(chunk => !this.loadedChunks.has(chunk.id) && !this.activeLoads.has(chunk.id));
    
    if (remainingChunks.length > 0) {
      const reprioritized = this.prioritizeChunks(
        remainingChunks.map(chunk => chunk.data),
        bounds
      );
      
      // Update loading queue
      this.loadingQueue = reprioritized;
    }
  }

  // Get loading statistics
  getLoadingStats(): { loaded: number; total: number; active: number } {
    return {
      loaded: this.loadedChunks.size,
      total: this.loadingQueue.length + this.loadedChunks.size,
      active: this.activeLoads.size
    };
  }

  // Clear loaded chunks to free memory
  clearLoadedChunks(): void {
    this.loadedChunks.clear();
    this.activeLoads.clear();
    this.loadingQueue = [];
  }
}

// React hook for progressive loading
export const useProgressiveLoading = (options: ProgressiveLoadingOptions = {}) => {
  const [loader] = React.useState(() => new ProgressiveLoader(options));
  const [loadingProgress, setLoadingProgress] = React.useState({ loaded: 0, total: 0 });
  const [isLoading, setIsLoading] = React.useState(false);
  const [error, setError] = React.useState<Error | null>(null);

  const loadLevel = React.useCallback(async (
    level: Level,
    viewportBounds?: { x: number; y: number; width: number; height: number }
  ) => {
    setIsLoading(true);
    setError(null);
    
    try {
      const progressiveLoader = new ProgressiveLoader({
        ...options,
        onProgress: (loaded, total) => {
          setLoadingProgress({ loaded, total });
          options.onProgress?.(loaded, total);
        },
        onError: (err) => {
          setError(err);
          options.onError?.(err);
        }
      });
      
      const result = await progressiveLoader.loadLevelProgressively(level, viewportBounds);
      return result;
    } finally {
      setIsLoading(false);
    }
  }, [options]);

  const updateViewport = React.useCallback((bounds: { x: number; y: number; width: number; height: number }) => {
    loader.updateViewport(bounds);
  }, [loader]);

  const getStats = React.useCallback(() => {
    return loader.getLoadingStats();
  }, [loader]);

  const clearCache = React.useCallback(() => {
    loader.clearLoadedChunks();
  }, [loader]);

  return {
    loadLevel,
    updateViewport,
    getStats,
    clearCache,
    loadingProgress,
    isLoading,
    error
  };
};

export default ProgressiveLoader;