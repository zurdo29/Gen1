// import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { PresetManager } from './PresetManager';
import { createDefaultConfig } from '../../utils/configDefaults';

// Mock the API service
vi.mock('../../services/api', () => ({
  default: {
    getPresets: vi.fn(() => Promise.resolve([])),
    savePreset: vi.fn(() => Promise.resolve({ 
      id: '1', 
      name: 'Test Preset', 
      description: 'Test', 
      config: createDefaultConfig(), 
      createdAt: new Date() 
    })),
    createShareLink: vi.fn(() => Promise.resolve({ 
      shareId: '123', 
      shareUrl: 'http://example.com/share/123', 
      expiresAt: new Date() 
    }))
  }
}));

// Mock the notifications hook
vi.mock('../../hooks/useNotifications', () => ({
  useNotifications: () => ({
    showSuccess: vi.fn(),
    showError: vi.fn(),
    showInfo: vi.fn()
  })
}));

describe('PresetManager', () => {
  const defaultConfig = createDefaultConfig();
  const mockOnLoadPreset = vi.fn();
  const mockOnClose = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders preset manager dialog when open', () => {
    render(
      <PresetManager
        open={true}
        onClose={mockOnClose}
        currentConfig={defaultConfig}
        onLoadPreset={mockOnLoadPreset}
      />
    );

    expect(screen.getByText('Configuration Presets')).toBeInTheDocument();
    expect(screen.getByText('Built-in Presets')).toBeInTheDocument();
    expect(screen.getByText('Your Saved Presets')).toBeInTheDocument();
  });

  it('displays built-in presets', () => {
    render(
      <PresetManager
        open={true}
        onClose={mockOnClose}
        currentConfig={defaultConfig}
        onLoadPreset={mockOnLoadPreset}
      />
    );

    // Check for built-in preset names
    expect(screen.getByText('Dungeon')).toBeInTheDocument();
    expect(screen.getByText('Maze')).toBeInTheDocument();
    expect(screen.getByText('Survival')).toBeInTheDocument();
    expect(screen.getByText('Platformer')).toBeInTheDocument();
  });

  it('opens save preset dialog when save button is clicked', async () => {
    render(
      <PresetManager
        open={true}
        onClose={mockOnClose}
        currentConfig={defaultConfig}
        onLoadPreset={mockOnLoadPreset}
      />
    );

    const saveButton = screen.getByText('Save Current');
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(screen.getByText('Save Configuration Preset')).toBeInTheDocument();
    });
  });

  it('calls onLoadPreset when a built-in preset is loaded', () => {
    render(
      <PresetManager
        open={true}
        onClose={mockOnClose}
        currentConfig={defaultConfig}
        onLoadPreset={mockOnLoadPreset}
      />
    );

    const loadButtons = screen.getAllByText('Load');
    fireEvent.click(loadButtons[0]); // Click first load button

    expect(mockOnLoadPreset).toHaveBeenCalled();
  });

  it('does not render when closed', () => {
    render(
      <PresetManager
        open={false}
        onClose={mockOnClose}
        currentConfig={defaultConfig}
        onLoadPreset={mockOnLoadPreset}
      />
    );

    expect(screen.queryByText('Configuration Presets')).not.toBeInTheDocument();
  });
});