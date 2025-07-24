import { GenerationConfig } from '../types';

/**
 * Compresses a configuration object for URL sharing
 */
export const compressConfigForUrl = (config: GenerationConfig): string => {
  try {
    // Create a simplified version of the config for URL sharing
    const simplified = {
      w: config.width,
      h: config.height,
      s: config.seed,
      a: config.generationAlgorithm,
      p: config.algorithmParameters,
      t: config.terrainTypes,
      e: config.entities.map(entity => ({
        t: entity.type,
        c: entity.count,
        d: entity.minDistance,
        s: entity.placementStrategy
      })),
      v: {
        n: config.visualTheme.themeName,
        c: config.visualTheme.colorPalette
      },
      g: {
        s: config.gameplay.playerSpeed,
        h: config.gameplay.playerHealth,
        d: config.gameplay.difficulty,
        l: config.gameplay.timeLimit,
        v: config.gameplay.victoryConditions
      }
    };

    // Convert to JSON and encode
    const json = JSON.stringify(simplified);
    return btoa(encodeURIComponent(json));
  } catch (error) {
    console.error('Failed to compress config for URL:', error);
    throw new Error('Failed to create shareable URL');
  }
};

/**
 * Decompresses a configuration from URL parameter
 */
export const decompressConfigFromUrl = (compressed: string): GenerationConfig => {
  try {
    // Decode and parse
    const json = decodeURIComponent(atob(compressed));
    const simplified = JSON.parse(json);

    // Reconstruct full config
    const config: GenerationConfig = {
      width: simplified.w || 50,
      height: simplified.h || 50,
      seed: simplified.s || 0,
      generationAlgorithm: simplified.a || 'perlin',
      algorithmParameters: simplified.p || {},
      terrainTypes: simplified.t || ['ground', 'wall', 'water'],
      entities: (simplified.e || []).map((e: any) => ({
        type: e.t || 'Item',
        count: e.c || 1,
        minDistance: e.d || 1.0,
        maxDistanceFromPlayer: Number.MAX_VALUE,
        properties: {},
        placementStrategy: e.s || 'random'
      })),
      visualTheme: {
        themeName: simplified.v?.n || 'default',
        colorPalette: simplified.v?.c || {},
        tileSprites: {},
        entitySprites: {},
        effectSettings: {}
      },
      gameplay: {
        playerSpeed: simplified.g?.s || 5.0,
        playerHealth: simplified.g?.h || 100,
        difficulty: simplified.g?.d || 'normal',
        timeLimit: simplified.g?.l || 0,
        victoryConditions: simplified.g?.v || ['reach_exit'],
        mechanics: {}
      }
    };

    return config;
  } catch (error) {
    console.error('Failed to decompress config from URL:', error);
    throw new Error('Invalid or corrupted configuration URL');
  }
};

/**
 * Creates a shareable URL for a configuration
 */
export const createShareableUrl = (config: GenerationConfig, baseUrl?: string): string => {
  const compressed = compressConfigForUrl(config);
  const base = baseUrl || window.location.origin + window.location.pathname;
  return `${base}?config=${compressed}`;
};

/**
 * Extracts configuration from current URL if present
 */
export const getConfigFromUrl = (): GenerationConfig | null => {
  try {
    const urlParams = new URLSearchParams(window.location.search);
    const configParam = urlParams.get('config');
    
    if (configParam) {
      return decompressConfigFromUrl(configParam);
    }
    
    return null;
  } catch (error) {
    console.error('Failed to extract config from URL:', error);
    return null;
  }
};

/**
 * Updates the current URL with configuration (without page reload)
 */
export const updateUrlWithConfig = (config: GenerationConfig): void => {
  try {
    const compressed = compressConfigForUrl(config);
    const url = new URL(window.location.href);
    url.searchParams.set('config', compressed);
    window.history.replaceState({}, '', url.toString());
  } catch (error) {
    console.error('Failed to update URL with config:', error);
  }
};

/**
 * Clears configuration from URL
 */
export const clearConfigFromUrl = (): void => {
  const url = new URL(window.location.href);
  url.searchParams.delete('config');
  window.history.replaceState({}, '', url.toString());
};

/**
 * Generates a QR code data URL for sharing (requires qrcode library)
 */
export const generateQRCodeForConfig = async (config: GenerationConfig): Promise<string> => {
  try {
    // For now, return a placeholder. In a real implementation, you'd use a QR code library
    const shareUrl = createShareableUrl(config);
    
    // Placeholder QR code generation
    // In a real app, you'd use: import QRCode from 'qrcode';
    // return await QRCode.toDataURL(shareUrl);
    
    return `data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200"><rect width="200" height="200" fill="white"/><text x="100" y="100" text-anchor="middle" fill="black" font-size="12">QR Code for: ${shareUrl.substring(0, 30)}...</text></svg>`;
  } catch (error) {
    console.error('Failed to generate QR code:', error);
    throw new Error('Failed to generate QR code');
  }
};

/**
 * Validates if a configuration URL is valid
 */
export const isValidConfigUrl = (url: string): boolean => {
  try {
    const urlObj = new URL(url);
    const configParam = urlObj.searchParams.get('config');
    
    if (!configParam) {
      return false;
    }
    
    // Try to decompress to validate
    decompressConfigFromUrl(configParam);
    return true;
  } catch (error) {
    return false;
  }
};