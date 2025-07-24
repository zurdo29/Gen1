import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import CacheService, { levelCache, configCache, apiCache } from './cache';

describe('CacheService', () => {
  let cache: CacheService;

  beforeEach(() => {
    cache = new CacheService({
      maxSize: 1, // 1MB for testing
      defaultTTL: 1000, // 1 second for testing
      maxEntries: 5,
      cleanupInterval: 100 // 100ms for testing
    });
  });

  afterEach(() => {
    cache.destroy();
  });

  describe('basic operations', () => {
    it('should set and get values', () => {
      const testData = { test: 'data' };
      cache.set('test-key', testData);
      
      const retrieved = cache.get('test-key');
      expect(retrieved).toEqual(testData);
    });

    it('should return null for non-existent keys', () => {
      const result = cache.get('non-existent');
      expect(result).toBeNull();
    });

    it('should check if key exists', () => {
      cache.set('test-key', 'test-data');
      
      expect(cache.has('test-key')).toBe(true);
      expect(cache.has('non-existent')).toBe(false);
    });

    it('should delete entries', () => {
      cache.set('test-key', 'test-data');
      expect(cache.has('test-key')).toBe(true);
      
      const deleted = cache.delete('test-key');
      expect(deleted).toBe(true);
      expect(cache.has('test-key')).toBe(false);
    });

    it('should clear all entries', () => {
      cache.set('key1', 'data1');
      cache.set('key2', 'data2');
      
      cache.clear();
      
      expect(cache.has('key1')).toBe(false);
      expect(cache.has('key2')).toBe(false);
    });
  });

  describe('TTL (Time To Live)', () => {
    it('should expire entries after TTL', async () => {
      cache.set('test-key', 'test-data', 50); // 50ms TTL
      
      expect(cache.get('test-key')).toBe('test-data');
      
      // Wait for expiration
      await new Promise(resolve => setTimeout(resolve, 60));
      
      expect(cache.get('test-key')).toBeNull();
    });

    it('should use default TTL when not specified', async () => {
      cache.set('test-key', 'test-data'); // Uses default 1000ms TTL
      
      expect(cache.get('test-key')).toBe('test-data');
      
      // Should still be valid after 500ms
      await new Promise(resolve => setTimeout(resolve, 500));
      expect(cache.get('test-key')).toBe('test-data');
    });

    it('should update access time on get', () => {
      cache.set('test-key', 'test-data');
      
      // Access the entry
      cache.get('test-key');
      
      const stats = cache.getStats();
      expect(stats.hits).toBe(1);
    });
  });

  describe('array key generation', () => {
    it('should generate consistent keys from arrays', () => {
      const keyParts = ['user', 123, { type: 'config' }];
      
      cache.set(keyParts, 'test-data');
      const retrieved = cache.get(keyParts);
      
      expect(retrieved).toBe('test-data');
    });

    it('should generate different keys for different arrays', () => {
      cache.set(['key', 1], 'data1');
      cache.set(['key', 2], 'data2');
      
      expect(cache.get(['key', 1])).toBe('data1');
      expect(cache.get(['key', 2])).toBe('data2');
    });
  });

  describe('size management', () => {
    it('should track cache size', () => {
      const largeData = 'x'.repeat(1000); // 1KB of data
      cache.set('large-key', largeData);
      
      const stats = cache.getStats();
      expect(stats.totalSize).toBeGreaterThan(0);
    });

    it('should evict entries when over max entries', () => {
      // Add more entries than maxEntries (5)
      for (let i = 0; i < 10; i++) {
        cache.set(`key-${i}`, `data-${i}`);
      }
      
      const stats = cache.getStats();
      expect(stats.entryCount).toBeLessThanOrEqual(5);
    });
  });

  describe('LRU eviction', () => {
    it('should evict least recently used entries', () => {
      // Fill cache to capacity
      for (let i = 0; i < 5; i++) {
        cache.set(`key-${i}`, `data-${i}`);
      }
      
      // Access some entries to make them recently used
      cache.get('key-1');
      cache.get('key-3');
      
      // Add more entries to trigger eviction
      cache.set('new-key-1', 'new-data-1');
      cache.set('new-key-2', 'new-data-2');
      
      // Recently accessed entries should still exist
      expect(cache.get('key-1')).toBe('data-1');
      expect(cache.get('key-3')).toBe('data-3');
    });
  });

  describe('statistics', () => {
    it('should track hits and misses', () => {
      cache.set('test-key', 'test-data');
      
      // Hit
      cache.get('test-key');
      
      // Miss
      cache.get('non-existent');
      
      const stats = cache.getStats();
      expect(stats.hits).toBe(1);
      expect(stats.misses).toBe(1);
      expect(stats.hitRate).toBe(50);
    });

    it('should track access counts', () => {
      cache.set('test-key', 'test-data');
      
      // Access multiple times
      cache.get('test-key');
      cache.get('test-key');
      cache.get('test-key');
      
      const topEntries = cache.getTopEntries('accessCount', 1);
      expect(topEntries[0].accessCount).toBe(3);
    });
  });

  describe('batch operations', () => {
    it('should set multiple entries at once', () => {
      const entries = [
        { key: 'key1', data: 'data1' },
        { key: 'key2', data: 'data2' },
        { key: 'key3', data: 'data3' }
      ];
      
      cache.setMany(entries);
      
      expect(cache.get('key1')).toBe('data1');
      expect(cache.get('key2')).toBe('data2');
      expect(cache.get('key3')).toBe('data3');
    });

    it('should get multiple entries at once', () => {
      cache.set('key1', 'data1');
      cache.set('key2', 'data2');
      
      const results = cache.getMany(['key1', 'key2', 'key3']);
      
      expect(results).toEqual([
        { key: 'key1', data: 'data1' },
        { key: 'key2', data: 'data2' },
        { key: 'key3', data: null }
      ]);
    });
  });

  describe('preload functionality', () => {
    it('should preload data if not cached', async () => {
      const loader = vi.fn().mockResolvedValue('loaded-data');
      
      const result = await cache.preload('test-key', loader);
      
      expect(result).toBe('loaded-data');
      expect(loader).toHaveBeenCalledTimes(1);
      expect(cache.get('test-key')).toBe('loaded-data');
    });

    it('should return cached data without calling loader', async () => {
      cache.set('test-key', 'cached-data');
      const loader = vi.fn().mockResolvedValue('loaded-data');
      
      const result = await cache.preload('test-key', loader);
      
      expect(result).toBe('cached-data');
      expect(loader).not.toHaveBeenCalled();
    });
  });

  describe('export/import', () => {
    it('should export cache data', () => {
      cache.set('key1', 'data1');
      cache.set('key2', 'data2');
      
      const exported = cache.export();
      const data = JSON.parse(exported);
      
      expect(data.entries).toHaveLength(2);
      expect(data.stats).toBeDefined();
      expect(data.config).toBeDefined();
    });

    it('should import cache data', () => {
      // Create initial cache
      cache.set('key1', 'data1');
      const exported = cache.export();
      
      // Clear and import
      cache.clear();
      cache.import(exported);
      
      expect(cache.get('key1')).toBe('data1');
    });

    it('should handle invalid import data', () => {
      const invalidData = '{"invalid": "data"}';
      
      expect(() => cache.import(invalidData)).not.toThrow();
      
      // Cache should remain empty
      const stats = cache.getStats();
      expect(stats.entryCount).toBe(0);
    });
  });

  describe('cleanup and memory management', () => {
    it('should automatically clean up expired entries', async () => {
      cache.set('short-lived', 'data', 50); // 50ms TTL
      cache.set('long-lived', 'data', 5000); // 5s TTL
      
      expect(cache.has('short-lived')).toBe(true);
      expect(cache.has('long-lived')).toBe(true);
      
      // Wait for cleanup
      await new Promise(resolve => setTimeout(resolve, 200));
      
      expect(cache.has('short-lived')).toBe(false);
      expect(cache.has('long-lived')).toBe(true);
    });
  });

  describe('specialized cache instances', () => {
    it('should have different configurations for different cache types', () => {
      expect(levelCache).toBeInstanceOf(CacheService);
      expect(configCache).toBeInstanceOf(CacheService);
      expect(apiCache).toBeInstanceOf(CacheService);
    });

    it('should maintain separate data in different cache instances', () => {
      levelCache.set('test', 'level-data');
      configCache.set('test', 'config-data');
      
      expect(levelCache.get('test')).toBe('level-data');
      expect(configCache.get('test')).toBe('config-data');
    });
  });

  describe('performance characteristics', () => {
    it('should handle large numbers of entries efficiently', () => {
      const startTime = performance.now();
      
      // Add 1000 entries
      for (let i = 0; i < 1000; i++) {
        cache.set(`key-${i}`, `data-${i}`);
      }
      
      const setTime = performance.now() - startTime;
      
      const getStartTime = performance.now();
      
      // Retrieve 1000 entries
      for (let i = 0; i < 1000; i++) {
        cache.get(`key-${i}`);
      }
      
      const getTime = performance.now() - getStartTime;
      
      // Operations should be reasonably fast
      expect(setTime).toBeLessThan(100); // Less than 100ms
      expect(getTime).toBeLessThan(50);  // Less than 50ms
    });

    it('should have consistent performance with cache hits', () => {
      cache.set('test-key', 'test-data');
      
      const times: number[] = [];
      
      // Measure multiple cache hits
      for (let i = 0; i < 100; i++) {
        const start = performance.now();
        cache.get('test-key');
        const end = performance.now();
        times.push(end - start);
      }
      
      const avgTime = times.reduce((a, b) => a + b, 0) / times.length;
      const maxTime = Math.max(...times);
      
      // Cache hits should be very fast and consistent
      expect(avgTime).toBeLessThan(1); // Less than 1ms average
      expect(maxTime).toBeLessThan(5);  // Less than 5ms max
    });
  });
});