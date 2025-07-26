// Advanced caching service for performance optimization
import React from 'react';

interface CacheEntry<T> {
  data: T;
  timestamp: number;
  ttl: number;
  accessCount: number;
  lastAccessed: number;
  size?: number;
}

interface CacheStats {
  hits: number;
  misses: number;
  evictions: number;
  totalSize: number;
  entryCount: number;
}

interface CacheConfig {
  maxSize: number; // Maximum cache size in MB
  defaultTTL: number; // Default TTL in milliseconds
  maxEntries: number; // Maximum number of entries
  cleanupInterval: number; // Cleanup interval in milliseconds
}

class CacheService {
  private cache = new Map<string, CacheEntry<any>>();
  private stats: CacheStats = {
    hits: 0,
    misses: 0,
    evictions: 0,
    totalSize: 0,
    entryCount: 0
  };
  private config: CacheConfig;
  private cleanupTimer?: NodeJS.Timeout;

  constructor(config: Partial<CacheConfig> = {}) {
    this.config = {
      maxSize: 50, // 50MB default
      defaultTTL: 1000 * 60 * 15, // 15 minutes
      maxEntries: 1000,
      cleanupInterval: 1000 * 60 * 5, // 5 minutes
      ...config
    };

    this.startCleanup();
  }

  private startCleanup() {
    this.cleanupTimer = setInterval(() => {
      this.cleanup();
    }, this.config.cleanupInterval);
  }

  private cleanup() {
    const now = Date.now();
    const entriesToRemove: string[] = [];

    for (const [key, entry] of this.cache.entries()) {
      // Remove expired entries
      if (now - entry.timestamp > entry.ttl) {
        entriesToRemove.push(key);
      }
    }

    // Remove expired entries
    entriesToRemove.forEach(key => {
      this.delete(key);
      this.stats.evictions++;
    });

    // If still over limits, remove least recently used entries
    if (this.cache.size > this.config.maxEntries || this.stats.totalSize > this.config.maxSize * 1024 * 1024) {
      this.evictLRU();
    }
  }

  private evictLRU() {
    const entries = Array.from(this.cache.entries())
      .sort(([, a], [, b]) => a.lastAccessed - b.lastAccessed);

    const targetSize = Math.floor(this.config.maxEntries * 0.8); // Remove 20% of entries
    const toRemove = entries.slice(0, entries.length - targetSize);

    toRemove.forEach(([key]) => {
      this.delete(key);
      this.stats.evictions++;
    });
  }

  private calculateSize(data: any): number {
    try {
      return new Blob([JSON.stringify(data)]).size;
    } catch {
      return 1024; // Default size estimate
    }
  }

  private generateKey(keyParts: any[]): string {
    return keyParts.map(part => 
      typeof part === 'object' ? JSON.stringify(part) : String(part)
    ).join(':');
  }

  set<T>(key: string | any[], data: T, ttl?: number): void {
    const cacheKey = Array.isArray(key) ? this.generateKey(key) : key;
    const size = this.calculateSize(data);
    const now = Date.now();

    // Remove existing entry if it exists
    if (this.cache.has(cacheKey)) {
      const existing = this.cache.get(cacheKey)!;
      this.stats.totalSize -= existing.size || 0;
    }

    const entry: CacheEntry<T> = {
      data,
      timestamp: now,
      ttl: ttl || this.config.defaultTTL,
      accessCount: 0,
      lastAccessed: now,
      size
    };

    this.cache.set(cacheKey, entry);
    this.stats.totalSize += size;
    this.stats.entryCount = this.cache.size;

    // Trigger cleanup if over limits
    if (this.cache.size > this.config.maxEntries || this.stats.totalSize > this.config.maxSize * 1024 * 1024) {
      this.cleanup();
    }
  }

  get<T>(key: string | any[]): T | null {
    const cacheKey = Array.isArray(key) ? this.generateKey(key) : key;
    const entry = this.cache.get(cacheKey);

    if (!entry) {
      this.stats.misses++;
      return null;
    }

    const now = Date.now();

    // Check if expired
    if (now - entry.timestamp > entry.ttl) {
      this.delete(cacheKey);
      this.stats.misses++;
      this.stats.evictions++;
      return null;
    }

    // Update access statistics
    entry.accessCount++;
    entry.lastAccessed = now;
    this.stats.hits++;

    return entry.data;
  }

  has(key: string | any[]): boolean {
    const cacheKey = Array.isArray(key) ? this.generateKey(key) : key;
    const entry = this.cache.get(cacheKey);

    if (!entry) return false;

    // Check if expired
    const now = Date.now();
    if (now - entry.timestamp > entry.ttl) {
      this.delete(cacheKey);
      return false;
    }

    return true;
  }

  delete(key: string | any[]): boolean {
    const cacheKey = Array.isArray(key) ? this.generateKey(key) : key;
    const entry = this.cache.get(cacheKey);

    if (entry) {
      this.stats.totalSize -= entry.size || 0;
      this.cache.delete(cacheKey);
      this.stats.entryCount = this.cache.size;
      return true;
    }

    return false;
  }

  clear(): void {
    this.cache.clear();
    this.stats = {
      hits: 0,
      misses: 0,
      evictions: 0,
      totalSize: 0,
      entryCount: 0
    };
  }

  getStats(): CacheStats & { hitRate: number; avgAccessCount: number } {
    const totalRequests = this.stats.hits + this.stats.misses;
    const hitRate = totalRequests > 0 ? (this.stats.hits / totalRequests) * 100 : 0;
    
    const entries = Array.from(this.cache.values());
    const avgAccessCount = entries.length > 0 
      ? entries.reduce((sum, entry) => sum + entry.accessCount, 0) / entries.length 
      : 0;

    return {
      ...this.stats,
      hitRate,
      avgAccessCount
    };
  }

  // Get cache entries sorted by various criteria
  getTopEntries(sortBy: 'accessCount' | 'size' | 'age', limit = 10) {
    const entries = Array.from(this.cache.entries()).map(([key, entry]) => ({
      key,
      ...entry
    }));

    switch (sortBy) {
      case 'accessCount':
        return entries.sort((a, b) => b.accessCount - a.accessCount).slice(0, limit);
      case 'size':
        return entries.sort((a, b) => (b.size || 0) - (a.size || 0)).slice(0, limit);
      case 'age':
        return entries.sort((a, b) => a.timestamp - b.timestamp).slice(0, limit);
      default:
        return entries.slice(0, limit);
    }
  }

  // Preload common data
  async preload<T>(key: string | any[], loader: () => Promise<T>, ttl?: number): Promise<T> {
    const cached = this.get<T>(key);
    if (cached !== null) {
      return cached;
    }

    const data = await loader();
    this.set(key, data, ttl);
    return data;
  }

  // Batch operations
  setMany<T>(entries: { key: string | any[]; data: T; ttl?: number }[]): void {
    entries.forEach(({ key, data, ttl }) => {
      this.set(key, data, ttl);
    });
  }

  getMany<T>(keys: (string | any[])[]): { key: string | any[]; data: T | null }[] {
    return keys.map(key => ({
      key,
      data: this.get<T>(key)
    }));
  }

  // Export/Import for persistence
  export(): string {
    const exportData = {
      timestamp: Date.now(),
      config: this.config,
      stats: this.stats,
      entries: Array.from(this.cache.entries())
    };

    return JSON.stringify(exportData);
  }

  import(data: string): void {
    try {
      const importData = JSON.parse(data);
      
      // Validate import data
      if (!importData.entries || !Array.isArray(importData.entries)) {
        throw new Error('Invalid cache data format');
      }

      this.clear();
      
      // Import entries
      importData.entries.forEach(([key, entry]: [string, CacheEntry<any>]) => {
        // Check if entry is still valid
        const now = Date.now();
        if (now - entry.timestamp < entry.ttl) {
          this.cache.set(key, entry);
          this.stats.totalSize += entry.size || 0;
        }
      });

      this.stats.entryCount = this.cache.size;
    } catch (error) {
      console.error('Failed to import cache data:', error);
    }
  }

  destroy(): void {
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
    }
    this.clear();
  }
}

// Specialized cache instances
export const levelCache = new CacheService({
  maxSize: 100, // 100MB for level data
  defaultTTL: 1000 * 60 * 30, // 30 minutes
  maxEntries: 500
});

export const configCache = new CacheService({
  maxSize: 10, // 10MB for configurations
  defaultTTL: 1000 * 60 * 60, // 1 hour
  maxEntries: 1000
});

export const apiCache = new CacheService({
  maxSize: 20, // 20MB for API responses
  defaultTTL: 1000 * 60 * 15, // 15 minutes
  maxEntries: 200
});

// React hooks for cache management
export const useCache = <T>(
  key: string | any[],
  loader: () => Promise<T>,
  options: { ttl?: number; cache?: CacheService } = {}
) => {
  const [data, setData] = React.useState<T | null>(null);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<Error | null>(null);
  
  const cache = options.cache || levelCache;

  const loadData = React.useCallback(async (force = false) => {
    if (!force) {
      const cached = cache.get<T>(key);
      if (cached !== null) {
        setData(cached);
        return cached;
      }
    }

    setLoading(true);
    setError(null);

    try {
      const result = await loader();
      cache.set(key, result, options.ttl);
      setData(result);
      return result;
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Unknown error');
      setError(error);
      throw error;
    } finally {
      setLoading(false);
    }
  }, [key, loader, options.ttl, cache]);

  React.useEffect(() => {
    loadData();
  }, [loadData]);

  const invalidate = React.useCallback(() => {
    cache.delete(key);
    loadData(true);
  }, [key, cache, loadData]);

  return {
    data,
    loading,
    error,
    reload: () => loadData(true),
    invalidate
  };
};

// Cache performance monitoring
export const useCacheStats = (cache: CacheService = levelCache) => {
  const [stats, setStats] = React.useState(cache.getStats());

  React.useEffect(() => {
    const interval = setInterval(() => {
      setStats(cache.getStats());
    }, 5000);

    return () => clearInterval(interval);
  }, [cache]);

  return stats;
};

export default CacheService;