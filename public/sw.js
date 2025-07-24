// Service Worker for Web Level Editor
// Provides offline functionality and intelligent caching

const CACHE_NAME = 'web-level-editor-v1';
const STATIC_CACHE_NAME = 'web-level-editor-static-v1';
const DYNAMIC_CACHE_NAME = 'web-level-editor-dynamic-v1';
const API_CACHE_NAME = 'web-level-editor-api-v1';

// Static assets to cache immediately
const STATIC_ASSETS = [
  '/',
  '/index.html',
  '/manifest.json',
  '/static/js/bundle.js',
  '/static/css/main.css',
  // Add other static assets as needed
];

// API endpoints to cache
const API_ENDPOINTS = [
  '/api/export/formats',
  '/api/configuration/presets',
  '/api/health'
];

// Cache strategies
const CACHE_STRATEGIES = {
  CACHE_FIRST: 'cache-first',
  NETWORK_FIRST: 'network-first',
  STALE_WHILE_REVALIDATE: 'stale-while-revalidate',
  NETWORK_ONLY: 'network-only',
  CACHE_ONLY: 'cache-only'
};

// Route configurations
const ROUTE_CONFIG = {
  '/api/generation/': { strategy: CACHE_STRATEGIES.NETWORK_FIRST, ttl: 300000 }, // 5 minutes
  '/api/export/formats': { strategy: CACHE_STRATEGIES.CACHE_FIRST, ttl: 3600000 }, // 1 hour
  '/api/configuration/presets': { strategy: CACHE_STRATEGIES.STALE_WHILE_REVALIDATE, ttl: 1800000 }, // 30 minutes
  '/api/configuration/share/': { strategy: CACHE_STRATEGIES.CACHE_FIRST, ttl: 86400000 }, // 24 hours
  '/api/health': { strategy: CACHE_STRATEGIES.NETWORK_FIRST, ttl: 60000 }, // 1 minute
  'static/': { strategy: CACHE_STRATEGIES.CACHE_FIRST, ttl: 86400000 }, // 24 hours
  'images/': { strategy: CACHE_STRATEGIES.CACHE_FIRST, ttl: 86400000 } // 24 hours
};

// Install event - cache static assets
self.addEventListener('install', (event) => {
  console.log('Service Worker: Installing...');
  
  event.waitUntil(
    Promise.all([
      // Cache static assets
      caches.open(STATIC_CACHE_NAME).then((cache) => {
        console.log('Service Worker: Caching static assets');
        return cache.addAll(STATIC_ASSETS);
      }),
      
      // Pre-cache important API endpoints
      caches.open(API_CACHE_NAME).then((cache) => {
        console.log('Service Worker: Pre-caching API endpoints');
        return Promise.all(
          API_ENDPOINTS.map(endpoint => 
            fetch(endpoint)
              .then(response => response.ok ? cache.put(endpoint, response) : null)
              .catch(() => null) // Ignore errors during pre-caching
          )
        );
      })
    ]).then(() => {
      console.log('Service Worker: Installation complete');
      // Force activation of new service worker
      return self.skipWaiting();
    })
  );
});

// Activate event - clean up old caches
self.addEventListener('activate', (event) => {
  console.log('Service Worker: Activating...');
  
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames.map((cacheName) => {
          // Delete old caches
          if (cacheName !== STATIC_CACHE_NAME && 
              cacheName !== DYNAMIC_CACHE_NAME && 
              cacheName !== API_CACHE_NAME) {
            console.log('Service Worker: Deleting old cache:', cacheName);
            return caches.delete(cacheName);
          }
        })
      );
    }).then(() => {
      console.log('Service Worker: Activation complete');
      // Take control of all pages immediately
      return self.clients.claim();
    })
  );
});

// Fetch event - handle requests with appropriate caching strategy
self.addEventListener('fetch', (event) => {
  const { request } = event;
  const url = new URL(request.url);
  
  // Skip non-GET requests and chrome-extension requests
  if (request.method !== 'GET' || url.protocol === 'chrome-extension:') {
    return;
  }
  
  // Determine caching strategy based on URL
  const strategy = determineStrategy(url.pathname);
  
  event.respondWith(
    handleRequest(request, strategy)
      .catch((error) => {
        console.error('Service Worker: Request failed:', error);
        return handleOfflineFallback(request);
      })
  );
});

// Determine caching strategy for a given path
function determineStrategy(pathname) {
  for (const [pattern, config] of Object.entries(ROUTE_CONFIG)) {
    if (pathname.includes(pattern)) {
      return config;
    }
  }
  
  // Default strategy
  return { strategy: CACHE_STRATEGIES.NETWORK_FIRST, ttl: 300000 };
}

// Handle request based on strategy
async function handleRequest(request, config) {
  const { strategy, ttl } = config;
  
  switch (strategy) {
    case CACHE_STRATEGIES.CACHE_FIRST:
      return handleCacheFirst(request, ttl);
    
    case CACHE_STRATEGIES.NETWORK_FIRST:
      return handleNetworkFirst(request, ttl);
    
    case CACHE_STRATEGIES.STALE_WHILE_REVALIDATE:
      return handleStaleWhileRevalidate(request, ttl);
    
    case CACHE_STRATEGIES.NETWORK_ONLY:
      return fetch(request);
    
    case CACHE_STRATEGIES.CACHE_ONLY:
      return handleCacheOnly(request);
    
    default:
      return handleNetworkFirst(request, ttl);
  }
}

// Cache First strategy
async function handleCacheFirst(request, ttl) {
  const cachedResponse = await getCachedResponse(request, ttl);
  
  if (cachedResponse) {
    return cachedResponse;
  }
  
  const networkResponse = await fetch(request);
  
  if (networkResponse.ok) {
    await cacheResponse(request, networkResponse.clone(), ttl);
  }
  
  return networkResponse;
}

// Network First strategy
async function handleNetworkFirst(request, ttl) {
  try {
    const networkResponse = await fetch(request);
    
    if (networkResponse.ok) {
      await cacheResponse(request, networkResponse.clone(), ttl);
    }
    
    return networkResponse;
  } catch (error) {
    const cachedResponse = await getCachedResponse(request, ttl);
    
    if (cachedResponse) {
      return cachedResponse;
    }
    
    throw error;
  }
}

// Stale While Revalidate strategy
async function handleStaleWhileRevalidate(request, ttl) {
  const cachedResponse = await getCachedResponse(request, ttl);
  
  // Start network request in background
  const networkPromise = fetch(request).then(async (response) => {
    if (response.ok) {
      await cacheResponse(request, response.clone(), ttl);
    }
    return response;
  }).catch(() => null);
  
  // Return cached response immediately if available
  if (cachedResponse) {
    return cachedResponse;
  }
  
  // Wait for network response if no cache
  return networkPromise;
}

// Cache Only strategy
async function handleCacheOnly(request) {
  const cachedResponse = await getCachedResponse(request);
  
  if (cachedResponse) {
    return cachedResponse;
  }
  
  throw new Error('No cached response available');
}

// Get cached response if valid
async function getCachedResponse(request, ttl) {
  const cache = await caches.open(getDynamicCacheName(request));
  const cachedResponse = await cache.match(request);
  
  if (!cachedResponse) {
    return null;
  }
  
  // Check TTL if specified
  if (ttl) {
    const cachedTime = cachedResponse.headers.get('sw-cached-time');
    if (cachedTime && Date.now() - parseInt(cachedTime) > ttl) {
      await cache.delete(request);
      return null;
    }
  }
  
  return cachedResponse;
}

// Cache response with timestamp
async function cacheResponse(request, response, ttl) {
  const cache = await caches.open(getDynamicCacheName(request));
  
  // Add timestamp header for TTL checking
  const responseToCache = new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers: {
      ...Object.fromEntries(response.headers.entries()),
      'sw-cached-time': Date.now().toString()
    }
  });
  
  await cache.put(request, responseToCache);
}

// Get appropriate cache name for request
function getDynamicCacheName(request) {
  const url = new URL(request.url);
  
  if (url.pathname.startsWith('/api/')) {
    return API_CACHE_NAME;
  }
  
  return DYNAMIC_CACHE_NAME;
}

// Handle offline fallback
async function handleOfflineFallback(request) {
  const url = new URL(request.url);
  
  // For HTML requests, return cached index.html
  if (request.headers.get('accept')?.includes('text/html')) {
    const cachedIndex = await caches.match('/index.html');
    if (cachedIndex) {
      return cachedIndex;
    }
  }
  
  // For API requests, return offline response
  if (url.pathname.startsWith('/api/')) {
    return new Response(
      JSON.stringify({
        error: 'Offline',
        message: 'This feature requires an internet connection',
        offline: true
      }),
      {
        status: 503,
        statusText: 'Service Unavailable',
        headers: {
          'Content-Type': 'application/json'
        }
      }
    );
  }
  
  // For other requests, return generic offline response
  return new Response('Offline', {
    status: 503,
    statusText: 'Service Unavailable'
  });
}

// Background sync for failed requests
self.addEventListener('sync', (event) => {
  if (event.tag === 'background-sync') {
    event.waitUntil(handleBackgroundSync());
  }
});

// Handle background sync
async function handleBackgroundSync() {
  console.log('Service Worker: Handling background sync');
  
  // Get failed requests from IndexedDB and retry them
  // This would be implemented based on specific needs
}

// Push notifications (for future use)
self.addEventListener('push', (event) => {
  if (event.data) {
    const data = event.data.json();
    
    event.waitUntil(
      self.registration.showNotification(data.title, {
        body: data.body,
        icon: '/icon-192x192.png',
        badge: '/badge-72x72.png',
        data: data.url
      })
    );
  }
});

// Notification click handler
self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  
  if (event.notification.data) {
    event.waitUntil(
      clients.openWindow(event.notification.data)
    );
  }
});

// Message handler for communication with main thread
self.addEventListener('message', (event) => {
  const { type, payload } = event.data;
  
  switch (type) {
    case 'SKIP_WAITING':
      self.skipWaiting();
      break;
    
    case 'GET_CACHE_STATS':
      getCacheStats().then(stats => {
        event.ports[0].postMessage({ type: 'CACHE_STATS', payload: stats });
      });
      break;
    
    case 'CLEAR_CACHE':
      clearCache(payload.cacheName).then(() => {
        event.ports[0].postMessage({ type: 'CACHE_CLEARED' });
      });
      break;
    
    case 'PREFETCH_URLS':
      prefetchUrls(payload.urls).then(() => {
        event.ports[0].postMessage({ type: 'PREFETCH_COMPLETE' });
      });
      break;
  }
});

// Get cache statistics
async function getCacheStats() {
  const cacheNames = await caches.keys();
  const stats = {};
  
  for (const cacheName of cacheNames) {
    const cache = await caches.open(cacheName);
    const keys = await cache.keys();
    stats[cacheName] = keys.length;
  }
  
  return stats;
}

// Clear specific cache
async function clearCache(cacheName) {
  if (cacheName) {
    await caches.delete(cacheName);
  } else {
    const cacheNames = await caches.keys();
    await Promise.all(cacheNames.map(name => caches.delete(name)));
  }
}

// Prefetch URLs
async function prefetchUrls(urls) {
  const cache = await caches.open(DYNAMIC_CACHE_NAME);
  
  await Promise.all(
    urls.map(async (url) => {
      try {
        const response = await fetch(url);
        if (response.ok) {
          await cache.put(url, response);
        }
      } catch (error) {
        console.warn('Failed to prefetch:', url, error);
      }
    })
  );
}

console.log('Service Worker: Script loaded');