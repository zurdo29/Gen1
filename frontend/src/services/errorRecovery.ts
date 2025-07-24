import { GenerationConfig } from '../types';
import { apiService } from './api';

export interface RecoveryStrategy {
  name: string;
  description: string;
  canRecover: (error: any, context: any) => boolean;
  recover: (error: any, context: any) => Promise<any>;
  priority: number; // Higher number = higher priority
}

export class ErrorRecoveryService {
  private static instance: ErrorRecoveryService;
  private strategies: RecoveryStrategy[] = [];

  static getInstance(): ErrorRecoveryService {
    if (!ErrorRecoveryService.instance) {
      ErrorRecoveryService.instance = new ErrorRecoveryService();
      ErrorRecoveryService.instance.initializeDefaultStrategies();
    }
    return ErrorRecoveryService.instance;
  }

  private initializeDefaultStrategies(): void {
    this.registerStrategy({
      name: 'Configuration Reset',
      description: 'Reset configuration to default values',
      priority: 5,
      canRecover: (error, context) => {
        return error.response?.status === 400 && 
               context.operation?.includes('generation') &&
               context.config;
      },
      recover: async (error, context) => {
        const defaultConfig: GenerationConfig = {
          width: 50,
          height: 50,
          seed: Math.floor(Math.random() * 1000000),
          generationAlgorithm: 'perlin-noise',
          algorithmParameters: {},
          terrainTypes: ['grass', 'stone', 'water'],
          entities: [],
          visualTheme: {
            themeName: 'default',
            colorPalette: {},
            tileSprites: {},
            entitySprites: {},
            effectSettings: {}
          },
          gameplay: {
            playerSpeed: 5,
            playerHealth: 100,
            difficulty: 'medium',
            timeLimit: 300,
            victoryConditions: ['reach_exit'],
            mechanics: {}
          }
        };

        return { config: defaultConfig, message: 'Configuration reset to default values' };
      }
    });

    this.registerStrategy({
      name: 'Reduce Level Complexity',
      description: 'Automatically reduce level size and entity count',
      priority: 4,
      canRecover: (error, context) => {
        return (error.response?.data?.code === 'MEMORY_LIMIT_EXCEEDED' ||
                error.response?.data?.code === 'GENERATION_TIMEOUT') &&
               context.config;
      },
      recover: async (error, context) => {
        const config = { ...context.config };
        
        // Reduce dimensions by 50%
        config.width = Math.max(10, Math.floor(config.width * 0.5));
        config.height = Math.max(10, Math.floor(config.height * 0.5));
        
        // Reduce entity counts
        config.entities = config.entities.map((entity: any) => ({
          ...entity,
          count: Math.max(1, Math.floor(entity.count * 0.5))
        }));

        return { 
          config, 
          message: `Reduced level size to ${config.width}x${config.height} and entity counts by 50%` 
        };
      }
    });

    this.registerStrategy({
      name: 'Retry with Exponential Backoff',
      description: 'Retry the operation with increasing delays',
      priority: 3,
      canRecover: (error, context) => {
        return error.response?.status >= 500 || 
               error.code === 'NETWORK_ERROR' ||
               error.response?.status === 429;
      },
      recover: async (error, context) => {
        const maxRetries = 3;
        const baseDelay = 1000; // 1 second
        
        for (let attempt = 1; attempt <= maxRetries; attempt++) {
          const delay = baseDelay * Math.pow(2, attempt - 1);
          
          await new Promise(resolve => setTimeout(resolve, delay));
          
          try {
            if (context.originalOperation) {
              const result = await context.originalOperation();
              return { 
                result, 
                message: `Operation succeeded after ${attempt} ${attempt === 1 ? 'retry' : 'retries'}` 
              };
            }
          } catch (retryError) {
            if (attempt === maxRetries) {
              throw retryError;
            }
          }
        }
        
        throw error;
      }
    });

    this.registerStrategy({
      name: 'Fallback Export Format',
      description: 'Try alternative export formats',
      priority: 4,
      canRecover: (error, context) => {
        return error.response?.data?.code === 'UNSUPPORTED_FORMAT' ||
               error.response?.data?.code === 'EXPORT_SIZE_LIMIT';
      },
      recover: async (error, context) => {
        const fallbackFormats = ['json', 'csv', 'xml'];
        const currentFormat = context.format;
        
        for (const format of fallbackFormats) {
          if (format !== currentFormat) {
            try {
              const result = await apiService.exportLevel(
                context.level, 
                format, 
                { ...context.options, format }
              );
              return { 
                result, 
                format,
                message: `Export successful using ${format.toUpperCase()} format instead of ${currentFormat?.toUpperCase()}` 
              };
            } catch (fallbackError) {
              continue;
            }
          }
        }
        
        throw error;
      }
    });

    this.registerStrategy({
      name: 'Offline Mode Fallback',
      description: 'Switch to offline mode for basic functionality',
      priority: 2,
      canRecover: (error, context) => {
        return error.code === 'NETWORK_ERROR' && 
               !context.requiresServer;
      },
      recover: async (error, context) => {
        // Enable offline mode
        localStorage.setItem('offlineMode', 'true');
        
        // Use cached data if available
        const cachedData = localStorage.getItem(`cache_${context.operation}`);
        if (cachedData) {
          return { 
            result: JSON.parse(cachedData), 
            message: 'Using cached data in offline mode' 
          };
        }
        
        return { 
          message: 'Switched to offline mode. Some features may be limited.' 
        };
      }
    });

    this.registerStrategy({
      name: 'Simplified Configuration',
      description: 'Use a simpler configuration that is more likely to succeed',
      priority: 3,
      canRecover: (error, context) => {
        return error.response?.status === 400 && 
               context.config &&
               (context.config.width > 100 || context.config.height > 100);
      },
      recover: async (error, context) => {
        const config = { ...context.config };
        
        // Use simpler algorithm
        config.generationAlgorithm = 'simple-random';
        config.algorithmParameters = {};
        
        // Limit level size
        config.width = Math.min(config.width, 50);
        config.height = Math.min(config.height, 50);
        
        // Reduce entity types
        config.entities = config.entities.slice(0, 3).map((entity: any) => ({
          ...entity,
          count: Math.min(entity.count, 10)
        }));
        
        return { 
          config, 
          message: 'Simplified configuration for better compatibility' 
        };
      }
    });
  }

  registerStrategy(strategy: RecoveryStrategy): void {
    this.strategies.push(strategy);
    this.strategies.sort((a, b) => b.priority - a.priority);
  }

  async attemptRecovery(error: any, context: any): Promise<{
    success: boolean;
    result?: any;
    message?: string;
    strategy?: string;
  }> {
    for (const strategy of this.strategies) {
      if (strategy.canRecover(error, context)) {
        try {
          console.log(`Attempting recovery with strategy: ${strategy.name}`);
          
          const result = await strategy.recover(error, context);
          
          return {
            success: true,
            result: result.result || result,
            message: result.message || `Recovery successful using ${strategy.name}`,
            strategy: strategy.name
          };
        } catch (recoveryError) {
          console.warn(`Recovery strategy ${strategy.name} failed:`, recoveryError);
          continue;
        }
      }
    }

    return {
      success: false,
      message: 'No recovery strategy could resolve this error'
    };
  }

  getAvailableStrategies(error: any, context: any): RecoveryStrategy[] {
    return this.strategies.filter(strategy => strategy.canRecover(error, context));
  }

  async suggestRecoveryActions(error: any, context: any): Promise<{
    automatic: RecoveryStrategy[];
    manual: string[];
  }> {
    const availableStrategies = this.getAvailableStrategies(error, context);
    
    const manualSuggestions: string[] = [];
    
    // Add context-specific manual suggestions
    if (error.response?.status === 400) {
      manualSuggestions.push('Check your configuration parameters for invalid values');
      manualSuggestions.push('Try using a preset configuration as a starting point');
    }
    
    if (error.response?.data?.code === 'GENERATION_TIMEOUT') {
      manualSuggestions.push('Reduce the level dimensions');
      manualSuggestions.push('Use a simpler generation algorithm');
      manualSuggestions.push('Decrease the number of entities');
    }
    
    if (error.code === 'NETWORK_ERROR') {
      manualSuggestions.push('Check your internet connection');
      manualSuggestions.push('Try refreshing the page');
      manualSuggestions.push('Switch to offline mode if available');
    }
    
    if (error.response?.status === 429) {
      manualSuggestions.push('Wait a few minutes before trying again');
      manualSuggestions.push('Reduce the frequency of your requests');
    }

    return {
      automatic: availableStrategies,
      manual: manualSuggestions
    };
  }
}

export const errorRecoveryService = ErrorRecoveryService.getInstance();