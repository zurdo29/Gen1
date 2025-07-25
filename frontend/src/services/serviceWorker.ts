// Service Worker registration and management
import React from 'react';

interface ServiceWorkerConfig {
  onUpdate?: (registration: ServiceWorkerRegistration) => void;
  onSuccess?: (registration: ServiceWorkerRegistration) => void;
  onError?: (error: Error) => void;
}

type CacheStats = Record<string, number>;

class ServiceWorkerManager {
  private registration: ServiceWorkerRegistration | null = null;
  private isSupported: boolean;
  private updateAvailable = false;

  constructor() {
    this.isSupported = 'serviceWorker' in navigator;
  }

  async register(config: ServiceWorkerConfig = {}): Promise<ServiceWorkerRegistration | null> {
    if (!this.isSupported) {
      console.warn('Service Worker not supported in this browser');
      return null;
    }

    try {
      const registration = await navigator.serviceWorker.register('/sw.js', {
        scope: '/'
      });

      this.registration = registration;

      // Handle updates
      registration.addEventListener('updatefound', () => {
        const newWorker = registration.installing;
        if (newWorker) {
          newWorker.addEventListener('statechange', () => {
            if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
              this.updateAvailable = true;
              config.onUpdate?.(registration);
            }
          });
        }
      });

      // Handle successful registration
      if (registration.active) {
        config.onSuccess?.(registration);
      }

      console.log('Service Worker registered successfully');
      return registration;
    } catch (error) {
      const swError = error instanceof Error ? error : new Error('Service Worker registration failed');
      console.error('Service Worker registration failed:', swError);
      config.onError?.(swError);
      return null;
    }
  }

  async unregister(): Promise<boolean> {
    if (!this.registration) {
      return false;
    }

    try {
      const result = await this.registration.unregister();
      this.registration = null;
      console.log('Service Worker unregistered');
      return result;
    } catch (error) {
      console.error('Service Worker unregistration failed:', error);
      return false;
    }
  }

  async update(): Promise<void> {
    if (!this.registration) {
      throw new Error('No service worker registered');
    }

    await this.registration.update();
  }

  async skipWaiting(): Promise<void> {
    if (!this.registration?.waiting) {
      return;
    }

    // Send message to service worker to skip waiting
    this.registration.waiting.postMessage({ type: 'SKIP_WAITING' });
    
    // Wait for the new service worker to take control
    return new Promise((resolve) => {
      navigator.serviceWorker.addEventListener('controllerchange', () => {
        resolve();
      }, { once: true });
    });
  }

  async getCacheStats(): Promise<CacheStats> {
    if (!this.registration?.active) {
      return {};
    }

    return new Promise((resolve) => {
      const messageChannel = new MessageChannel();
      
      messageChannel.port1.onmessage = (event) => {
        if (event.data.type === 'CACHE_STATS') {
          resolve(event.data.payload);
        }
      };

      this.registration.active!.postMessage(
        { type: 'GET_CACHE_STATS' },
        [messageChannel.port2]
      );
    });
  }

  async clearCache(cacheName?: string): Promise<void> {
    if (!this.registration?.active) {
      return;
    }

    return new Promise((resolve) => {
      const messageChannel = new MessageChannel();
      
      messageChannel.port1.onmessage = (event) => {
        if (event.data.type === 'CACHE_CLEARED') {
          resolve();
        }
      };

      this.registration.active!.postMessage(
        { type: 'CLEAR_CACHE', payload: { cacheName } },
        [messageChannel.port2]
      );
    });
  }

  async prefetchUrls(urls: string[]): Promise<void> {
    if (!this.registration?.active) {
      return;
    }

    return new Promise((resolve) => {
      const messageChannel = new MessageChannel();
      
      messageChannel.port1.onmessage = (event) => {
        if (event.data.type === 'PREFETCH_COMPLETE') {
          resolve();
        }
      };

      this.registration.active!.postMessage(
        { type: 'PREFETCH_URLS', payload: { urls } },
        [messageChannel.port2]
      );
    });
  }

  isUpdateAvailable(): boolean {
    return this.updateAvailable;
  }

  isRegistered(): boolean {
    return this.registration !== null;
  }

  getRegistration(): ServiceWorkerRegistration | null {
    return this.registration;
  }
}

// Create singleton instance
export const serviceWorkerManager = new ServiceWorkerManager();

// React hook for service worker management
export const useServiceWorker = (config: ServiceWorkerConfig = {}) => {
  const [isRegistered, setIsRegistered] = React.useState(false);
  const [updateAvailable, setUpdateAvailable] = React.useState(false);
  const [isLoading, setIsLoading] = React.useState(false);
  const [error, setError] = React.useState<Error | null>(null);

  React.useEffect(() => {
    const register = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const registration = await serviceWorkerManager.register({
          onUpdate: (reg) => {
            setUpdateAvailable(true);
            config.onUpdate?.(reg);
          },
          onSuccess: (reg) => {
            setIsRegistered(true);
            config.onSuccess?.(reg);
          },
          onError: (err) => {
            setError(err);
            config.onError?.(err);
          }
        });

        setIsRegistered(!!registration);
      } catch (err) {
        const error = err instanceof Error ? err : new Error('Registration failed');
        setError(error);
      } finally {
        setIsLoading(false);
      }
    };

    register();
  }, []);

  const update = React.useCallback(async () => {
    try {
      await serviceWorkerManager.skipWaiting();
      setUpdateAvailable(false);
      window.location.reload();
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Update failed');
      setError(error);
    }
  }, []);

  const unregister = React.useCallback(async () => {
    try {
      await serviceWorkerManager.unregister();
      setIsRegistered(false);
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Unregister failed');
      setError(error);
    }
  }, []);

  return {
    isRegistered,
    updateAvailable,
    isLoading,
    error,
    update,
    unregister,
    getCacheStats: serviceWorkerManager.getCacheStats.bind(serviceWorkerManager),
    clearCache: serviceWorkerManager.clearCache.bind(serviceWorkerManager),
    prefetchUrls: serviceWorkerManager.prefetchUrls.bind(serviceWorkerManager)
  };
};

// Offline detection hook
export const useOfflineStatus = () => {
  const [isOnline, setIsOnline] = React.useState(navigator.onLine);
  const [wasOffline, setWasOffline] = React.useState(false);

  React.useEffect(() => {
    const handleOnline = () => {
      setIsOnline(true);
      if (wasOffline) {
        // Trigger any necessary sync operations
        console.log('Back online - syncing data');
      }
    };

    const handleOffline = () => {
      setIsOnline(false);
      setWasOffline(true);
    };

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, [wasOffline]);

  return {
    isOnline,
    isOffline: !isOnline,
    wasOffline
  };
};

// Network quality detection
export const useNetworkQuality = () => {
  const [networkQuality, setNetworkQuality] = React.useState<'fast' | 'slow' | 'offline'>('fast');
  const [connectionType, setConnectionType] = React.useState<string>('unknown');

  React.useEffect(() => {
    // @ts-expect-error - Connection API is experimental
    const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;

    if (connection) {
      const updateConnectionInfo = () => {
        setConnectionType(connection.effectiveType || 'unknown');
        
        // Determine network quality based on effective type
        if (connection.effectiveType === '4g') {
          setNetworkQuality('fast');
        } else if (connection.effectiveType === '3g' || connection.effectiveType === '2g') {
          setNetworkQuality('slow');
        } else {
          setNetworkQuality('fast'); // Default for unknown
        }
      };

      updateConnectionInfo();
      connection.addEventListener('change', updateConnectionInfo);

      return () => {
        connection.removeEventListener('change', updateConnectionInfo);
      };
    }

    // Fallback: measure network speed
    const measureNetworkSpeed = async () => {
      try {
        const startTime = performance.now();
        await fetch('/api/health', { cache: 'no-cache' });
        const endTime = performance.now();
        const responseTime = endTime - startTime;

        if (responseTime < 200) {
          setNetworkQuality('fast');
        } else if (responseTime < 1000) {
          setNetworkQuality('slow');
        } else {
          setNetworkQuality('offline');
        }
      } catch {
        setNetworkQuality('offline');
      }
    };

    const interval = setInterval(measureNetworkSpeed, 30000); // Check every 30 seconds
    measureNetworkSpeed(); // Initial check

    return () => clearInterval(interval);
  }, []);

  return {
    networkQuality,
    connectionType,
    isFast: networkQuality === 'fast',
    isSlow: networkQuality === 'slow',
    isOffline: networkQuality === 'offline'
  };
};

export default serviceWorkerManager;