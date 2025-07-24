import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { signalRService } from './signalr';
import { Level, ValidationResult } from '../types';

// Create mock connection first
const mockConnection = {
  start: vi.fn(),
  stop: vi.fn(),
  invoke: vi.fn(),
  on: vi.fn(),
  onreconnecting: vi.fn(),
  onreconnected: vi.fn(),
  onclose: vi.fn(),
  state: 'Disconnected'
};

// Mock SignalR
vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn(() => ({
    withUrl: vi.fn().mockReturnThis(),
    withAutomaticReconnect: vi.fn().mockReturnThis(),
    configureLogging: vi.fn().mockReturnThis(),
    build: vi.fn(() => mockConnection)
  })),
  LogLevel: {
    Information: 'Information'
  }
}));

describe('SignalRService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockConnection.state = 'Disconnected';
  });

  afterEach(async () => {
    await signalRService.disconnect();
  });

  describe('Connection Management', () => {
    it('should establish connection successfully', async () => {
      // Arrange
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      // Act
      await signalRService.connect();

      // Assert
      expect(mockConnection.start).toHaveBeenCalledOnce();
      expect(signalRService.isConnected()).toBe(true);
    });

    it('should handle connection failure', async () => {
      // Arrange
      const error = new Error('Connection failed');
      mockConnection.start.mockRejectedValue(error);

      // Act & Assert
      await expect(signalRService.connect()).rejects.toThrow('Connection failed');
      expect(signalRService.isConnected()).toBe(false);
    });

    it('should not start connection if already connected', async () => {
      // Arrange
      mockConnection.state = 'Connected';

      // Act
      await signalRService.connect();

      // Assert
      expect(mockConnection.start).not.toHaveBeenCalled();
    });

    it('should disconnect gracefully', async () => {
      // Arrange
      mockConnection.state = 'Connected';
      mockConnection.stop.mockResolvedValue(undefined);

      // Act
      await signalRService.disconnect();

      // Assert
      expect(mockConnection.stop).toHaveBeenCalledOnce();
    });
  });

  describe('Session Management', () => {
    beforeEach(async () => {
      mockConnection.state = 'Connected';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.invoke.mockResolvedValue(undefined);
      await signalRService.connect();
    });

    it('should join session successfully', async () => {
      // Arrange
      const sessionId = 'test-session-123';

      // Act
      await signalRService.joinSession(sessionId);

      // Assert
      expect(mockConnection.invoke).toHaveBeenCalledWith('JoinSession', sessionId);
      expect(signalRService.getCurrentSessionId()).toBe(sessionId);
    });

    it('should leave current session when joining new one', async () => {
      // Arrange
      const sessionId1 = 'session-1';
      const sessionId2 = 'session-2';

      await signalRService.joinSession(sessionId1);
      mockConnection.invoke.mockClear();

      // Act
      await signalRService.joinSession(sessionId2);

      // Assert
      expect(mockConnection.invoke).toHaveBeenCalledWith('LeaveSession', sessionId1);
      expect(mockConnection.invoke).toHaveBeenCalledWith('JoinSession', sessionId2);
      expect(signalRService.getCurrentSessionId()).toBe(sessionId2);
    });

    it('should leave session successfully', async () => {
      // Arrange
      const sessionId = 'test-session-123';
      await signalRService.joinSession(sessionId);
      mockConnection.invoke.mockClear();

      // Act
      await signalRService.leaveSession(sessionId);

      // Assert
      expect(mockConnection.invoke).toHaveBeenCalledWith('LeaveSession', sessionId);
      expect(signalRService.getCurrentSessionId()).toBeNull();
    });

    it('should handle session join failure', async () => {
      // Arrange
      const sessionId = 'test-session-123';
      const error = new Error('Join failed');
      mockConnection.invoke.mockRejectedValue(error);

      // Act & Assert
      await expect(signalRService.joinSession(sessionId)).rejects.toThrow('Join failed');
    });
  });

  describe('Event Callbacks', () => {
    let mockCallbacks: any;

    beforeEach(() => {
      mockCallbacks = {
        onGenerationProgress: vi.fn(),
        onPreviewGenerated: vi.fn(),
        onGenerationError: vi.fn(),
        onValidationResult: vi.fn()
      };

      signalRService.setCallbacks(mockCallbacks);
    });

    it('should register event handlers on connection', () => {
      // Assert
      expect(mockConnection.on).toHaveBeenCalledWith('GenerationProgress', expect.any(Function));
      expect(mockConnection.on).toHaveBeenCalledWith('PreviewGenerated', expect.any(Function));
      expect(mockConnection.on).toHaveBeenCalledWith('GenerationError', expect.any(Function));
      expect(mockConnection.on).toHaveBeenCalledWith('ValidationResult', expect.any(Function));
      expect(mockConnection.on).toHaveBeenCalledWith('PreviewRequested', expect.any(Function));
    });

    it('should handle generation progress events', () => {
      // Arrange
      const sessionId = 'test-session';
      const progress = 50;
      const message = 'Generating terrain...';

      // Get the registered callback
      const progressCallback = mockConnection.on.mock.calls
        .find(call => call[0] === 'GenerationProgress')[1];

      // Act
      progressCallback(sessionId, progress, message);

      // Assert
      expect(mockCallbacks.onGenerationProgress).toHaveBeenCalledWith(sessionId, progress, message);
    });

    it('should handle preview generated events', () => {
      // Arrange
      const sessionId = 'test-session';
      const level: Level = {
        id: 'test-level',
        config: {} as any,
        terrain: {} as any,
        entities: [],
        metadata: {} as any
      };

      // Get the registered callback
      const previewCallback = mockConnection.on.mock.calls
        .find(call => call[0] === 'PreviewGenerated')[1];

      // Act
      previewCallback(sessionId, level);

      // Assert
      expect(mockCallbacks.onPreviewGenerated).toHaveBeenCalledWith(sessionId, level);
    });

    it('should handle generation error events', () => {
      // Arrange
      const sessionId = 'test-session';
      const error = 'Generation failed';

      // Get the registered callback
      const errorCallback = mockConnection.on.mock.calls
        .find(call => call[0] === 'GenerationError')[1];

      // Act
      errorCallback(sessionId, error);

      // Assert
      expect(mockCallbacks.onGenerationError).toHaveBeenCalledWith(sessionId, error);
    });

    it('should handle validation result events', () => {
      // Arrange
      const sessionId = 'test-session';
      const result: ValidationResult = {
        isValid: false,
        errors: [{ field: 'width', message: 'Invalid width', code: 'INVALID_WIDTH' }],
        warnings: []
      };

      // Get the registered callback
      const validationCallback = mockConnection.on.mock.calls
        .find(call => call[0] === 'ValidationResult')[1];

      // Act
      validationCallback(sessionId, result);

      // Assert
      expect(mockCallbacks.onValidationResult).toHaveBeenCalledWith(sessionId, result);
    });

    it('should clear callbacks', () => {
      // Act
      signalRService.clearCallbacks();

      // Simulate events after clearing
      const progressCallback = mockConnection.on.mock.calls
        .find(call => call[0] === 'GenerationProgress')[1];
      progressCallback('test', 50, 'test');

      // Assert
      expect(mockCallbacks.onGenerationProgress).not.toHaveBeenCalled();
    });
  });

  describe('Reconnection Handling', () => {
    beforeEach(async () => {
      mockConnection.state = 'Connected';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.invoke.mockResolvedValue(undefined);
      await signalRService.connect();
    });

    it('should rejoin session on reconnection', async () => {
      // Arrange
      const sessionId = 'test-session-123';
      await signalRService.joinSession(sessionId);

      // Get the reconnected callback
      const reconnectedCallback = mockConnection.onreconnected.mock.calls[0][0];
      mockConnection.invoke.mockClear();

      // Act
      reconnectedCallback();

      // Assert
      expect(mockConnection.invoke).toHaveBeenCalledWith('JoinSession', sessionId);
    });

    it('should not rejoin if no current session', () => {
      // Get the reconnected callback
      const reconnectedCallback = mockConnection.onreconnected.mock.calls[0][0];

      // Act
      reconnectedCallback();

      // Assert
      expect(mockConnection.invoke).not.toHaveBeenCalled();
    });
  });
});