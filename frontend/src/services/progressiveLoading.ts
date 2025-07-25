// Progressive loading service for large level data
import React from 'react';

interface ProgressiveLoadingConfig {
  chunkSize: number;
  maxConcurrent: number;
  retryAttempts: number;
  retryDelay: number;
}

interface LoadingChunk<T> {
  id: string;
  data: T;
  index: number;
  size: number;
}

interface LoadingProgress {
  loaded: number;
  total: number;
  percentage: number;
  currentChunk?: string;
  error?: Error;
}

interface ProgressiveLoader<T> {
  load(): Promise<T[]>;
  pause(): void;
  resume(): void;
  cancel(): void;
  getProgress(): LoadingProgress;
}

class ProgressiveLoadingService {
  private config: ProgressiveLoadingConfig;
  private activeLoaders = new Map<string, ProgressiveLoader<any>>();

  constructor(config: Partial<ProgressiveLoadingConfig> = {}) {
    this.config = {
      chunkSize: 1024 * 1024, // 1MB chunks
      maxConcurrent: 3,
      retryAttempts: 3,
      retryDelay: 1000,
      ...config
    };
  }

  createLevelLoader(levelId: string, dataUrl: string): ProgressiveLoader<any> {
    const loader = new LevelProgressiveLoader(levelId, dataUrl, this.config);
    this.activeLoaders.set(levelId, loader);
    return loader;
  }

  createBatchLoader(batchId: string, urls: string[]): ProgressiveLoader<any> {
    const loader = new BatchProgressiveLoader(batchId, urls, this.config);
    this.activeLoaders.set(batchId, loader);
    return loader;
  }

  getLoader(id: string): ProgressiveLoader<any> | undefined {
    return this.activeLoaders.get(id);
  }

  removeLoader(id: string): void {
    const loader = this.activeLoaders.get(id);
    if (loader) {
      loader.cancel();
      this.activeLoaders.delete(id);
    }
  }

  pauseAll(): void {
    this.activeLoaders.forEach(loader => loader.pause());
  }

  resumeAll(): void {
    this.activeLoaders.forEach(loader => loader.resume());
  }

  cancelAll(): void {
    this.activeLoaders.forEach(loader => loader.cancel());
    this.activeLoaders.clear();
  }

  getActiveLoaders(): string[] {
    return Array.from(this.activeLoaders.keys());
  }
}

class LevelProgressiveLoader implements ProgressiveLoader<any> {
  private id: string;
  private dataUrl: string;
  private config: ProgressiveLoadingConfig;
  private chunks: LoadingChunk<any>[] = [];
  private loadedChunks = new Map<number, any>();
  private progress: LoadingProgress;
  private isPaused = false;
  private isCancelled = false;
  private activeRequests = new Set<Promise<any>>();

  constructor(id: string, dataUrl: string, config: ProgressiveLoadingConfig) {
    this.id = id;
    this.dataUrl = dataUrl;
    this.config = config;
    this.progress = {
      loaded: 0,
      total: 0,
      percentage: 0
    };
  }

  async load(): Promise<any[]> {
    try {
      // First, get the total size and chunk information
      await this.initializeChunks();
      
      // Load chunks progressively
      const results = await this.loadChunks();
      
      return results;
    } catch (error) {
      this.progress.error = error instanceof Error ? error : new Error('Loading failed');
      throw error;
    }
  }

  private async initializeChunks(): Promise<void> {
    const response = await fetch(`${this.dataUrl}/info`, {
      method: 'HEAD'
    });

    if (!response.ok) {
      throw new Error(`Failed to get level info: ${response.statusText}`);
    }

    const contentLength = response.headers.get('content-length');
    const totalSize = contentLength ? parseInt(contentLength) : 0;
    
    if (totalSize === 0) {
      throw new Error('Unable to determine level size');
    }

    const chunkCount = Math.ceil(totalSize / this.config.chunkSize);
    this.chunks = [];

    for (let i = 0; i < chunkCount; i++) {
      const start = i * this.config.chunkSize;
      const end = Math.min(start + this.config.chunkSize - 1, totalSize - 1);
      
      this.chunks.push({
        id: `${this.id}-chunk-${i}`,
        data: null,
        index: i,
        size: end - start + 1
      });
    }

    this.progress.total = this.chunks.length;
  }

  private async loadChunks(): Promise<any[]> {
    const semaphore = new Semaphore(this.config.maxConcurrent);
    const promises: Promise<void>[] = [];

    for (const chunk of this.chunks) {
      if (this.isCancelled) break;

      const promise = semaphore.acquire().then(async (release) => {
        try {
          if (!this.isCancelled && !this.isPaused) {
            await this.loadChunk(chunk);
          }
        } finally {
          release();
        }
      });

      promises.push(promise);
      this.activeRequests.add(promise);
    }

    await Promise.all(promises);
    
    // Combine chunks in order
    const results: any[] = [];
    for (let i = 0; i < this.chunks.length; i++) {
      const chunkData = this.loadedChunks.get(i);
      if (chunkData) {
        results.push(chunkData);
      }
    }

    return results;
  }

  private async loadChunk(chunk: LoadingChunk<any>): Promise<void> {
    let attempts = 0;
    
    while (attempts < this.config.retryAttempts) {
      if (this.isCancelled || this.isPaused) return;

      try {
        this.progress.currentChunk = chunk.id;
        
        const start = chunk.index * this.config.chunkSize;
        const end = start + chunk.size - 1;
        
        const response = await fetch(this.dataUrl, {
          headers: {
            'Range': `bytes=${start}-${end}`
          }
        });

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const data = await response.json();
        this.loadedChunks.set(chunk.index, data);
        
        this.progress.loaded++;
        this.progress.percentage = (this.progress.loaded / this.progress.total) * 100;
        
        return;
      } catch (error) {
        attempts++;
        
        if (attempts >= this.config.retryAttempts) {
          throw new Error(`Failed to load chunk ${chunk.id} after ${attempts} attempts: ${error}`);
        }
        
        // Wait before retry
        await new Promise(resolve => setTimeout(resolve, this.config.retryDelay * attempts));
      }
    }
  }

  pause(): void {
    this.isPaused = true;
  }

  resume(): void {
    this.isPaused = false;
  }

  cancel(): void {
    this.isCancelled = true;
    this.activeRequests.forEach(_request => {
      // Note: We can't actually cancel fetch requests, but we can ignore their results
    });
    this.activeRequests.clear();
  }

  getProgress(): LoadingProgress {
    return { ...this.progress };
  }
}

class BatchProgressiveLoader implements ProgressiveLoader<any> {
  private id: string;
  private urls: string[];
  private config: ProgressiveLoadingConfig;
  private loaders: LevelProgressiveLoader[] = [];
  private progress: LoadingProgress;
  private isPaused = false;
  private isCancelled = false;

  constructor(id: string, urls: string[], config: ProgressiveLoadingConfig) {
    this.id = id;
    this.urls = urls;
    this.config = config;
    this.progress = {
      loaded: 0,
      total: urls.length,
      percentage: 0
    };
  }

  async load(): Promise<any[]> {
    try {
      const results: any[] = [];
      
      for (let i = 0; i < this.urls.length; i++) {
        if (this.isCancelled) break;
        
        const loader = new LevelProgressiveLoader(`${this.id}-${i}`, this.urls[i], this.config);
        this.loaders.push(loader);
        
        const levelData = await loader.load();
        results.push(levelData);
        
        this.progress.loaded++;
        this.progress.percentage = (this.progress.loaded / this.progress.total) * 100;
      }
      
      return results;
    } catch (error) {
      this.progress.error = error instanceof Error ? error : new Error('Batch loading failed');
      throw error;
    }
  }

  pause(): void {
    this.isPaused = true;
    this.loaders.forEach(loader => loader.pause());
  }

  resume(): void {
    this.isPaused = false;
    this.loaders.forEach(loader => loader.resume());
  }

  cancel(): void {
    this.isCancelled = true;
    this.loaders.forEach(loader => loader.cancel());
  }

  getProgress(): LoadingProgress {
    if (this.loaders.length === 0) {
      return { ...this.progress };
    }

    // Aggregate progress from all loaders
    let totalLoaded = 0;
    let totalChunks = 0;
    
    this.loaders.forEach(loader => {
      const loaderProgress = loader.getProgress();
      totalLoaded += loaderProgress.loaded;
      totalChunks += loaderProgress.total;
    });

    return {
      loaded: totalLoaded,
      total: totalChunks,
      percentage: totalChunks > 0 ? (totalLoaded / totalChunks) * 100 : 0,
      currentChunk: this.progress.currentChunk,
      error: this.progress.error
    };
  }
}

// Semaphore for controlling concurrent requests
class Semaphore {
  private permits: number;
  private queue: (() => void)[] = [];

  constructor(permits: number) {
    this.permits = permits;
  }

  async acquire(): Promise<() => void> {
    return new Promise((resolve) => {
      if (this.permits > 0) {
        this.permits--;
        resolve(() => this.release());
      } else {
        this.queue.push(() => {
          this.permits--;
          resolve(() => this.release());
        });
      }
    });
  }

  private release(): void {
    this.permits++;
    if (this.queue.length > 0) {
      const next = this.queue.shift()!;
      next();
    }
  }
}

// React hook for progressive loading
export const useProgressiveLoading = <T>(
  id: string,
  dataUrl: string | string[],
  options: Partial<ProgressiveLoadingConfig> = {}
) => {
  const [data, setData] = React.useState<T[] | null>(null);
  const [progress, setProgress] = React.useState<LoadingProgress>({
    loaded: 0,
    total: 0,
    percentage: 0
  });
  const [isLoading, setIsLoading] = React.useState(false);
  const [error, setError] = React.useState<Error | null>(null);
  
  const serviceRef = React.useRef(new ProgressiveLoadingService(options));
  const loaderRef = React.useRef<ProgressiveLoader<T> | null>(null);

  const startLoading = React.useCallback(async () => {
    if (isLoading) return;

    setIsLoading(true);
    setError(null);
    setData(null);

    try {
      const service = serviceRef.current;
      
      // Create appropriate loader
      const loader = Array.isArray(dataUrl)
        ? service.createBatchLoader(id, dataUrl)
        : service.createLevelLoader(id, dataUrl);
      
      loaderRef.current = loader;

      // Start progress monitoring
      const progressInterval = setInterval(() => {
        setProgress(loader.getProgress());
      }, 100);

      // Load data
      const result = await loader.load();
      setData(result);
      
      clearInterval(progressInterval);
      setProgress(loader.getProgress());
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Loading failed');
      setError(error);
    } finally {
      setIsLoading(false);
    }
  }, [id, dataUrl, isLoading]);

  const pauseLoading = React.useCallback(() => {
    loaderRef.current?.pause();
  }, []);

  const resumeLoading = React.useCallback(() => {
    loaderRef.current?.resume();
  }, []);

  const cancelLoading = React.useCallback(() => {
    loaderRef.current?.cancel();
    serviceRef.current.removeLoader(id);
    setIsLoading(false);
  }, [id]);

  React.useEffect(() => {
    return () => {
      // Cleanup on unmount
      cancelLoading();
    };
  }, [cancelLoading]);

  return {
    data,
    progress,
    isLoading,
    error,
    startLoading,
    pauseLoading,
    resumeLoading,
    cancelLoading
  };
};

export default ProgressiveLoadingService;