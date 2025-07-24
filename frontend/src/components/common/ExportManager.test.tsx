import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import ExportManager from './ExportManager';
import { apiService } from '../../services/api';
import { useNotifications } from '../../hooks/useNotifications';
import { Level, ExportFormat } from '../../types';

// Mock dependencies
vi.mock('../../services/api');
vi.mock('../../hooks/useNotifications');

const mockApiService = vi.mocked(apiService);
const mockUseNotifications = vi.mocked(useNotifications);

const mockLevel: Level = {
  id: 'test-level-1',
  config: {
    width: 50,
    height: 50,
    seed: 12345,
    generationAlgorithm: 'perlin',
    algorithmParameters: {},
    terrainTypes: ['grass', 'stone'],
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
  },
  terrain: {
    width: 50,
    height: 50,
    tiles: []
  },
  entities: [],
  metadata: {
    generatedAt: new Date(),
    generationTime: 1000,
    version: '1.0.0'
  }
};

const mockFormats: ExportFormat[] = [
  {
    id: 'json',
    name: 'JSON',
    description: 'Web-friendly JSON format',
    fileExtension: 'json',
    supportsCustomization: true
  },
  {
    id: 'unity',
    name: 'Unity',
    description: 'Unity-compatible format',
    fileExtension: 'unity',
    supportsCustomization: true
  },
  {
    id: 'csv',
    name: 'CSV',
    description: 'Spreadsheet format',
    fileExtension: 'csv',
    supportsCustomization: false
  }
];

const mockAddNotification = vi.fn();

describe('ExportManager', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    
    mockUseNotifications.mockReturnValue({
      notifications: [],
      addNotification: mockAddNotification,
      removeNotification: vi.fn(),
      clearNotifications: vi.fn()
    });

    mockApiService.getExportFormats.mockResolvedValue(mockFormats);
    mockApiService.validateConfiguration.mockResolvedValue({
      isValid: true,
      errors: [],
      warnings: []
    });

    // Mock URL.createObjectURL and related DOM methods
    global.URL.createObjectURL = vi.fn(() => 'mock-url');
    global.URL.revokeObjectURL = vi.fn();
    
    // Mock document methods
    const mockLink = {
      href: '',
      download: '',
      click: vi.fn()
    };
    vi.spyOn(document, 'createElement').mockReturnValue(mockLink as any);
    vi.spyOn(document.body, 'appendChild').mockImplementation(() => mockLink as any);
    vi.spyOn(document.body, 'removeChild').mockImplementation(() => mockLink as any);
  });

  it('renders export dialog when open', async () => {
    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
      />
    );

    expect(screen.getByText('Export Level')).toBeInTheDocument();
    expect(screen.getByLabelText('Export Format')).toBeInTheDocument();
    
    await waitFor(() => {
      expect(mockApiService.getExportFormats).toHaveBeenCalled();
    });
  });

  it('loads and displays available formats', async () => {
    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
      />
    );

    await waitFor(() => {
      expect(mockApiService.getExportFormats).toHaveBeenCalled();
    });

    // Click on format selector to open dropdown
    const formatSelect = screen.getByLabelText('Export Format');
    fireEvent.mouseDown(formatSelect);

    await waitFor(() => {
      expect(screen.getByText('JSON')).toBeInTheDocument();
      expect(screen.getByText('Unity')).toBeInTheDocument();
      expect(screen.getByText('CSV')).toBeInTheDocument();
    });
  });

  it('shows batch export mode for multiple levels', () => {
    const multipleLevels = [mockLevel, { ...mockLevel, id: 'test-level-2' }];
    
    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
        levels={multipleLevels}
      />
    );

    expect(screen.getByText('Export 2 Levels')).toBeInTheDocument();
  });

  it('validates export configuration when format changes', async () => {
    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
      />
    );

    await waitFor(() => {
      expect(mockApiService.getExportFormats).toHaveBeenCalled();
    });

    // Format should be auto-selected and validation should be called
    await waitFor(() => {
      expect(mockApiService.validateConfiguration).toHaveBeenCalled();
    });
  });

  it('shows validation errors', async () => {
    mockApiService.validateConfiguration.mockResolvedValue({
      isValid: false,
      errors: [{ field: 'level', message: 'Level data is invalid', code: 'INVALID_LEVEL' }],
      warnings: []
    });

    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Validation Errors:')).toBeInTheDocument();
      expect(screen.getByText('• Level data is invalid')).toBeInTheDocument();
    });
  });

  it('shows validation warnings', async () => {
    mockApiService.validateConfiguration.mockResolvedValue({
      isValid: true,
      errors: [],
      warnings: [{ field: 'entities', message: 'Level has no entities', suggestion: 'Add some entities' }]
    });

    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Warnings:')).toBeInTheDocument();
      expect(screen.getByText('• Level has no entities')).toBeInTheDocument();
    });
  });

  it('disables export button when validation fails', async () => {
    mockApiService.validateConfiguration.mockResolvedValue({
      isValid: false,
      errors: [{ field: 'level', message: 'Invalid level', code: 'INVALID' }],
      warnings: []
    });

    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
      />
    );

    await waitFor(() => {
      const exportButton = screen.getByRole('button', { name: /export/i });
      expect(exportButton).toBeDisabled();
    });
  });

  it('performs single level export', async () => {
    const mockBlob = new Blob(['test data'], { type: 'application/json' });
    mockApiService.exportLevel.mockResolvedValue(mockBlob);

    const user = userEvent.setup();
    
    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
      />
    );

    await waitFor(() => {
      expect(mockApiService.getExportFormats).toHaveBeenCalled();
    });

    const exportButton = screen.getByRole('button', { name: /^export$/i });
    await user.click(exportButton);

    await waitFor(() => {
      expect(mockApiService.exportLevel).toHaveBeenCalledWith(
        mockLevel,
        'json', // First format should be auto-selected
        expect.objectContaining({
          format: 'json',
          includeMetadata: true
        })
      );
    });

    expect(mockAddNotification).toHaveBeenCalledWith({
      type: 'success',
      title: 'Export Complete',
      message: expect.stringContaining('test-level-1.json')
    });
  });

  it('performs batch export', async () => {
    const multipleLevels = [mockLevel, { ...mockLevel, id: 'test-level-2' }];
    const mockJobId = 'job-123';
    
    mockApiService.exportBatch.mockResolvedValue(mockJobId);
    mockApiService.getJobStatus
      .mockResolvedValueOnce({
        jobId: mockJobId,
        status: 'running',
        progress: 50,
        errorMessage: undefined,
        result: undefined
      })
      .mockResolvedValueOnce({
        jobId: mockJobId,
        status: 'completed',
        progress: 100,
        errorMessage: undefined,
        result: undefined
      });

    // Mock fetch for batch download
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      blob: () => Promise.resolve(new Blob(['zip data']))
    });

    const user = userEvent.setup();
    
    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
        levels={multipleLevels}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Export 2 Levels')).toBeInTheDocument();
    });

    const exportButton = screen.getByRole('button', { name: /export batch/i });
    await user.click(exportButton);

    await waitFor(() => {
      expect(mockApiService.exportBatch).toHaveBeenCalledWith(
        multipleLevels,
        'json',
        expect.objectContaining({
          format: 'json',
          includeMetadata: true
        })
      );
    });

    // Wait for polling to complete
    await waitFor(() => {
      expect(mockAddNotification).toHaveBeenCalledWith({
        type: 'success',
        title: 'Batch Export Complete',
        message: '2 levels exported successfully'
      });
    }, { timeout: 3000 });
  });

  it('shows format customization options for supported formats', async () => {
    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
      />
    );

    await waitFor(() => {
      expect(mockApiService.getExportFormats).toHaveBeenCalled();
    });

    // Should show format settings accordion for JSON format (supports customization)
    expect(screen.getByText('Format Settings')).toBeInTheDocument();
  });

  it('handles export preview', async () => {
    const mockBlob = new Blob(['{"test": "preview"}'], { type: 'application/json' });
    mockApiService.exportLevel.mockResolvedValue(mockBlob);

    const user = userEvent.setup();
    
    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
      />
    );

    await waitFor(() => {
      expect(mockApiService.getExportFormats).toHaveBeenCalled();
    });

    const previewButton = screen.getByRole('button', { name: /preview/i });
    await user.click(previewButton);

    await waitFor(() => {
      expect(mockApiService.exportLevel).toHaveBeenCalledWith(
        mockLevel,
        'json',
        expect.objectContaining({
          customSettings: expect.objectContaining({
            preview: true
          })
        })
      );
    });

    // Preview dialog should open
    expect(screen.getByText('Export Preview')).toBeInTheDocument();
  });

  it('handles export errors gracefully', async () => {
    mockApiService.exportLevel.mockRejectedValue(new Error('Export failed'));

    const user = userEvent.setup();
    
    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
      />
    );

    await waitFor(() => {
      expect(mockApiService.getExportFormats).toHaveBeenCalled();
    });

    const exportButton = screen.getByRole('button', { name: /^export$/i });
    await user.click(exportButton);

    await waitFor(() => {
      expect(mockAddNotification).toHaveBeenCalledWith({
        type: 'error',
        title: 'Export Failed',
        message: 'Export failed'
      });
    });
  });

  it('includes metadata option', async () => {
    const user = userEvent.setup();
    
    render(
      <ExportManager
        open={true}
        onClose={vi.fn()}
        level={mockLevel}
      />
    );

    const metadataCheckbox = screen.getByLabelText(/include metadata/i);
    expect(metadataCheckbox).toBeChecked(); // Should be checked by default

    await user.click(metadataCheckbox);
    expect(metadataCheckbox).not.toBeChecked();
  });
});