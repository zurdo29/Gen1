import axios, { AxiosInstance, AxiosError } from 'axios';
import { 
  GenerationConfig, 
  Level, 
  ValidationResult, 
  ConfigPreset, 
  ShareResult, 
  ExportFormat,
  ExportOptions,
  JobStatus,
  BatchGenerationRequest,
  PreviewRequestResponse,
  PreviewStatus
} from '../types';

class ApiService {
  private client: AxiosInstance;
  private requestId = '';

  constructor() {
    this.client = axios.create({
      baseURL: '/api',
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor for logging and request ID
    this.client.interceptors.request.use(
      (config) => {
        this.requestId = Date.now().toString();
        config.headers['X-Request-ID'] = this.requestId;
        
        console.log(`API Request [${this.requestId}]: ${config.method?.toUpperCase()} ${config.url}`);
        return config;
      },
      (error) => {
        console.error('API Request Error:', error);
        return Promise.reject(this.enhanceError(error, 'Request preparation failed'));
      }
    );

    // Response interceptor for enhanced error handling
    this.client.interceptors.response.use(
      (response) => {
        const requestId = response.config.headers['X-Request-ID'];
        console.log(`API Response [${requestId}]: ${response.status} ${response.config.url}`);
        return response;
      },
      (error: AxiosError) => {
        const requestId = error.config?.headers?.['X-Request-ID'] || 'unknown';
        console.error(`API Response Error [${requestId}]:`, error.response?.data || error.message);
        
        return Promise.reject(this.enhanceError(error, 'API request failed'));
      }
    );
  }

  private enhanceError(error: any, context: string): AxiosError {
    // Add additional context to the error
    if (error.isAxiosError) {
      error.context = context;
      error.requestId = this.requestId;
      error.timestamp = new Date().toISOString();
    }
    return error;
  }

  // Health check
  async checkHealth(): Promise<{ status: string; version: string; timestamp: string }> {
    const response = await this.client.get('/health');
    return response.data;
  }

  // Generation endpoints
  async generateLevel(config: GenerationConfig): Promise<Level> {
    const response = await this.client.post('/generation/generate', config);
    return response.data;
  }

  async generateBatch(request: BatchGenerationRequest): Promise<string> {
    const response = await this.client.post('/generation/generate-batch', request);
    return response.data.jobId;
  }

  async cancelBatch(jobId: string): Promise<void> {
    await this.client.delete(`/generation/batch/${jobId}`);
  }

  async getJobStatus(jobId: string): Promise<JobStatus> {
    const response = await this.client.get(`/generation/job/${jobId}/status`);
    return response.data;
  }

  async validateConfiguration(config: GenerationConfig): Promise<ValidationResult> {
    const response = await this.client.post('/generation/validate-config', config);
    return response.data;
  }

  // Configuration endpoints
  async getPresets(): Promise<ConfigPreset[]> {
    const response = await this.client.get('/configuration/presets');
    return response.data;
  }

  async savePreset(preset: Omit<ConfigPreset, 'id' | 'createdAt'>): Promise<ConfigPreset> {
    const response = await this.client.post('/configuration/presets', preset);
    return response.data;
  }

  async getSharedConfiguration(shareId: string): Promise<GenerationConfig> {
    const response = await this.client.get(`/configuration/share/${shareId}`);
    return response.data;
  }

  async createShareLink(config: GenerationConfig, expiryDays?: number): Promise<ShareResult> {
    const response = await this.client.post('/configuration/share', {
      config,
      expiryDays
    });
    return response.data;
  }

  // Export endpoints
  async exportLevel(level: Level, format: string, options: ExportOptions): Promise<Blob> {
    const response = await this.client.post('/export/level', {
      level,
      format,
      options
    }, {
      responseType: 'blob'
    });
    return response.data;
  }

  async getExportFormats(): Promise<ExportFormat[]> {
    const response = await this.client.get('/export/formats');
    return response.data;
  }

  async exportBatch(levels: Level[], format: string, options: ExportOptions): Promise<string> {
    const response = await this.client.post('/export/batch-export', {
      levels,
      format,
      options
    });
    return response.data.jobId;
  }

  // Real-time preview endpoints
  async requestPreview(sessionId: string, config: GenerationConfig, debounceMs = 500): Promise<PreviewRequestResponse> {
    const response = await this.client.post('/generation/preview', {
      sessionId,
      config,
      debounceMs
    });
    return response.data;
  }

  async getPreviewStatus(sessionId: string): Promise<PreviewStatus> {
    const response = await this.client.get(`/generation/preview/${sessionId}/status`);
    return response.data;
  }

  async cancelPreview(sessionId: string): Promise<void> {
    await this.client.delete(`/generation/preview/${sessionId}`);
  }
}

export const apiService = new ApiService();

// Export individual methods for easier testing and usage
export const generateLevel = (config: GenerationConfig) => apiService.generateLevel(config);
export const validateConfiguration = (config: GenerationConfig) => apiService.validateConfiguration(config);
export const getPresets = () => apiService.getPresets();
export const savePreset = (preset: Omit<ConfigPreset, 'id' | 'createdAt'>) => apiService.savePreset(preset);
export const exportLevel = (level: Level, format: string, options: ExportOptions) => apiService.exportLevel(level, format, options);
export const getExportFormats = () => apiService.getExportFormats();
export const shareConfiguration = (config: GenerationConfig, expiryDays?: number) => apiService.createShareLink(config, expiryDays);
export const getSharedConfiguration = (shareId: string) => apiService.getSharedConfiguration(shareId);

export default apiService;