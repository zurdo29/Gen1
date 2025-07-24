import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import ProgressiveLoadingService from './progressiveLoading';

// Mock fetch
global.fetch = vi.fn();

describe('ProgressiveLoadingService', () => {
  let service: ProgressiveLoadingService;

  beforeEach(() => {
    service = new ProgressiveLoadingService({
      chunkSize: 1024, // 1KB chunks for testing
      maxConcurrent: 2,
      retryAttempts: 2,
      retryDelay: 100
    });
    vi.clearAllMocks();
  });

  afterEach(() => {
    service.cancelAll();
  });

  describe('level loading', () => {
    it('should create a level loader', () => {
      const loader = service.createLevelLoader('test-level', '/api/levels/test');
      expect(loader).toBeDefined();
      expect(service.getLoader('test-level')).toBe(loader);
    });

    it('should load level data progressively', async () => {
      const mockData = { terrain: 'test', entities: [] };
      
      // Mock HEAD request for size info
      (fetch as any).mockImplementationOnce(() =>
        Promise.resolve({
          ok: true,
          headers: {
            get: (name: string) => name === 'content-length' ? '2048' : null
          }
        })
      );

      // Mock chunk requests
      (fetch as any).mockImplementation(() =>
        Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockData)
        })
      );

      const loader = service.createLevelLoader('test-level', '/api/levels/test');
      const result = await loader.load();

      expect(result).toBeDefined();
      expect(fetch).toHaveBeenCalledTimes(3); // 1 HEAD + 2 chunk requests
    });

    it('should handle loading errors with retries', async () => {
      // Mock HEAD request
      (fetch as any).mockImplementationOnce(() =>
        Promise.resolve({
          ok: true,
          headers: {
            get: (name: string) => name === 'content-length' ? '1024' : null
          }
        })
      );

      // Mock failed chunk request, then success
      (fetch as any)
        .mockImplementationOnce(() => Promise.reject(new Error('Network error')))
        .mockImplementationOnce(() =>
          Promise.resolve({
            ok: true,
            json: () => Promise.resolve({ data: 'test' })
          })
        );

      const loader = service.createLevelLoader('test-level', '/api/levels/test');
      const result = await loader.load();

      expect(result).toBeDefined();
      expect(fetch).toHaveBeenCalledTimes(3); // 1 HEAD + 2 chunk attempts
    });

    it('should track loading progress', async () => {
      // Mock HEAD request
      (fetch as any).mockImplementationOnce(() =>
        Promise.resolve({
          ok: true,
          headers: {
            get: (name: string) => name === 'content-length' ? '2048' : null
          }
        })
      );

      // Mock chunk requests with delay
      (fetch as any).mockImplementation(() =>
        new Promise(resolve => {
          setTimeout(() => {
            resolve({
              ok: true,
              json: () => Promise.resolve({ data: 'test' })
            });
          }, 50);
        })
      );

      const loader = service.createLevelLoader('test-level', '/api/levels/test');
      
      // Start loading
      const loadPromise = loader.load();
      
      // Check initial progress
      let progress = loader.getProgress();
      expect(progress.loaded).toBe(0);
      expect(progress.total).toBe(0);
      
      // Wait for completion
      await loadPromise;
      
      // Check final progress
      progress = loader.getProgress();
      expect(progress.loaded).toBe(progress.total);
      expect(progress.percentage).toBe(100);
    });

    it('should support pause and resume', async () => {
      // Mock HEAD request
      (fetch as any).mockImplementationOnce(() =>
        Promise.resolve({
          ok: true,
          headers: {
            get: (name: string) => name === 'content-length' ? '1024' : null
          }
        })
      );

      // Mock slow chunk request
      (fetch as any).mockImplementation(() =>
        new Promise(resolve => {
          setTimeout(() => {
            resolve({
              ok: true,
              json: () => Promise.resolve({ data: 'test' })
            });
          }, 200);
        })
      );

      const loader = service.createLevelLoader('test-level', '/api/levels/test');
      
      // Start loading and immediately pause
      const loadPromise = loader.load();
      loader.pause();
      
      // Should still complete (pause doesn't cancel in-flight requests)
      const result = await loadPromise;
      expect(result).toBeDefined();
    });

    it('should support cancellation', async () => {
      const loader = service.createLevelLoader('test-level', '/api/levels/test');
      
      // Cancel immediately
      loader.cancel();
      
      // Progress should indicate cancellation
      const progress = loader.getProgress();
      expect(progress.loaded).toBe(0);
    });
  });

  describe('batch loading', () => {
    it('should create a batch loader', () => {
      const urls = ['/api/levels/1', '/api/levels/2'];
      const loader = service.createBatchLoader('test-batch', urls);
      
      expect(loader).toBeDefined();
      expect(service.getLoader('test-batch')).toBe(loader);
    });

    it('should load multiple levels', async () => {
      const urls = ['/api/levels/1', '/api/levels/2'];
      
      // Mock HEAD requests
      (fetch as any).mockImplementation((url: string) => {
        if (url.includes('info')) {
          return Promise.resolve({
            ok: true,
            headers: {
              get: (name: string) => name === 'content-length' ? '1024' : null
            }
          });
        }
        
        // Mock chunk requests
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ 
            data: url.includes('1') ? 'level1' : 'level2' 
          })
        });
      });

      const loader = service.createBatchLoader('test-batch', urls);
      const results = await loader.load();

      expect(results).toHaveLength(2);
      expect(fetch).toHaveBeenCalled();
    });

    it('should track batch progress', async () => {
      const urls = ['/api/levels/1'];
      
      // Mock requests
      (fetch as any).mockImplementation((url: string) => {
        if (url.includes('info')) {
          return Promise.resolve({
            ok: true,
            headers: {
              get: (name: string) => name === 'content-length' ? '1024' : null
            }
          });
        }
        
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ data: 'test' })
        });
      });

      const loader = service.createBatchLoader('test-batch', urls);
      
      const loadPromise = loader.load();
      
      // Check progress
      let progress = loader.getProgress();
      expect(progress.total).toBeGreaterThanOrEqual(0);
      
      await loadPromise;
      
      progress = loader.getProgress();
      expect(progress.percentage).toBe(100);
    });
  });

  describe('service management', () => {
    it('should manage multiple loaders', () => {
      const loader1 = service.createLevelLoader('level1', '/api/levels/1');
      const loader2 = service.createLevelLoader('level2', '/api/levels/2');
      
      expect(service.getActiveLoaders()).toContain('level1');
      expect(service.getActiveLoaders()).toContain('level2');
      
      service.removeLoader('level1');
      expect(service.getActiveLoaders()).not.toContain('level1');
      expect(service.getActiveLoaders()).toContain('level2');
    });

    it('should pause and resume all loaders', () => {
      const loader1 = service.createLevelLoader('level1', '/api/levels/1');
      const loader2 = service.createLevelLoader('level2', '/api/levels/2');
      
      const pauseSpy1 = vi.spyOn(loader1, 'pause');
      const pauseSpy2 = vi.spyOn(loader2, 'pause');
      const resumeSpy1 = vi.spyOn(loader1, 'resume');
      const resumeSpy2 = vi.spyOn(loader2, 'resume');
      
      service.pauseAll();
      expect(pauseSpy1).toHaveBeenCalled();
      expect(pauseSpy2).toHaveBeenCalled();
      
      service.resumeAll();
      expect(resumeSpy1).toHaveBeenCalled();
      expect(resumeSpy2).toHaveBeenCalled();
    });

    it('should cancel all loaders', () => {
      const loader1 = service.createLevelLoader('level1', '/api/levels/1');
      const loader2 = service.createLevelLoader('level2', '/api/levels/2');
      
      const cancelSpy1 = vi.spyOn(loader1, 'cancel');
      const cancelSpy2 = vi.spyOn(loader2, 'cancel');
      
      service.cancelAll();
      
      expect(cancelSpy1).toHaveBeenCalled();
      expect(cancelSpy2).toHaveBeenCalled();
      expect(service.getActiveLoaders()).toHaveLength(0);
    });
  });

  describe('error handling', () => {
    it('should handle network errors gracefully', async () => {
      // Mock HEAD request failure
      (fetch as any).mockImplementationOnce(() =>
        Promise.reject(new Error('Network error'))
      );

      const loader = service.createLevelLoader('test-level', '/api/levels/test');
      
      await expect(loader.load()).rejects.toThrow('Network error');
    });

    it('should handle invalid response sizes', async () => {
      // Mock HEAD request with no content-length
      (fetch as any).mockImplementationOnce(() =>
        Promise.resolve({
          ok: true,
          headers: {
            get: () => null
          }
        })
      );

      const loader = service.createLevelLoader('test-level', '/api/levels/test');
      
      await expect(loader.load()).rejects.toThrow('Unable to determine level size');
    });

    it('should handle chunk loading failures', async () => {
      // Mock HEAD request
      (fetch as any).mockImplementationOnce(() =>
        Promise.resolve({
          ok: true,
          headers: {
            get: (name: string) => name === 'content-length' ? '1024' : null
          }
        })
      );

      // Mock failed chunk requests
      (fetch as any).mockImplementation(() =>
        Promise.reject(new Error('Chunk error'))
      );

      const loader = service.createLevelLoader('test-level', '/api/levels/test');
      
      await expect(loader.load()).rejects.toThrow();
    });
  });

  describe('performance', () => {
    it('should respect concurrency limits', async () => {
      let concurrentRequests = 0;
      let maxConcurrent = 0;

      // Mock HEAD request
      (fetch as any).mockImplementationOnce(() =>
        Promise.resolve({
          ok: true,
          headers: {
            get: (name: string) => name === 'content-length' ? '4096' : null // 4 chunks
          }
        })
      );

      // Mock chunk requests with concurrency tracking
      (fetch as any).mockImplementation(() => {
        concurrentRequests++;
        maxConcurrent = Math.max(maxConcurrent, concurrentRequests);
        
        return new Promise(resolve => {
          setTimeout(() => {
            concurrentRequests--;
            resolve({
              ok: true,
              json: () => Promise.resolve({ data: 'test' })
            });
          }, 50);
        });
      });

      const loader = service.createLevelLoader('test-level', '/api/levels/test');
      await loader.load();

      // Should not exceed maxConcurrent setting (2)
      expect(maxConcurrent).toBeLessThanOrEqual(2);
    });

    it('should handle large numbers of chunks efficiently', async () => {
      // Mock HEAD request for large file
      (fetch as any).mockImplementationOnce(() =>
        Promise.resolve({
          ok: true,
          headers: {
            get: (name: string) => name === 'content-length' ? '10240' : null // 10 chunks
          }
        })
      );

      // Mock fast chunk requests
      (fetch as any).mockImplementation(() =>
        Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ data: 'chunk' })
        })
      );

      const startTime = performance.now();
      const loader = service.createLevelLoader('test-level', '/api/levels/test');
      await loader.load();
      const endTime = performance.now();

      // Should complete reasonably quickly
      expect(endTime - startTime).toBeLessThan(1000); // Less than 1 second
    });
  });
});