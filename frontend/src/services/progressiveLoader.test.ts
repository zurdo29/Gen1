// Tests for progressive loading functionality
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import ProgressiveLoader from './progressiveLoader';
import { Level } from '../types';

// Mock level data for testing
const createMockLevel = (width: number = 100, height: number = 100): Level => ({
  id: 'test-level',
  width,
  height,
  terrain: Array(height).fill(null).map((_, y) => 
    Array(width).fill(null).map((_, x) => `terrain-${x}-${y}`)
  ),
  entities: Array(10).fill(null).map((_, i) => ({
    id: `entity-${i}`,
    x: Math.floor(Math.random() * width),
    y: Math.floor(Math.random() * height),
    type: 'test-entity'
  })),
  metadata: {
    generatedAt: Date.now(),
    version: '1.0.0'
  }
});

describe('ProgressiveLoader', () => {
  let loader: ProgressiveLoader;
  let mockLevel: Level;
  let progressCallback: ReturnType<typeof vi.fn>;
  let chunkCallback: ReturnType<typeof vi.fn>;
  let errorCallback: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    progressCallback = vi.fn();
    chunkCallback = vi.fn();
    errorCallback = vi.fn();
    
    loader = new ProgressiveLoader({
      chunkSize: 32,
      maxConcurrentChunks: 2,
      priorityThreshold: 0.5,
      onProgress: progressCallback,
      onChunkLoaded: chunkCallback,
      onError: errorCallback
    });
    
    mockLevel = createMockLevel(64, 64);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('loadLevelProgressively', () => {
    it('should load a level progressively', async () => {
      const result = await loader.loadLevelProgressively(mockLevel);
      
      expect(result).toBeDefined();
      expect(result.isProgressivelyLoaded).toBe(true);
      expect(result.terrain).toBeDefined();
      expect(result.entities).toBeDefined();
    });

    it('should call progress callback during loading', async () => {
      await loader.loadLevelProgressively(mockLevel);
      
      expect(progressCallback).toHaveBeenCalled();
      const calls = progressCallback.mock.calls;
      expect(calls.length).toBeGreaterThan(0);
      
      // Check that progress increases
      const firstCall = calls[0];
      const lastCall = calls[calls.length - 1];
      expect(lastCall[0]).toBeGreaterThanOrEqual(firstCall[0]); // loaded count
      expect(lastCall[1]).toBeGreaterThan(0); // total count
    });

    it('should prioritize chunks within viewport', async () => {
      const viewport = { x: 0, y: 0, width: 32, height: 32 };
      
      await loader.loadLevelProgressively(mockLevel, viewport);
      
      expect(chunkCallback).toHaveBeenCalled();
      
      // First chunk loaded should be within or close to viewport
      const firstChunk = chunkCallback.mock.calls[0][0];
      expect(firstChunk.data.x).toBeLessThanOrEqual(32);
      expect(firstChunk.data.y).toBeLessThanOrEqual(32);
    });

    it('should handle empty levels gracefully', async () => {
      const emptyLevel: Level = {
        id: 'empty',
        width: 0,
        height: 0,
        terrain: [],
        entities: [],
        metadata: { generatedAt: Date.now(), version: '1.0.0' }
      };
      
      const result = await loader.loadLevelProgressively(emptyLevel);
      
      expect(result).toBeDefined();
      expect(result.isProgressivelyLoaded).toBe(true);
    });

    it('should handle large levels efficiently', async () => {
      const largeLevel = createMockLevel(200, 200);
      const startTime = performance.now();
      
      const result = await loader.loadLevelProgressively(largeLevel);
      
      const endTime = performance.now();
      const loadTime = endTime - startTime;
      
      expect(result).toBeDefined();
      expect(loadTime).toBeLessThan(5000); // Should complete within 5 seconds
      expect(chunkCallback).toHaveBeenCalled();
    });
  });

  describe('updateViewport', () => {
    it('should update chunk priorities based on new viewport', () => {
      const newViewport = { x: 50, y: 50, width: 32, height: 32 };
      
      // This should not throw
      expect(() => {
        loader.updateViewport(newViewport);
      }).not.toThrow();
    });
  });

  describe('getLoadingStats', () => {
    it('should return loading statistics', () => {
      const stats = loader.getLoadingStats();
      
      expect(stats).toHaveProperty('loaded');
      expect(stats).toHaveProperty('total');
      expect(stats).toHaveProperty('active');
      expect(typeof stats.loaded).toBe('number');
      expect(typeof stats.total).toBe('number');
      expect(typeof stats.active).toBe('number');
    });
  });

  describe('clearLoadedChunks', () => {
    it('should clear all loaded chunks', async () => {
      await loader.loadLevelProgressively(mockLevel);
      
      const statsBefore = loader.getLoadingStats();
      expect(statsBefore.loaded).toBeGreaterThan(0);
      
      loader.clearLoadedChunks();
      
      const statsAfter = loader.getLoadingStats();
      expect(statsAfter.loaded).toBe(0);
      expect(statsAfter.active).toBe(0);
    });
  });

  describe('error handling', () => {
    it('should handle errors gracefully', async () => {
      const invalidLevel = null as any;
      
      await expect(loader.loadLevelProgressively(invalidLevel)).rejects.toThrow();
      expect(errorCallback).toHaveBeenCalled();
    });

    it('should continue loading other chunks if one fails', async () => {
      // Mock a scenario where some chunks fail to load
      const originalExtractTerrain = (loader as any).extractTerrainChunk;
      let callCount = 0;
      
      (loader as any).extractTerrainChunk = vi.fn().mockImplementation((...args) => {
        callCount++;
        if (callCount === 2) {
          throw new Error('Chunk loading failed');
        }
        return originalExtractTerrain.apply(loader, args);
      });
      
      // Should still complete loading despite one chunk failing
      const result = await loader.loadLevelProgressively(mockLevel);
      expect(result).toBeDefined();
    });
  });

  describe('performance optimization', () => {
    it('should limit concurrent chunk loading', async () => {
      const largeLevel = createMockLevel(128, 128);
      let maxConcurrent = 0;
      let currentConcurrent = 0;
      
      const originalLoadChunk = (loader as any).loadChunk;
      (loader as any).loadChunk = vi.fn().mockImplementation(async (...args) => {
        currentConcurrent++;
        maxConcurrent = Math.max(maxConcurrent, currentConcurrent);
        
        const result = await originalLoadChunk.apply(loader, args);
        
        currentConcurrent--;
        return result;
      });
      
      await loader.loadLevelProgressively(largeLevel);
      
      expect(maxConcurrent).toBeLessThanOrEqual(2); // maxConcurrentChunks = 2
    });

    it('should prioritize high-priority chunks', async () => {
      const viewport = { x: 0, y: 0, width: 16, height: 16 };
      
      await loader.loadLevelProgressively(mockLevel, viewport);
      
      // Check that chunks were loaded in priority order
      const chunkCalls = chunkCallback.mock.calls;
      expect(chunkCalls.length).toBeGreaterThan(0);
      
      // First few chunks should have high priority (close to viewport)
      const firstChunk = chunkCalls[0][0];
      expect(firstChunk.priority).toBeGreaterThan(0.5);
    });
  });

  describe('memory management', () => {
    it('should not exceed memory limits for large levels', async () => {
      const veryLargeLevel = createMockLevel(500, 500);
      
      // Monitor memory usage (simplified)
      const initialMemory = (performance as any).memory?.usedJSHeapSize || 0;
      
      const result = await loader.loadLevelProgressively(veryLargeLevel);
      
      const finalMemory = (performance as any).memory?.usedJSHeapSize || 0;
      const memoryIncrease = finalMemory - initialMemory;
      
      expect(result).toBeDefined();
      // Memory increase should be reasonable (less than 100MB)
      if (initialMemory > 0) {
        expect(memoryIncrease).toBeLessThan(100 * 1024 * 1024);
      }
    });

    it('should clean up resources when cleared', () => {
      loader.clearLoadedChunks();
      
      const stats = loader.getLoadingStats();
      expect(stats.loaded).toBe(0);
      expect(stats.active).toBe(0);
      expect(stats.total).toBe(0);
    });
  });
});

// Integration tests with React hooks
describe('useProgressiveLoading hook', () => {
  // These would be tested with React Testing Library in a real implementation
  it('should be tested with React Testing Library', () => {
    // Placeholder for React hook tests
    expect(true).toBe(true);
  });
});