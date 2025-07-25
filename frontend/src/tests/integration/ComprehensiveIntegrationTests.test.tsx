import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';

// Import your main components
import App from '../../App';
import { LevelPreview } from '../../components/LevelPreview';
import { ParameterConfiguration } from '../../components/ParameterConfiguration';
import { _ExportManager } from '../../components/ExportManager';
import { BatchGeneration } from '../../components/BatchGeneration';

// Mock API responses
const mockApiResponses = {
  generateLevel: {
    level: {
      width: 50,
      height: 50,
      tiles: Array(2500).fill(null).map((_, i) => ({
        x: i % 50,
        y: Math.floor(i / 50),
        type: 'grass',
        elevation: Math.random()
      })),
      entities: [
        { id: '1', type: 'tree', x: 10, y: 10 },
        { id: '2', type: 'rock', x: 25, y: 30 },
        { id: '3', type: 'spawn', x: 5, y: 5 }
      ]
    },
    previewData: {
      thumbnail: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=='
    },
    metadata: {
      generationTime: 1250,
      seed: 12345
    }
  },
  validateConfig: {
    isValid: true,
    errors: [],
    warnings: []
  },
  exportFormats: [
    { id: 'JSON', name: 'JSON', description: 'Standard JSON format', fileExtension: 'json' },
    { id: 'Unity', name: 'Unity Prefab', description: 'Unity-compatible format', fileExtension: 'prefab' },
    { id: 'CSV', name: 'CSV', description: 'Comma-separated values', fileExtension: 'csv' },
    { id: 'Image', name: 'PNG Image', description: 'Visual representation', fileExtension: 'png' }
  ],
  presets: [
    {
      id: '1',
      name: 'Forest Adventure',
      description: 'Dense forest with moderate difficulty',
      config: {
        width: 40,
        height: 40,
        terrainType: 'PerlinNoise',
        entityDensity: 0.3,
        theme: 'Forest'
      }
    }
  ]
};

// Mock fetch globally
global.fetch = vi.fn();

// Mock Canvas API for level preview
const mockCanvas = {
  getContext: vi.fn(() => ({
    fillRect: vi.fn(),
    clearRect: vi.fn(),
    getImageData: vi.fn(),
    putImageData: vi.fn(),
    createImageData: vi.fn(),
    setTransform: vi.fn(),
    drawImage: vi.fn(),
    save: vi.fn(),
    restore: vi.fn(),
    scale: vi.fn(),
    rotate: vi.fn(),
    translate: vi.fn(),
    transform: vi.fn(),
    fillText: vi.fn(),
    measureText: vi.fn(() => ({ width: 0 })),
  })),
  toDataURL: vi.fn(),
  addEventListener: vi.fn(),
  removeEventListener: vi.fn(),
};

// Mock HTMLCanvasElement
Object.defineProperty(HTMLCanvasElement.prototype, 'getContext', {
  value: mockCanvas.getContext,
});

// Mock WebSocket for real-time updates
class MockWebSocket {
  constructor(url: string) {
    this.url = url;
    this.readyState = WebSocket.CONNECTING;
    setTimeout(() => {
      this.readyState = WebSocket.OPEN;
      this.onopen?.({ target: this } as Event);
    }, 100);
  }

  url: string;
  readyState: number;
  onopen?: (event: Event) => void;
  onmessage?: (event: MessageEvent) => void;
  onclose?: (event: CloseEvent) => void;
  onerror?: (event: Event) => void;

  send(_data: string) {
    // Simulate receiving progress updates
    setTimeout(() => {
      this.onmessage?.({
        data: JSON.stringify({ type: 'progress', progress: 25 })
      } as MessageEvent);
    }, 200);
    
    setTimeout(() => {
      this.onmessage?.({
        data: JSON.stringify({ type: 'progress', progress: 75 })
      } as MessageEvent);
    }, 400);
    
    setTimeout(() => {
      this.onmessage?.({
        data: JSON.stringify({ type: 'complete', result: mockApiResponses.generateLevel })
      } as MessageEvent);
    }, 600);
  }

  close() {
    this.readyState = WebSocket.CLOSED;
    this.onclose?.({ code: 1000, reason: 'Normal closure' } as CloseEvent);
  }
}

global.WebSocket = MockWebSocket as any;

// Test wrapper component
const TestWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        {children}
      </BrowserRouter>
    </QueryClientProvider>
  );
};

describe('Comprehensive Integration Tests - Web Level Editor', () => {
  let user: ReturnType<typeof userEvent.setup>;

  beforeEach(() => {
    user = userEvent.setup();
    
    // Setup fetch mocks
    (global.fetch as any).mockImplementation((url: string, options?: RequestInit) => {
      const method = options?.method || 'GET';
      _body = options?.body ? JSON.parse(options.body as string) : null;

      // Mock API endpoints
      if (url.includes('/api/generation/generate') && method === 'POST') {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockApiResponses.generateLevel),
          headers: new Headers({ 'content-type': 'application/json' }),
        });
      }

      if (url.includes('/api/configuration/validate') && method === 'POST') {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockApiResponses.validateConfig),
        });
      }

      if (url.includes('/api/export/formats')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockApiResponses.exportFormats),
        });
      }

      if (url.includes('/api/configuration/presets')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockApiResponses.presets),
        });
      }

      if (url.includes('/api/export/level') && method === 'POST') {
        return Promise.resolve({
          ok: true,
          blob: () => Promise.resolve(new Blob(['mock export data'], { type: 'application/json' })),
          headers: new Headers({ 'content-type': 'application/json' }),
        });
      }

      if (url.includes('/api/configuration/share') && method === 'POST') {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({
            shareId: 'test-share-id',
            shareUrl: 'https://example.com/share/test-share-id',
            expiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString()
          }),
        });
      }

      return Promise.reject(new Error(`Unhandled request: ${method} ${url}`));
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('Complete Workflow: Configure → Generate → Preview → Edit → Export → Share', async () => {
    await act(async () => {
      render(
        <TestWrapper>
          <App />
        </TestWrapper>
      );
    });

    // Step 1: Configure parameters
    const widthInput = screen.getByLabelText(/width/i);
    const heightInput = screen.getByLabelText(/height/i);
    const terrainSelect = screen.getByLabelText(/terrain type/i);
    const entityDensitySlider = screen.getByLabelText(/entity density/i);

    await act(async () => {
      await user.clear(widthInput);
      await user.type(widthInput, '50');
      await user.clear(heightInput);
      await user.type(heightInput, '50');
      await user.selectOptions(terrainSelect, 'PerlinNoise');
      fireEvent.change(entityDensitySlider, { target: { value: '0.3' } });
    });

    // Step 2: Generate level
    const generateButton = screen.getByRole('button', { name: /generate level/i });
    
    await act(async () => {
      await user.click(generateButton);
    });

    // Wait for generation to complete
    await waitFor(() => {
      expect(screen.getByText(/generation complete/i)).toBeInTheDocument();
    }, { timeout: 3000 });

    // Step 3: Verify preview is displayed
    const canvas = screen.getByRole('img', { name: /level preview/i }) || 
                  screen.getByTestId('level-canvas');
    expect(canvas).toBeInTheDocument();

    // Step 4: Test interactive editing
    const editModeButton = screen.getByRole('button', { name: /edit mode/i });
    await act(async () => {
      await user.click(editModeButton);
    });

    // Simulate clicking on canvas to edit terrain
    await act(async () => {
      fireEvent.click(canvas, { clientX: 100, clientY: 100 });
    });

    // Verify edit controls appear
    expect(screen.getByText(/terrain type/i)).toBeInTheDocument();

    // Step 5: Test export functionality
    const exportButton = screen.getByRole('button', { name: /export/i });
    await act(async () => {
      await user.click(exportButton);
    });

    // Select export format
    const formatSelect = screen.getByLabelText(/export format/i);
    await act(async () => {
      await user.selectOptions(formatSelect, 'JSON');
    });

    const downloadButton = screen.getByRole('button', { name: /download/i });
    await act(async () => {
      await user.click(downloadButton);
    });

    // Verify export was triggered
    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/export/level'),
        expect.objectContaining({ method: 'POST' })
      );
    });

    // Step 6: Test sharing functionality
    const shareButton = screen.getByRole('button', { name: /share/i });
    await act(async () => {
      await user.click(shareButton);
    });

    await waitFor(() => {
      expect(screen.getByText(/share link created/i)).toBeInTheDocument();
      expect(screen.getByDisplayValue(/https:\/\/example\.com\/share/)).toBeInTheDocument();
    });
  });

  it('Real-time Parameter Updates with Debouncing', async () => {
    await act(async () => {
      render(
        <TestWrapper>
          <ParameterConfiguration />
        </TestWrapper>
      );
    });

    const entityDensitySlider = screen.getByLabelText(/entity density/i);
    
    // Rapidly change the slider value
    await act(async () => {
      fireEvent.change(entityDensitySlider, { target: { value: '0.1' } });
      fireEvent.change(entityDensitySlider, { target: { value: '0.2' } });
      fireEvent.change(entityDensitySlider, { target: { value: '0.3' } });
      fireEvent.change(entityDensitySlider, { target: { value: '0.4' } });
    });

    // Wait for debounced update
    await waitFor(() => {
      expect(screen.getByText(/updating preview/i)).toBeInTheDocument();
    }, { timeout: 1000 });

    // Verify only one API call was made (debounced)
    await waitFor(() => {
      const generateCalls = (global.fetch as any).mock.calls.filter(
        (call: any) => call[0].includes('/api/generation/generate')
      );
      expect(generateCalls).toHaveLength(1);
    });
  });

  it('Batch Generation with Progress Tracking', async () => {
    await act(async () => {
      render(
        <TestWrapper>
          <BatchGeneration />
        </TestWrapper>
      );
    });

    // Configure batch generation
    const countInput = screen.getByLabelText(/number of variations/i);
    await act(async () => {
      await user.clear(countInput);
      await user.type(countInput, '5');
    });

    // Add variation parameters
    const addVariationButton = screen.getByRole('button', { name: /add variation/i });
    await act(async () => {
      await user.click(addVariationButton);
    });

    const parameterSelect = screen.getByLabelText(/parameter/i);
    await act(async () => {
      await user.selectOptions(parameterSelect, 'seed');
    });

    // Start batch generation
    const startBatchButton = screen.getByRole('button', { name: /start batch generation/i });
    await act(async () => {
      await user.click(startBatchButton);
    });

    // Verify progress tracking
    await waitFor(() => {
      expect(screen.getByText(/generating batch/i)).toBeInTheDocument();
      expect(screen.getByRole('progressbar')).toBeInTheDocument();
    });

    // Wait for completion
    await waitFor(() => {
      expect(screen.getByText(/batch generation complete/i)).toBeInTheDocument();
    }, { timeout: 5000 });

    // Verify results grid
    const resultThumbnails = screen.getAllByTestId('batch-result-thumbnail');
    expect(resultThumbnails).toHaveLength(5);
  });

  it('Canvas Interaction: Zoom, Pan, and Edit', async () => {
    await act(async () => {
      render(
        <TestWrapper>
          <LevelPreview 
            level={mockApiResponses.generateLevel.level}
            isEditable={true}
          />
        </TestWrapper>
      );
    });

    const canvas = screen.getByTestId('level-canvas');

    // Test zoom functionality
    await act(async () => {
      fireEvent.wheel(canvas, { deltaY: -100 }); // Zoom in
    });

    // Verify zoom controls update
    const zoomLevel = screen.getByTestId('zoom-level');
    expect(zoomLevel).toHaveTextContent(/110%|120%|150%/); // Some zoom increase

    // Test pan functionality
    await act(async () => {
      fireEvent.mouseDown(canvas, { clientX: 100, clientY: 100 });
      fireEvent.mouseMove(canvas, { clientX: 150, clientY: 150 });
      fireEvent.mouseUp(canvas);
    });

    // Test tile editing
    await act(async () => {
      fireEvent.click(canvas, { clientX: 200, clientY: 200 });
    });

    // Verify edit menu appears
    expect(screen.getByText(/change terrain/i)).toBeInTheDocument();

    // Select new terrain type
    const terrainOption = screen.getByText(/water/i);
    await act(async () => {
      await user.click(terrainOption);
    });

    // Verify undo/redo functionality
    const undoButton = screen.getByRole('button', { name: /undo/i });
    expect(undoButton).toBeInTheDocument();
    
    await act(async () => {
      await user.click(undoButton);
    });
  });

  it('Error Handling and Recovery', async () => {
    // Mock API failure
    (global.fetch as any).mockImplementationOnce(() => 
      Promise.resolve({
        ok: false,
        status: 500,
        json: () => Promise.resolve({ error: 'Internal server error' }),
      })
    );

    await act(async () => {
      render(
        <TestWrapper>
          <App />
        </TestWrapper>
      );
    });

    const generateButton = screen.getByRole('button', { name: /generate level/i });
    
    await act(async () => {
      await user.click(generateButton);
    });

    // Verify error message is displayed
    await waitFor(() => {
      expect(screen.getByText(/generation failed/i)).toBeInTheDocument();
      expect(screen.getByText(/internal server error/i)).toBeInTheDocument();
    });

    // Verify retry button appears
    const retryButton = screen.getByRole('button', { name: /retry/i });
    expect(retryButton).toBeInTheDocument();

    // Reset fetch mock to success
    (global.fetch as any).mockImplementation((url: string, _options?: RequestInit) => {
      if (url.includes('/api/generation/generate')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockApiResponses.generateLevel),
        });
      }
      return Promise.reject(new Error(`Unhandled request: ${url}`));
    });

    // Test retry functionality
    await act(async () => {
      await user.click(retryButton);
    });

    await waitFor(() => {
      expect(screen.getByText(/generation complete/i)).toBeInTheDocument();
    });
  });

  it('Performance: Virtual Scrolling for Large Levels', async () => {
    const largeLevelData = {
      ...mockApiResponses.generateLevel.level,
      width: 200,
      height: 200,
      tiles: Array(40000).fill(null).map((_, i) => ({
        x: i % 200,
        y: Math.floor(i / 200),
        type: 'grass',
        elevation: Math.random()
      }))
    };

    await act(async () => {
      render(
        <TestWrapper>
          <LevelPreview level={largeLevelData} />
        </TestWrapper>
      );
    });

    const canvas = screen.getByTestId('level-canvas');
    
    // Verify canvas is rendered
    expect(canvas).toBeInTheDocument();

    // Test scrolling performance
    const startTime = performance.now();
    
    await act(async () => {
      // Simulate rapid scrolling
      for (let i = 0; i < 10; i++) {
        fireEvent.scroll(canvas, { target: { scrollTop: i * 100 } });
      }
    });

    const endTime = performance.now();
    const scrollTime = endTime - startTime;

    // Verify scrolling is performant (should be under 100ms for virtual scrolling)
    expect(scrollTime).toBeLessThan(100);
  });

  it('Accessibility: Keyboard Navigation and Screen Reader Support', async () => {
    await act(async () => {
      render(
        <TestWrapper>
          <App />
        </TestWrapper>
      );
    });

    // Test keyboard navigation
    const firstInput = screen.getByLabelText(/width/i);
    firstInput.focus();

    // Tab through form elements
    await act(async () => {
      await user.tab();
    });
    expect(screen.getByLabelText(/height/i)).toHaveFocus();

    await act(async () => {
      await user.tab();
    });
    expect(screen.getByLabelText(/terrain type/i)).toHaveFocus();

    // Test ARIA labels and descriptions
    const generateButton = screen.getByRole('button', { name: /generate level/i });
    expect(generateButton).toHaveAttribute('aria-describedby');

    // Test keyboard shortcuts
    await act(async () => {
      fireEvent.keyDown(document, { key: 'g', ctrlKey: true });
    });

    // Verify generate shortcut works
    await waitFor(() => {
      expect(screen.getByText(/generating/i)).toBeInTheDocument();
    });
  });

  it('Offline Support and Service Worker', async () => {
    // Mock service worker
    const mockServiceWorker = {
      register: vi.fn(() => Promise.resolve({ scope: '/' })),
      ready: Promise.resolve({
        active: { postMessage: vi.fn() }
      })
    };

    Object.defineProperty(navigator, 'serviceWorker', {
      value: mockServiceWorker,
      writable: true
    });

    // Mock offline state
    Object.defineProperty(navigator, 'onLine', {
      value: false,
      writable: true
    });

    await act(async () => {
      render(
        <TestWrapper>
          <App />
        </TestWrapper>
      );
    });

    // Verify offline indicator
    expect(screen.getByText(/offline mode/i)).toBeInTheDocument();

    // Test cached data access
    const cachedPresets = screen.getByText(/cached presets/i);
    expect(cachedPresets).toBeInTheDocument();

    // Simulate going back online
    Object.defineProperty(navigator, 'onLine', {
      value: true,
      writable: true
    });

    fireEvent(window, new Event('online'));

    await waitFor(() => {
      expect(screen.queryByText(/offline mode/i)).not.toBeInTheDocument();
    });
  });
});