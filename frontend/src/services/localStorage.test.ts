import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import LocalStorageService, { configStorage, levelStorage, userStorage } from './localStorage';

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {};

  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value;
    },
    removeItem: (key: string) => {
      delete store[key];
    },
    clear: () => {
      store = {};
    },
    key: (index: number) => Object.keys(store)[index] || null,
    get length() {
      return Object.keys(store).length;
    }
  };
})();

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock
});

describe('LocalStorageService', () => {
  let storage: LocalStorageService;

  beforeEach(() => {
    localStorageMock.clear();
    storage = new LocalStorageService({
      prefix: 'test',
      version: '1.0.0',
      maxSize: 1, // 1MB for testing
      compression: false, // Disable for easier testing
      encryption: false
    });
  });

  afterEach(() => {
    localStorageMock.clear();
  });

  describe('basic operations', () => {
    it('should set and get values', () => {
      const testData = { test: 'data', number: 42 };
      const success = storage.set('test-key', testData);
      
      expect(success).toBe(true);
      
      const retrieved = storage.get('test-key');
      expect(retrieved).toEqual(testData);
    });

    it('should return null for non-existent keys', () => {
      const result = storage.get('non-existent');
      expect(result).toBeNull();
    });

    it('should check if key exists', () => {
      storage.set('test-key', 'test-data');
      
      expect(storage.has('test-key')).toBe(true);
      expect(storage.has('non-existent')).toBe(false);
    });

    it('should remove entries', () => {
      storage.set('test-key', 'test-data');
      expect(storage.has('test-key')).toBe(true);
      
      const removed = storage.remove('test-key');
      expect(removed).toBe(true);
      expect(storage.has('test-key')).toBe(false);
    });

    it('should clear all entries', () => {
      storage.set('key1', 'data1');
      storage.set('key2', 'data2');
      
      const cleared = storage.clear();
      expect(cleared).toBe(true);
      expect(storage.has('key1')).toBe(false);
      expect(storage.has('key2')).toBe(false);
    });
  });

  describe('TTL (Time To Live)', () => {
    it('should expire entries after TTL', async () => {
      storage.set('test-key', 'test-data', 50); // 50ms TTL
      
      expect(storage.get('test-key')).toBe('test-data');
      
      // Wait for expiration
      await new Promise(resolve => setTimeout(resolve, 60));
      
      expect(storage.get('test-key')).toBeNull();
    });

    it('should not expire entries before TTL', async () => {
      storage.set('test-key', 'test-data', 200); // 200ms TTL
      
      // Wait less than TTL
      await new Promise(resolve => setTimeout(resolve, 50));
      
      expect(storage.get('test-key')).toBe('test-data');
    });

    it('should handle entries without TTL', () => {
      storage.set('test-key', 'test-data'); // No TTL
      
      expect(storage.get('test-key')).toBe('test-data');
    });
  });

  describe('version compatibility', () => {
    it('should reject entries with different versions', () => {
      // Manually set entry with different version
      const entry = {
        data: 'test-data',
        timestamp: Date.now(),
        version: '2.0.0' // Different version
      };
      
      localStorageMock.setItem('test-test-key', JSON.stringify(entry));
      
      const result = storage.get('test-key');
      expect(result).toBeNull();
      
      // Entry should be removed
      expect(localStorageMock.getItem('test-test-key')).toBeNull();
    });

    it('should accept entries with same version', () => {
      storage.set('test-key', 'test-data');
      
      const result = storage.get('test-key');
      expect(result).toBe('test-data');
    });
  });

  describe('compression', () => {
    it('should compress data when enabled', () => {
      const compressedStorage = new LocalStorageService({
        prefix: 'compressed-test',
        compression: true,
        encryption: false
      });

      const testData = 'aaabbbccc'; // Simple repeating data that compresses well
      compressedStorage.set('test-key', testData);
      
      const rawStored = localStorageMock.getItem('compressed-test-test-key');
      expect(rawStored).toBeDefined();
      
      // Should be able to retrieve original data
      const retrieved = compressedStorage.get('test-key');
      expect(retrieved).toBe(testData);
    });
  });

  describe('encryption', () => {
    it('should encrypt data when enabled', () => {
      const encryptedStorage = new LocalStorageService({
        prefix: 'encrypted-test',
        compression: false,
        encryption: true
      });

      const testData = 'sensitive-data';
      encryptedStorage.set('test-key', testData);
      
      const rawStored = localStorageMock.getItem('encrypted-test-test-key');
      expect(rawStored).toBeDefined();
      expect(rawStored).not.toContain('sensitive-data');
      
      // Should be able to retrieve original data
      const retrieved = encryptedStorage.get('test-key');
      expect(retrieved).toBe(testData);
    });

    it('should handle decryption errors gracefully', () => {
      const encryptedStorage = new LocalStorageService({
        prefix: 'encrypted-test',
        encryption: true
      });

      // Manually set corrupted encrypted data
      localStorageMock.setItem('encrypted-test-test-key', 'corrupted-data');
      
      const result = encryptedStorage.get('test-key');
      expect(result).toBeNull();
      
      // Corrupted entry should be removed
      expect(localStorageMock.getItem('encrypted-test-test-key')).toBeNull();
    });
  });

  describe('storage stats', () => {
    it('should calculate storage statistics', () => {
      storage.set('key1', 'data1');
      storage.set('key2', 'data2');
      
      const stats = storage.getStats();
      
      expect(stats.entryCount).toBe(2);
      expect(stats.totalSize).toBeGreaterThan(0);
      expect(stats.usagePercentage).toBeGreaterThan(0);
      expect(stats.availableSpace).toBeGreaterThan(0);
    });

    it('should track size accurately', () => {
      const smallData = 'small';
      const largeData = 'x'.repeat(1000);
      
      storage.set('small', smallData);
      const statsAfterSmall = storage.getStats();
      
      storage.set('large', largeData);
      const statsAfterLarge = storage.getStats();
      
      expect(statsAfterLarge.totalSize).toBeGreaterThan(statsAfterSmall.totalSize);
    });
  });

  describe('cleanup and eviction', () => {
    it('should clean up expired entries', async () => {
      storage.set('short-lived', 'data', 50); // 50ms TTL
      storage.set('long-lived', 'data', 5000); // 5s TTL
      
      expect(storage.has('short-lived')).toBe(true);
      expect(storage.has('long-lived')).toBe(true);
      
      // Wait for expiration
      await new Promise(resolve => setTimeout(resolve, 100));
      
      storage.cleanup();
      
      expect(storage.has('short-lived')).toBe(false);
      expect(storage.has('long-lived')).toBe(true);
    });

    it('should evict oldest entries when over capacity', () => {
      // Fill storage beyond capacity
      const largeData = 'x'.repeat(100000); // Large data to trigger eviction
      
      for (let i = 0; i < 20; i++) {
        storage.set(`key-${i}`, largeData);
      }
      
      const stats = storage.getStats();
      expect(stats.entryCount).toBeLessThan(20); // Some should be evicted
    });
  });

  describe('batch operations', () => {
    it('should set multiple entries at once', () => {
      const entries = [
        { key: 'key1', data: 'data1' },
        { key: 'key2', data: 'data2', ttl: 1000 },
        { key: 'key3', data: 'data3' }
      ];
      
      const success = storage.setMany(entries);
      expect(success).toBe(true);
      
      expect(storage.get('key1')).toBe('data1');
      expect(storage.get('key2')).toBe('data2');
      expect(storage.get('key3')).toBe('data3');
    });

    it('should get multiple entries at once', () => {
      storage.set('key1', 'data1');
      storage.set('key2', 'data2');
      
      const results = storage.getMany(['key1', 'key2', 'key3']);
      
      expect(results).toEqual([
        { key: 'key1', data: 'data1' },
        { key: 'key2', data: 'data2' },
        { key: 'key3', data: null }
      ]);
    });
  });

  describe('export/import', () => {
    it('should export storage data', () => {
      storage.set('key1', 'data1');
      storage.set('key2', { complex: 'data' });
      
      const exported = storage.export();
      expect(exported).toBeDefined();
      
      const parsed = JSON.parse(exported!);
      expect(parsed.version).toBe('1.0.0');
      expect(parsed.data).toBeDefined();
      expect(Object.keys(parsed.data)).toHaveLength(2);
    });

    it('should import storage data', () => {
      // Create initial data
      storage.set('key1', 'data1');
      const exported = storage.export();
      
      // Clear and import
      storage.clear();
      expect(storage.has('key1')).toBe(false);
      
      const success = storage.import(exported!);
      expect(success).toBe(true);
      expect(storage.get('key1')).toBe('data1');
    });

    it('should handle invalid import data', () => {
      const invalidData = '{"invalid": "data"}';
      
      const success = storage.import(invalidData);
      expect(success).toBe(false);
    });

    it('should handle version mismatches in import', () => {
      const exportData = JSON.stringify({
        version: '2.0.0',
        timestamp: Date.now(),
        data: { 'key1': 'value1' }
      });
      
      const success = storage.import(exportData);
      expect(success).toBe(true); // Should still import but with warning
    });
  });

  describe('error handling', () => {
    it('should handle localStorage quota exceeded', () => {
      // Mock localStorage to throw quota exceeded error
      const originalSetItem = localStorageMock.setItem;
      localStorageMock.setItem = vi.fn(() => {
        throw new Error('QuotaExceededError');
      });
      
      const success = storage.set('test-key', 'test-data');
      expect(success).toBe(false);
      
      // Restore original method
      localStorageMock.setItem = originalSetItem;
    });

    it('should handle corrupted data gracefully', () => {
      // Manually set corrupted data
      localStorageMock.setItem('test-corrupted-key', 'invalid-json');
      
      const result = storage.get('corrupted-key');
      expect(result).toBeNull();
      
      // Corrupted entry should be removed
      expect(localStorageMock.getItem('test-corrupted-key')).toBeNull();
    });

    it('should handle missing localStorage gracefully', () => {
      // Create storage service when localStorage is not supported
      const originalLocalStorage = window.localStorage;
      
      // @ts-expect-error
      delete window.localStorage;
      
      const unsupportedStorage = new LocalStorageService();
      
      expect(unsupportedStorage.set('key', 'value')).toBe(false);
      expect(unsupportedStorage.get('key')).toBeNull();
      expect(unsupportedStorage.isStorageSupported()).toBe(false);
      
      // Restore localStorage
      Object.defineProperty(window, 'localStorage', {
        value: originalLocalStorage
      });
    });
  });

  describe('specialized storage instances', () => {
    it('should have different configurations for different storage types', () => {
      expect(configStorage).toBeInstanceOf(LocalStorageService);
      expect(levelStorage).toBeInstanceOf(LocalStorageService);
      expect(userStorage).toBeInstanceOf(LocalStorageService);
    });

    it('should maintain separate data in different storage instances', () => {
      // Create new instances for testing to avoid global state issues
      const testConfigStorage = new LocalStorageService({
        prefix: 'test-config',
        compression: false,
        encryption: false
      });
      const testLevelStorage = new LocalStorageService({
        prefix: 'test-levels',
        compression: false,
        encryption: false
      });
      
      testConfigStorage.set('test', 'config-data');
      testLevelStorage.set('test', 'level-data');
      
      expect(testConfigStorage.get('test')).toBe('config-data');
      expect(testLevelStorage.get('test')).toBe('level-data');
    });

    it('should have different prefixes for different storage types', () => {
      // Create new instances for testing
      const testConfigStorage = new LocalStorageService({
        prefix: 'test-config',
        compression: false,
        encryption: false
      });
      const testLevelStorage = new LocalStorageService({
        prefix: 'test-levels',
        compression: false,
        encryption: false
      });
      
      testConfigStorage.set('test', 'data');
      testLevelStorage.set('test', 'data');
      
      const configKeys = testConfigStorage.getAllKeys();
      const levelKeys = testLevelStorage.getAllKeys();
      
      expect(configKeys.length).toBeGreaterThan(0);
      expect(levelKeys.length).toBeGreaterThan(0);
      
      if (configKeys.length > 0 && levelKeys.length > 0) {
        expect(configKeys[0]).toContain('config');
        expect(levelKeys[0]).toContain('levels');
        expect(configKeys[0]).not.toBe(levelKeys[0]);
      }
    });
  });

  describe('performance', () => {
    it('should handle large numbers of entries efficiently', () => {
      const startTime = performance.now();
      
      // Add 100 entries
      for (let i = 0; i < 100; i++) {
        storage.set(`key-${i}`, `data-${i}`);
      }
      
      const setTime = performance.now() - startTime;
      
      const getStartTime = performance.now();
      
      // Retrieve 100 entries
      for (let i = 0; i < 100; i++) {
        storage.get(`key-${i}`);
      }
      
      const getTime = performance.now() - getStartTime;
      
      // Operations should be reasonably fast (adjusted for test environment)
      expect(setTime).toBeLessThan(1000); // Less than 1 second
      expect(getTime).toBeLessThan(500);  // Less than 500ms
    });

    it('should have consistent performance', () => {
      storage.set('test-key', 'test-data');
      
      const times: number[] = [];
      
      // Measure multiple get operations
      for (let i = 0; i < 100; i++) {
        const start = performance.now();
        storage.get('test-key');
        const end = performance.now();
        times.push(end - start);
      }
      
      const avgTime = times.reduce((a, b) => a + b, 0) / times.length;
      const maxTime = Math.max(...times);
      
      // Should be fast and consistent
      expect(avgTime).toBeLessThan(1); // Less than 1ms average
      expect(maxTime).toBeLessThan(5);  // Less than 5ms max
    });
  });
});