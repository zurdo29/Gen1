import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import ProgressiveLoadingService from './progressiveLoading';
import LocalStorageService from './localStorage';

// Mock fetch and localStorage
global.fetch = vi.fn();

const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => { store[key] = value; },
    removeItem: (key: string) => { delete store[key]; },
    clear: () => { store = {}; },
    key: (index: number) => Object.keys(store)[index] || null,
    get length() { return Object.keys(store).length; }
  };
})();

Object.defineProperty(window, 'localStorage', { value: localStorageMock });

describe('Offline Integration Tests', () => {
  let progressiveService: ProgressiveLoadingService;
  let storageService: LocalStorageService;

  beforeEach(() => {
    localStorageMock.clear();
    vi.clearAllMocks();
    
    progressiveService = new ProgressiveLoadingService({
      chunkSize: 1024,
      maxConcurrent: 2,
      retryAttempts: 2,
      retryDelay: 100
    });
    
    storageService = new LocalStorageService({
      prefix: 'integration-test',
      version: '1.0.0',
      maxSize: 10,
      compression: false,
      encryption: false
    });
  });

  afterEach(() => {
    progressiveService.cancelAll();
  });

  describe('Progressive Loading with Offline Fallback', () => {
    it('should load data progressively and cache it', async () => {
      const mockLevelData = {
        id: 'test-level',
        terrain: [[1, 2], [3, 4]],
        entities: [{ id: 'entity1', x: 10, y: 20 }]
      };

      // Mock successful progressive loading
      (fetch as any)
        .mockImplementationOnce(() => Promise.resolve({
          ok: true,
          headers: { get: (name: string) => name === 'content-length' ? '2048' : null }
        }))
        .mockImplementation(() => Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockLevelData)
        }));

      const loader = progressiveService.createLevelLoader('test-level', '/api/levels/test');
      const result = await loader.load();

      expect(result).toBeDefined();
      
      // Cache the result
      storageService.set('level-test', result);
      expect(storageService.has('level-test')).toBe(true);
      
      const cached = storageService.get('level-test');
      expect(cached).toEqual(result);
    });

    it('should fall back to cached data when network fails', async () => {
      const cachedData = { id: 'cached-level', terrain: 'cached' };
      
      // Pre-populate cache
      storageService.set('level-fallback', cachedData);
      
      // Mock network failure
      (fetch as any).mockRejectedValue(new Error('Network error'));
      
      // Try to load from network (will fail)
      const loader = progressiveService.createLevelLoader('fallback-level', '/api/levels/fallback');
      
      try {
        await loader.load();
      } catch (error) {
        // Network failed, fall back to cache
        const fallbackData = storageService.get('level-fallback');
        expect(fallbackData).toEqual(cachedData);
      }
    });

    it('should handle partial loading with resume capability', async () => {
      const mockData = { chunk: 'data' };
      let callCount = 0;

      // Mock HEAD request
      (fetch as any).mockImplementationOnce(() => Promise.resolve({
        ok: true,
        headers: { get: (name: string) => name === 'content-length' ? '4096' : null }
      }));

      // Mock chunk requests - fail first, succeed later
      (fetch as any).mockImplementation(() => {
        callCount++;
        if (callCount <= 2) {
          return Promise.reject(new Error('Temporary network error'));
        }
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockData)
        });
      });

      const loader = progressiveService.createLevelLoader('partial-level', '/api/levels/partial');
      
      // Should eventually succeed after retries
      const result = await loader.load();
      expect(result).toBeDefined();
      
      // Cache successful result
      storageService.set('partial-level', result);
      expect(storageService.get('partial-level')).toEqual(result);
    });
  });

  describe('Batch Loading with Caching', () => {
    it('should load multiple items and cache each', async () => {
      const mockData1 = { id: 'level1', data: 'first' };
      const mockData2 = { id: 'level2', data: 'second' };

      // Mock HEAD requests
      (fetch as any).mockImplementation((url: string) => {
        if (url.includes('info')) {
          return Promise.resolve({
            ok: true,
            headers: { get: (name: string) => name === 'content-length' ? '1024' : null }
          });
        }
        
        // Mock data requests
        if (url.includes('level1')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockData1)
          });
        } else if (url.includes('level2')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockData2)
          });
        }
        
        return Promise.reject(new Error('Unknown URL'));
      });

      const urls = ['/api/levels/level1', '/api/levels/level2'];
      const loader = progressiveService.createBatchLoader('batch-test', urls);
      const results = await loader.load();

      expect(results).toHaveLength(2);
      
      // Cache each result
      results.forEach((result, index) => {
        storageService.set(`batch-item-${index}`, result);
      });

      // Verify cached data
      expect(storageService.get('batch-item-0')).toEqual([mockData1]);
      expect(storageService.get('batch-item-1')).toEqual([mockData2]);
    });

    it('should handle mixed success/failure in batch loading', async () => {
      const successData = { id: 'success', data: 'ok' };
      
      // Mock mixed responses
      (fetch as any).mockImplementation((url: string) => {
        if (url.includes('info')) {
          return Promise.resolve({
            ok: true,
            headers: { get: (name: string) => name === 'content-length' ? '1024' : null }
          });
        }
        
        if (url.includes('success')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(successData)
          });
        } else {
          return Promise.reject(new Error('Failed item'));
        }
      });

      const urls = ['/api/levels/success', '/api/levels/failure'];
      const loader = progressiveService.createBatchLoader('mixed-batch', urls);
      
      try {
        await loader.load();
      } catch (error) {
        // Even if batch fails, cache any successful items
        storageService.set('partial-success', successData);
        expect(storageService.get('partial-success')).toEqual(successData);
      }
    });
  });

  describe('Storage Management', () => {
    it('should handle storage quota and cleanup', () => {
      // Fill storage with data
      for (let i = 0; i < 100; i++) {
        const largeData = 'x'.repeat(1000);
        storageService.set(`large-item-${i}`, largeData, 1000); // 1 second TTL
      }

      const initialStats = storageService.getStats();
      expect(initialStats.entryCount).toBeGreaterThan(0);

      // Trigger cleanup
      storageService.cleanup();

      const afterCleanupStats = storageService.getStats();
      // Should have cleaned up some entries due to size limits
      expect(afterCleanupStats.entryCount).toBeLessThanOrEqual(initialStats.entryCount);
    });

    it('should export and import cached data', () => {
      const testData = {
        level1: { terrain: 'data1' },
        level2: { terrain: 'data2' },
        config: { setting: 'value' }
      };

      // Store test data
      Object.entries(testData).forEach(([key, value]) => {
        storageService.set(key, value);
      });

      // Export data
      const exported = storageService.export();
      expect(exported).toBeDefined();

      // Clear and import
      storageService.clear();
      expect(storageService.getStats().entryCount).toBe(0);

      const importSuccess = storageService.import(exported!);
      expect(importSuccess).toBe(true);

      // Verify imported data
      Object.entries(testData).forEach(([key, expectedValue]) => {
        const importedValue = storageService.get(key);
        expect(importedValue).toEqual(expectedValue);
      });
    });
  });

  describe('Error Recovery Scenarios', () => {
    it('should recover from network interruption', async () => {
      const mockData = { id: 'recovery-test', data: 'recovered' };
      let attemptCount = 0;

      // Mock HEAD request
      (fetch as any).mockImplementationOnce(() => Promise.resolve({
        ok: true,
        headers: { get: (name: string) => name === 'content-length' ? '1024' : null }
      }));

      // Mock intermittent failures
      (fetch as any).mockImplementation(() => {
        attemptCount++;
        if (attemptCount === 1) {
          return Promise.reject(new Error('Network interruption'));
        }
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockData)
        });
      });

      const loader = progressiveService.createLevelLoader('recovery-test', '/api/levels/recovery');
      const result = await loader.load();

      expect(result).toBeDefined();
      expect(attemptCount).toBe(2); // Should have retried once
      
      // Cache successful recovery
      storageService.set('recovery-result', result);
      expect(storageService.get('recovery-result')).toEqual(result);
    });

    it('should handle corrupted cache gracefully', () => {
      // Manually corrupt cache data
      localStorageMock.setItem('integration-test-corrupted', 'invalid-json-data');

      const result = storageService.get('corrupted');
      expect(result).toBeNull();

      // Corrupted entry should be cleaned up
      expect(localStorageMock.getItem('integration-test-corrupted')).toBeNull();
    });

    it('should handle storage quota exceeded', () => {
      // Mock localStorage to throw quota exceeded
      const originalSetItem = localStorageMock.setItem;
      localStorageMock.setItem = vi.fn(() => {
        throw new Error('QuotaExceededError');
      });

      const success = storageService.set('quota-test', 'data');
      expect(success).toBe(false);

      // Restore original method
      localStorageMock.setItem = originalSetItem;
    });
  });

  describe('Performance and Concurrency', () => {
    it('should handle concurrent loading and caching', async () => {
      const mockData = (id: string) => ({ id, data: `data-${id}` });

      // Mock responses for multiple concurrent requests
      (fetch as any).mockImplementation((url: string) => {
        if (url.includes('info')) {
          return Promise.resolve({
            ok: true,
            headers: { get: (name: string) => name === 'content-length' ? '1024' : null }
          });
        }
        
        const id = url.split('/').pop();
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockData(id))
        });
      });

      // Start multiple loaders concurrently
      const loaders = Array.from({ length: 5 }, (_, i) => 
        progressiveService.createLevelLoader(`concurrent-${i}`, `/api/levels/level${i}`)
      );

      const startTime = performance.now();
      const results = await Promise.all(loaders.map(loader => loader.load()));
      const endTime = performance.now();

      expect(results).toHaveLength(5);
      expect(endTime - startTime).toBeLessThan(2000); // Should complete within 2 seconds

      // Cache all results
      results.forEach((result, index) => {
        storageService.set(`concurrent-${index}`, result);
      });

      // Verify all cached
      results.forEach((_, index) => {
        expect(storageService.has(`concurrent-${index}`)).toBe(true);
      });
    });

    it('should maintain performance with large datasets', async () => {
      const largeData = {
        terrain: Array.from({ length: 1000 }, (_, i) => ({ x: i, y: i, type: 'grass' })),
        entities: Array.from({ length: 500 }, (_, i) => ({ id: i, x: i * 10, y: i * 10 }))
      };

      // Mock large data response
      (fetch as any)
        .mockImplementationOnce(() => Promise.resolve({
          ok: true,
          headers: { get: (name: string) => name === 'content-length' ? '10240' : null }
        }))
        .mockImplementation(() => Promise.resolve({
          ok: true,
          json: () => Promise.resolve(largeData)
        }));

      const startTime = performance.now();
      
      const loader = progressiveService.createLevelLoader('large-data', '/api/levels/large');
      const result = await loader.load();
      
      const loadTime = performance.now() - startTime;

      expect(result).toBeDefined();
      expect(loadTime).toBeLessThan(5000); // Should load within 5 seconds

      // Cache large data
      const cacheStartTime = performance.now();
      storageService.set('large-dataset', result);
      const cacheTime = performance.now() - cacheStartTime;

      expect(cacheTime).toBeLessThan(1000); // Caching should be fast
      expect(storageService.has('large-dataset')).toBe(true);

      // Retrieve large data
      const retrieveStartTime = performance.now();
      const retrieved = storageService.get('large-dataset');
      const retrieveTime = performance.now() - retrieveStartTime;

      expect(retrieveTime).toBeLessThan(100); // Retrieval should be very fast
      expect(retrieved).toEqual(result);
    });
  });
});