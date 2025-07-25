// Performance monitoring and optimization service

interface PerformanceMetric {
  name: string;
  value: number;
  timestamp: number;
  category: 'render' | 'api' | 'user' | 'memory';
  metadata?: Record<string, any>;
}

interface PerformanceThresholds {
  renderTime: number; // ms
  apiResponseTime: number; // ms
  memoryUsage: number; // MB
  fps: number;
}

class PerformanceService {
  private metrics: PerformanceMetric[] = [];
  private observers: PerformanceObserver[] = [];
  private thresholds: PerformanceThresholds = {
    renderTime: 16, // 60fps target
    apiResponseTime: 1000,
    memoryUsage: 100,
    fps: 30
  };
  private isMonitoring = false;

  constructor() {
    this.initializeObservers();
  }

  private initializeObservers() {
    // Performance Observer for navigation timing
    if ('PerformanceObserver' in window) {
      try {
        const navObserver = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            this.recordMetric({
              name: entry.name,
              value: entry.duration,
              timestamp: Date.now(),
              category: 'api',
              metadata: {
                entryType: entry.entryType,
                startTime: entry.startTime
              }
            });
          }
        });
        
        navObserver.observe({ entryTypes: ['navigation', 'resource'] });
        this.observers.push(navObserver);
      } catch (error) {
        console.warn('Performance Observer not supported:', error);
      }
    }
  }

  startMonitoring() {
    if (this.isMonitoring) return;
    
    this.isMonitoring = true;
    
    // Monitor memory usage
    this.startMemoryMonitoring();
    
    // Monitor FPS
    this.startFPSMonitoring();
    
    console.log('Performance monitoring started');
  }

  stopMonitoring() {
    this.isMonitoring = false;
    this.observers.forEach(observer => observer.disconnect());
    this.observers = [];
    
    console.log('Performance monitoring stopped');
  }

  private startMemoryMonitoring() {
    const checkMemory = () => {
      if (!this.isMonitoring) return;
      
      // @ts-expect-error - memory API is experimental
      if (performance.memory) {
        // @ts-expect-error
        const memoryInfo = performance.memory;
        this.recordMetric({
          name: 'memory-usage',
          value: memoryInfo.usedJSHeapSize / 1024 / 1024, // Convert to MB
          timestamp: Date.now(),
          category: 'memory',
          metadata: {
            totalJSHeapSize: memoryInfo.totalJSHeapSize / 1024 / 1024,
            jsHeapSizeLimit: memoryInfo.jsHeapSizeLimit / 1024 / 1024
          }
        });
      }
      
      setTimeout(checkMemory, 5000); // Check every 5 seconds
    };
    
    checkMemory();
  }

  private startFPSMonitoring() {
    let lastTime = performance.now();
    let frameCount = 0;
    
    const measureFPS = (currentTime: number) => {
      if (!this.isMonitoring) return;
      
      frameCount++;
      
      if (currentTime - lastTime >= 1000) { // Every second
        const fps = Math.round((frameCount * 1000) / (currentTime - lastTime));
        
        this.recordMetric({
          name: 'fps',
          value: fps,
          timestamp: Date.now(),
          category: 'render'
        });
        
        frameCount = 0;
        lastTime = currentTime;
      }
      
      requestAnimationFrame(measureFPS);
    };
    
    requestAnimationFrame(measureFPS);
  }

  recordMetric(metric: PerformanceMetric) {
    this.metrics.push(metric);
    
    // Keep only last 1000 metrics to prevent memory leaks
    if (this.metrics.length > 1000) {
      this.metrics = this.metrics.slice(-1000);
    }
    
    // Check thresholds and warn if exceeded
    this.checkThresholds(metric);
  }

  private checkThresholds(metric: PerformanceMetric) {
    let threshold: number | undefined;
    
    switch (metric.name) {
      case 'render-time':
        threshold = this.thresholds.renderTime;
        break;
      case 'api-response':
        threshold = this.thresholds.apiResponseTime;
        break;
      case 'memory-usage':
        threshold = this.thresholds.memoryUsage;
        break;
      case 'fps':
        if (metric.value < this.thresholds.fps) {
          console.warn(`Low FPS detected: ${metric.value}fps`);
        }
        return;
    }
    
    if (threshold && metric.value > threshold) {
      console.warn(`Performance threshold exceeded for ${metric.name}: ${metric.value}ms (threshold: ${threshold}ms)`);
    }
  }

  // Measure render performance
  measureRender<T>(name: string, renderFn: () => T): T {
    const start = performance.now();
    const result = renderFn();
    const end = performance.now();
    
    this.recordMetric({
      name: `render-${name}`,
      value: end - start,
      timestamp: Date.now(),
      category: 'render'
    });
    
    return result;
  }

  // Measure API performance
  async measureAPI<T>(name: string, apiFn: () => Promise<T>): Promise<T> {
    const start = performance.now();
    try {
      const result = await apiFn();
      const end = performance.now();
      
      this.recordMetric({
        name: `api-${name}`,
        value: end - start,
        timestamp: Date.now(),
        category: 'api',
        metadata: { success: true }
      });
      
      return result;
    } catch (error) {
      const end = performance.now();
      
      this.recordMetric({
        name: `api-${name}`,
        value: end - start,
        timestamp: Date.now(),
        category: 'api',
        metadata: { success: false, error: error instanceof Error ? error.message : 'Unknown error' }
      });
      
      throw error;
    }
  }

  // Measure user interaction performance
  measureUserAction(name: string, duration: number, metadata?: Record<string, any>) {
    this.recordMetric({
      name: `user-${name}`,
      value: duration,
      timestamp: Date.now(),
      category: 'user',
      metadata
    });
  }

  // Get performance statistics
  getStats(category?: PerformanceMetric['category'], timeWindow?: number) {
    let filteredMetrics = this.metrics;
    
    if (category) {
      filteredMetrics = filteredMetrics.filter(m => m.category === category);
    }
    
    if (timeWindow) {
      const cutoff = Date.now() - timeWindow;
      filteredMetrics = filteredMetrics.filter(m => m.timestamp > cutoff);
    }
    
    if (filteredMetrics.length === 0) {
      return null;
    }
    
    const values = filteredMetrics.map(m => m.value);
    const sum = values.reduce((a, b) => a + b, 0);
    
    return {
      count: filteredMetrics.length,
      average: sum / filteredMetrics.length,
      min: Math.min(...values),
      max: Math.max(...values),
      median: this.calculateMedian(values),
      p95: this.calculatePercentile(values, 95),
      p99: this.calculatePercentile(values, 99)
    };
  }

  private calculateMedian(values: number[]): number {
    const sorted = [...values].sort((a, b) => a - b);
    const mid = Math.floor(sorted.length / 2);
    
    return sorted.length % 2 === 0
      ? (sorted[mid - 1] + sorted[mid]) / 2
      : sorted[mid];
  }

  private calculatePercentile(values: number[], percentile: number): number {
    const sorted = [...values].sort((a, b) => a - b);
    const index = Math.ceil((percentile / 100) * sorted.length) - 1;
    return sorted[Math.max(0, index)];
  }

  // Get recent metrics
  getRecentMetrics(count = 100): PerformanceMetric[] {
    return this.metrics.slice(-count);
  }

  // Clear metrics
  clearMetrics() {
    this.metrics = [];
  }

  // Export metrics for analysis
  exportMetrics(): string {
    return JSON.stringify({
      timestamp: Date.now(),
      thresholds: this.thresholds,
      metrics: this.metrics,
      stats: {
        render: this.getStats('render'),
        api: this.getStats('api'),
        user: this.getStats('user'),
        memory: this.getStats('memory')
      }
    }, null, 2);
  }

  // Update thresholds
  updateThresholds(newThresholds: Partial<PerformanceThresholds>) {
    this.thresholds = { ...this.thresholds, ...newThresholds };
  }

  // Get performance recommendations
  getRecommendations(): string[] {
    const recommendations: string[] = [];
    const renderStats = this.getStats('render', 60000); // Last minute
    const apiStats = this.getStats('api', 60000);
    const memoryStats = this.getStats('memory', 60000);
    
    if (renderStats && renderStats.average > this.thresholds.renderTime) {
      recommendations.push('Consider optimizing render performance - average render time is high');
    }
    
    if (apiStats && apiStats.average > this.thresholds.apiResponseTime) {
      recommendations.push('API response times are slow - consider caching or optimization');
    }
    
    if (memoryStats && memoryStats.max > this.thresholds.memoryUsage) {
      recommendations.push('High memory usage detected - check for memory leaks');
    }
    
    const recentFPS = this.metrics
      .filter(m => m.name === 'fps' && m.timestamp > Date.now() - 10000)
      .map(m => m.value);
    
    if (recentFPS.length > 0 && Math.min(...recentFPS) < this.thresholds.fps) {
      recommendations.push('Low FPS detected - consider reducing render complexity');
    }
    
    return recommendations;
  }
}

// Create singleton instance
export const performanceService = new PerformanceService();

import React from 'react';

// React hook for performance monitoring
export const usePerformanceMonitoring = () => {
  const [isMonitoring, setIsMonitoring] = React.useState(false);
  const [stats, setStats] = React.useState<any>(null);
  
  React.useEffect(() => {
    if (isMonitoring) {
      performanceService.startMonitoring();
      
      const interval = setInterval(() => {
        setStats({
          render: performanceService.getStats('render', 60000),
          api: performanceService.getStats('api', 60000),
          memory: performanceService.getStats('memory', 60000)
        });
      }, 5000);
      
      return () => {
        clearInterval(interval);
        performanceService.stopMonitoring();
      };
    }
  }, [isMonitoring]);
  
  return {
    isMonitoring,
    stats,
    startMonitoring: () => setIsMonitoring(true),
    stopMonitoring: () => setIsMonitoring(false),
    measureRender: performanceService.measureRender.bind(performanceService),
    measureAPI: performanceService.measureAPI.bind(performanceService),
    getRecommendations: performanceService.getRecommendations.bind(performanceService)
  };
};

// Performance decorator for class methods
export function measurePerformance(category: PerformanceMetric['category']) {
  return function (target: any, propertyName: string, descriptor: PropertyDescriptor) {
    const method = descriptor.value;
    
    descriptor.value = function (...args: any[]) {
      const start = performance.now();
      const result = method.apply(this, args);
      const end = performance.now();
      
      performanceService.recordMetric({
        name: `${target.constructor.name}.${propertyName}`,
        value: end - start,
        timestamp: Date.now(),
        category
      });
      
      return result;
    };
    
    return descriptor;
  };
}

export default performanceService;