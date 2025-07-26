import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Level, ValidationResult } from '../types';

export type GenerationProgressCallback = (sessionId: string, progress: number, message: string) => void;

export type PreviewGeneratedCallback = (sessionId: string, level: Level) => void;

export type GenerationErrorCallback = (sessionId: string, error: string) => void;

export type ValidationResultCallback = (sessionId: string, result: ValidationResult) => void;

export interface SignalRCallbacks {
  onGenerationProgress?: GenerationProgressCallback;
  onPreviewGenerated?: PreviewGeneratedCallback;
  onGenerationError?: GenerationErrorCallback;
  onValidationResult?: ValidationResultCallback;
}

class SignalRService {
  private connection: HubConnection | null = null;
  private currentSessionId: string | null = null;
  private callbacks: SignalRCallbacks = {};

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection() {
    // Get the base URL for the API
    const baseUrl = (import.meta as any).env?.VITE_API_URL || '';
    const hubUrl = `${baseUrl}/hubs/generation`;

    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    // Set up event handlers
    this.connection.on('GenerationProgress', (sessionId: string, progress: number, message: string) => {
      console.log(`Generation progress for ${sessionId}: ${progress}% - ${message}`);
      this.callbacks.onGenerationProgress?.(sessionId, progress, message);
    });

    this.connection.on('PreviewGenerated', (sessionId: string, level: Level) => {
      console.log(`Preview generated for ${sessionId}:`, level);
      this.callbacks.onPreviewGenerated?.(sessionId, level);
    });

    this.connection.on('GenerationError', (sessionId: string, error: string) => {
      console.error(`Generation error for ${sessionId}:`, error);
      this.callbacks.onGenerationError?.(sessionId, error);
    });

    this.connection.on('ValidationResult', (sessionId: string, result: ValidationResult) => {
      console.log(`Validation result for ${sessionId}:`, result);
      this.callbacks.onValidationResult?.(sessionId, result);
    });

    this.connection.on('PreviewRequested', (sessionId: string) => {
      console.log(`Preview requested acknowledged for ${sessionId}`);
    });

    // Handle connection events
    this.connection.onreconnecting(() => {
      console.log('SignalR connection lost, attempting to reconnect...');
    });

    this.connection.onreconnected(() => {
      console.log('SignalR connection restored');
      // Rejoin session if we were in one
      if (this.currentSessionId) {
        this.joinSession(this.currentSessionId);
      }
    });

    this.connection.onclose(() => {
      console.log('SignalR connection closed');
    });
  }

  async connect(): Promise<void> {
    if (!this.connection) {
      this.initializeConnection();
    }

    if (this.connection!.state === 'Disconnected') {
      try {
        await this.connection!.start();
        console.log('SignalR connection established');
      } catch (error) {
        console.error('Failed to establish SignalR connection:', error);
        throw error;
      }
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection && this.connection.state !== 'Disconnected') {
      try {
        if (this.currentSessionId) {
          await this.leaveSession(this.currentSessionId);
        }
        await this.connection.stop();
        console.log('SignalR connection closed');
      } catch (error) {
        console.error('Error closing SignalR connection:', error);
      }
    }
  }

  async joinSession(sessionId: string): Promise<void> {
    if (!this.connection || this.connection.state !== 'Connected') {
      await this.connect();
    }

    try {
      // Leave current session if any
      if (this.currentSessionId && this.currentSessionId !== sessionId) {
        await this.leaveSession(this.currentSessionId);
      }

      await this.connection!.invoke('JoinSession', sessionId);
      this.currentSessionId = sessionId;
      console.log(`Joined session: ${sessionId}`);
    } catch (error) {
      console.error('Failed to join session:', error);
      throw error;
    }
  }

  async leaveSession(sessionId: string): Promise<void> {
    if (!this.connection || this.connection.state !== 'Connected') {
      return;
    }

    try {
      await this.connection.invoke('LeaveSession', sessionId);
      if (this.currentSessionId === sessionId) {
        this.currentSessionId = null;
      }
      console.log(`Left session: ${sessionId}`);
    } catch (error) {
      console.error('Failed to leave session:', error);
    }
  }

  setCallbacks(callbacks: SignalRCallbacks): void {
    this.callbacks = { ...this.callbacks, ...callbacks };
  }

  clearCallbacks(): void {
    this.callbacks = {};
  }

  isConnected(): boolean {
    return this.connection?.state === 'Connected';
  }

  getCurrentSessionId(): string | null {
    return this.currentSessionId;
  }
}

// Export singleton instance
export const signalRService = new SignalRService();
export default signalRService;