// Local storage service for configuration persistence and offline support
import React from 'react';

interface StorageConfig {
  prefix: string;
  version: string;
  maxSize: number; // Maximum storage size in MB
  compression: boolean;
  encryption: boolean;
}

interface StorageEntry<T> {
  data: T;
  timestamp: number;
  version: string;
  ttl?: number;
  compressed?: boolean;
  encrypted?: boolean;
}

interface StorageStats {
  totalSize: number;
  entryCount: number;
  availableSpace: number;
  usagePercentage: number;
}

class LocalStorageService {
  private config: StorageConfig;
  private isSupported: boolean;

  constructor(config: Partial<StorageConfig> = {}) {
    this.config = {
      prefix: 'web-level-editor',
      version: '1.0.0',
      maxSize: 50, // 50MB
      compression: true,
      encryption: false,
      ...config
    };

    this.isSupported = this.checkSupport();

    if (this.isSupported) {
      this.cleanup();
    }
  }

  private checkSupport(): boolean {
    try {
      const testKey = `${this.config.prefix}-test`;
      localStorage.setItem(testKey, 'test');
      localStorage.removeItem(testKey);
      return true;
    } catch {
      return false;
    }
  }

  private generateKey(key: string): string {
    return `${this.config.prefix}-${key}`;
  }

  private compress(data: string): string {
    if (!this.config.compression) return data;

    // For now, just return the data as-is
    // In production, use a proper compression library like lz-string
    return data;
  }

  private decompress(data: string): string {
    if (!this.config.compression) return data;

    // For now, just return the data as-is
    return data;
  }

  private encrypt(data: string): string {
    if (!this.config.encryption) return data;

    // Simple XOR encryption for demonstration
    // In production, use proper encryption
    const key = 'web-level-editor-key';
    let result = '';

    for (let i = 0; i < data.length; i++) {
      result += String.fromCharCode(
        data.charCodeAt(i) ^ key.charCodeAt(i % key.length)
      );
    }

    return btoa(result);
  }

  private decrypt(data: string): string {
    if (!this.config.encryption) return data;

    try {
      const decoded = atob(data);
      const key = 'web-level-editor-key';
      let result = '';

      for (let i = 0; i < decoded.length; i++) {
        result += String.fromCharCode(
          decoded.charCodeAt(i) ^ key.charCodeAt(i % key.length)
        );
      }

      return result;
    } catch {
      throw new Error('Failed to decrypt data');
    }
  }

  set<T>(key: string, data: T, ttl?: number): boolean {
    if (!this.isSupported) return false;

    try {
      const entry: StorageEntry<T> = {
        data,
        timestamp: Date.now(),
        version: this.config.version,
        ttl,
        compressed: this.config.compression,
        encrypted: this.config.encryption
      };

      let serialized = JSON.stringify(entry);

      if (this.config.compression) {
        serialized = this.compress(serialized);
      }

      if (this.config.encryption) {
        serialized = this.encrypt(serialized);
      }

      const storageKey = this.generateKey(key);

      // Check if we have enough space
      const estimatedSize = new Blob([serialized]).size;
      const stats = this.getStats();

      if (stats.totalSize + estimatedSize > this.config.maxSize * 1024 * 1024) {
        // Try to free up space
        this.cleanup();

        // Check again
        const newStats = this.getStats();
        if (newStats.totalSize + estimatedSize > this.config.maxSize * 1024 * 1024) {
          throw new Error('Not enough storage space');
        }
      }

      localStorage.setItem(storageKey, serialized);
      return true;
    } catch (error) {
      console.error('Failed to save to localStorage:', error);
      return false;
    }
  }

  get<T>(key: string): T | null {
    if (!this.isSupported) return null;

    try {
      const storageKey = this.generateKey(key);
      let serialized = localStorage.getItem(storageKey);

      if (!serialized) return null;

      if (this.config.encryption) {
        serialized = this.decrypt(serialized);
      }

      if (this.config.compression) {
        serialized = this.decompress(serialized);
      }

      const entry: StorageEntry<T> = JSON.parse(serialized);

      // Check version compatibility
      if (entry.version !== this.config.version) {
        this.remove(key);
        return null;
      }

      // Check TTL
      if (entry.ttl && Date.now() - entry.timestamp > entry.ttl) {
        this.remove(key);
        return null;
      }

      return entry.data;
    } catch (error) {
      console.error('Failed to read from localStorage:', error);
      this.remove(key); // Remove corrupted data
      return null;
    }
  }

  has(key: string): boolean {
    return this.get(key) !== null;
  }

  remove(key: string): boolean {
    if (!this.isSupported) return false;

    try {
      const storageKey = this.generateKey(key);
      localStorage.removeItem(storageKey);
      return true;
    } catch {
      return false;
    }
  }

  clear(): boolean {
    if (!this.isSupported) return false;

    try {
      const keys = this.getAllKeys();
      keys.forEach(key => localStorage.removeItem(key));
      return true;
    } catch {
      return false;
    }
  }

  getAllKeys(): string[] {
    if (!this.isSupported) return [];

    const keys: string[] = [];
    const prefix = `${this.config.prefix}-`;

    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith(prefix)) {
        keys.push(key);
      }
    }

    return keys;
  }

  getStats(): StorageStats {
    if (!this.isSupported) {
      return {
        totalSize: 0,
        entryCount: 0,
        availableSpace: 0,
        usagePercentage: 0
      };
    }

    const keys = this.getAllKeys();
    let totalSize = 0;

    keys.forEach(key => {
      const value = localStorage.getItem(key);
      if (value) {
        totalSize += new Blob([value]).size;
      }
    });

    const maxSize = this.config.maxSize * 1024 * 1024;
    const availableSpace = maxSize - totalSize;
    const usagePercentage = (totalSize / maxSize) * 100;

    return {
      totalSize,
      entryCount: keys.length,
      availableSpace,
      usagePercentage
    };
  }

  cleanup(): void {
    if (!this.isSupported) return;

    const keys = this.getAllKeys();
    const now = Date.now();

    // Remove expired entries
    keys.forEach(key => {
      try {
        const value = localStorage.getItem(key);
        if (value) {
          let serialized = value;

          if (this.config.encryption) {
            serialized = this.decrypt(serialized);
          }

          if (this.config.compression) {
            serialized = this.decompress(serialized);
          }

          const entry: StorageEntry<any> = JSON.parse(serialized);

          // Remove if expired or wrong version
          if (
            (entry.ttl && now - entry.timestamp > entry.ttl) ||
            entry.version !== this.config.version
          ) {
            localStorage.removeItem(key);
          }
        }
      } catch {
        // Remove corrupted entries
        localStorage.removeItem(key);
      }
    });

    // If still over limit, remove oldest entries
    const stats = this.getStats();
    if (stats.usagePercentage > 80) {
      this.evictOldest(Math.floor(stats.entryCount * 0.2)); // Remove 20% of entries
    }
  }

  private evictOldest(count: number): void {
    const entries: { key: string; timestamp: number }[] = [];

    this.getAllKeys().forEach(key => {
      try {
        const value = localStorage.getItem(key);
        if (value) {
          let serialized = value;

          if (this.config.encryption) {
            serialized = this.decrypt(serialized);
          }

          if (this.config.compression) {
            serialized = this.decompress(serialized);
          }

          const entry: StorageEntry<any> = JSON.parse(serialized);
          entries.push({ key, timestamp: entry.timestamp });
        }
      } catch {
        // Remove corrupted entries
        localStorage.removeItem(key);
      }
    });

    // Sort by timestamp and remove oldest
    entries
      .sort((a, b) => a.timestamp - b.timestamp)
      .slice(0, count)
      .forEach(({ key }) => localStorage.removeItem(key));
  }

  // Batch operations
  setMany<T>(entries: { key: string; data: T; ttl?: number }[]): boolean {
    try {
      entries.forEach(({ key, data, ttl }) => {
        this.set(key, data, ttl);
      });
      return true;
    } catch {
      return false;
    }
  }

  getMany<T>(keys: string[]): { key: string; data: T | null }[] {
    return keys.map(key => ({
      key,
      data: this.get<T>(key)
    }));
  }

  // Export/Import for backup
  export(): string | null {
    if (!this.isSupported) return null;

    try {
      const data: Record<string, any> = {};
      const keys = this.getAllKeys();

      keys.forEach(key => {
        const value = localStorage.getItem(key);
        if (value) {
          const originalKey = key.replace(`${this.config.prefix}-`, '');
          data[originalKey] = value;
        }
      });

      return JSON.stringify({
        version: this.config.version,
        timestamp: Date.now(),
        data
      });
    } catch {
      return null;
    }
  }

  import(exportedData: string): boolean {
    if (!this.isSupported) return false;

    try {
      const parsed = JSON.parse(exportedData);

      if (parsed.version !== this.config.version) {
        console.warn('Version mismatch in imported data');
      }

      Object.entries(parsed.data).forEach(([key, value]) => {
        const storageKey = this.generateKey(key);
        localStorage.setItem(storageKey, value as string);
      });

      return true;
    } catch {
      return false;
    }
  }

  isStorageSupported(): boolean {
    return this.isSupported;
  }
}

// Specialized storage instances
export const configStorage = new LocalStorageService({
  prefix: 'web-level-editor-config',
  maxSize: 10, // 10MB for configurations
  compression: true
});

export const levelStorage = new LocalStorageService({
  prefix: 'web-level-editor-levels',
  maxSize: 100, // 100MB for level data
  compression: true
});

export const userStorage = new LocalStorageService({
  prefix: 'web-level-editor-user',
  maxSize: 5, // 5MB for user preferences
  compression: false
});

// React hooks for local storage
export const useLocalStorage = <T>(
  key: string,
  defaultValue: T,
  storage: LocalStorageService = configStorage
) => {
  const [value, setValue] = React.useState<T>(() => {
    const stored = storage.get<T>(key);
    return stored !== null ? stored : defaultValue;
  });

  const setStoredValue = React.useCallback((newValue: T | ((prev: T) => T)) => {
    try {
      const valueToStore = newValue instanceof Function ? newValue(value) : newValue;
      setValue(valueToStore);
      storage.set(key, valueToStore);
    } catch (error) {
      console.error('Failed to save to localStorage:', error);
    }
  }, [key, value, storage]);

  const removeValue = React.useCallback(() => {
    setValue(defaultValue);
    storage.remove(key);
  }, [key, defaultValue, storage]);

  return [value, setStoredValue, removeValue] as const;
};

// Hook for persistent configuration
export const usePersistentConfig = <T extends Record<string, any>>(
  configKey: string,
  defaultConfig: T
) => {
  const [config, setConfig, removeConfig] = useLocalStorage(
    configKey,
    defaultConfig,
    configStorage
  );

  const updateConfig = React.useCallback((updates: Partial<T>) => {
    setConfig(prev => ({ ...prev, ...updates }));
  }, [setConfig]);

  const resetConfig = React.useCallback(() => {
    setConfig(defaultConfig);
  }, [setConfig, defaultConfig]);

  return {
    config,
    setConfig,
    updateConfig,
    resetConfig,
    removeConfig
  };
};

// Hook for offline data persistence
export const useOfflineData = <T>(
  key: string,
  fetcher: () => Promise<T>,
  options: { ttl?: number; storage?: LocalStorageService } = {}
) => {
  const storage = options.storage || levelStorage;
  const [data, setData] = React.useState<T | null>(null);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<Error | null>(null);
  const [isOffline, setIsOffline] = React.useState(!navigator.onLine);

  React.useEffect(() => {
    const handleOnline = () => setIsOffline(false);
    const handleOffline = () => setIsOffline(true);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  const loadData = React.useCallback(async (forceRefresh = false) => {
    setLoading(true);
    setError(null);

    try {
      // Try to get cached data first
      if (!forceRefresh) {
        const cached = storage.get<T>(key);
        if (cached !== null) {
          setData(cached);
          setLoading(false);

          // If online, try to refresh in background
          if (!isOffline) {
            try {
              const fresh = await fetcher();
              storage.set(key, fresh, options.ttl);
              setData(fresh);
            } catch {
              // Ignore background refresh errors
            }
          }
          return cached;
        }
      }

      // If offline and no cache, throw error
      if (isOffline) {
        throw new Error('No cached data available offline');
      }

      // Fetch fresh data
      const fresh = await fetcher();
      storage.set(key, fresh, options.ttl);
      setData(fresh);
      return fresh;
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Failed to load data');
      setError(error);

      // Try to return cached data as fallback
      const cached = storage.get<T>(key);
      if (cached !== null) {
        setData(cached);
        return cached;
      }

      throw error;
    } finally {
      setLoading(false);
    }
  }, [key, fetcher, options.ttl, storage, isOffline]);

  React.useEffect(() => {
    loadData();
  }, [loadData]);

  const refresh = React.useCallback(() => {
    return loadData(true);
  }, [loadData]);

  const clearCache = React.useCallback(() => {
    storage.remove(key);
    setData(null);
  }, [key, storage]);

  return {
    data,
    loading,
    error,
    isOffline,
    refresh,
    clearCache
  };
};

export default LocalStorageService;