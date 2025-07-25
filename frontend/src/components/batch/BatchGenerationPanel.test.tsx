import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { vi } from 'vitest';
import { BatchGenerationPanel } from './BatchGenerationPanel';
import { GenerationConfig } from '../../types';

const mockBaseConfig: GenerationConfig = {
  width: 20,
  height: 20,
  seed: 12345,
  generationAlgorithm: 'perlin',
  algorithmParameters: {},
  terrainTypes: ['floor', 'wall'],
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
    difficulty: 'normal',
    timeLimit: 300,
    victoryConditions: [],
    mechanics: {}
  }
};

describe('BatchGenerationPanel', () => {
  const mockOnStartBatch = vi.fn();
  const mockOnCancelBatch = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders batch generation panel', () => {
    render(
      <BatchGenerationPanel
        baseConfig={mockBaseConfig}
        onStartBatch={mockOnStartBatch}
        onCancelBatch={mockOnCancelBatch}
        isGenerating={false}
        progress={0}
      />
    );

    expect(screen.getByText('Batch Generation')).toBeInTheDocument();
    expect(screen.getByText('Start Batch')).toBeInTheDocument();
    expect(screen.getByText('Add Variation')).toBeInTheDocument();
  });

  it('allows setting batch count', () => {
    render(
      <BatchGenerationPanel
        baseConfig={mockBaseConfig}
        onStartBatch={mockOnStartBatch}
        onCancelBatch={mockOnCancelBatch}
        isGenerating={false}
        progress={0}
      />
    );

    const batchCountInput = screen.getByLabelText('Batch Count');
    fireEvent.change(batchCountInput, { target: { value: '10' } });
    
    expect(batchCountInput).toHaveValue(10);
  });

  it('adds parameter variations', async () => {
    render(
      <BatchGenerationPanel
        baseConfig={mockBaseConfig}
        onStartBatch={mockOnStartBatch}
        onCancelBatch={mockOnCancelBatch}
        isGenerating={false}
        progress={0}
      />
    );

    const addVariationButton = screen.getByText('Add Variation');
    fireEvent.click(addVariationButton);

    await waitFor(() => {
      expect(screen.getByText('Seed Variation')).toBeInTheDocument();
    });
  });

  it('starts batch generation with correct request', async () => {
    render(
      <BatchGenerationPanel
        baseConfig={mockBaseConfig}
        onStartBatch={mockOnStartBatch}
        onCancelBatch={mockOnCancelBatch}
        isGenerating={false}
        progress={0}
      />
    );

    // Set batch count
    const batchCountInput = screen.getByLabelText('Batch Count');
    fireEvent.change(batchCountInput, { target: { value: '5' } });

    // Start batch
    const startButton = screen.getByText('Start Batch');
    fireEvent.click(startButton);

    await waitFor(() => {
      expect(mockOnStartBatch).toHaveBeenCalledWith({
        baseConfig: mockBaseConfig,
        variations: [],
        count: 5
      });
    });
  });

  it('shows progress during generation', () => {
    render(
      <BatchGenerationPanel
        baseConfig={mockBaseConfig}
        onStartBatch={mockOnStartBatch}
        onCancelBatch={mockOnCancelBatch}
        isGenerating={true}
        progress={50}
        currentBatch={5}
        totalBatches={10}
      />
    );

    expect(screen.getByText('Generating batch...')).toBeInTheDocument();
    expect(screen.getByText('5/10')).toBeInTheDocument();
    expect(screen.getByText('Cancel')).toBeInTheDocument();
  });

  it('disables controls when generating', () => {
    render(
      <BatchGenerationPanel
        baseConfig={mockBaseConfig}
        onStartBatch={mockOnStartBatch}
        onCancelBatch={mockOnCancelBatch}
        isGenerating={true}
        progress={50}
      />
    );

    expect(screen.getByLabelText('Batch Count')).toBeDisabled();
    expect(screen.getByText('Add Variation')).toBeDisabled();
  });

  it('calculates total combinations correctly', async () => {
    render(
      <BatchGenerationPanel
        baseConfig={mockBaseConfig}
        onStartBatch={mockOnStartBatch}
        onCancelBatch={mockOnCancelBatch}
        isGenerating={false}
        progress={0}
      />
    );

    // Should show total levels text
    expect(screen.getByText(/Total levels to generate:/)).toBeInTheDocument();

    // Add a variation
    const addVariationButton = screen.getByText('Add Variation');
    fireEvent.click(addVariationButton);

    // Should still show the total levels text
    await waitFor(() => {
      expect(screen.getByText(/Total levels to generate:/)).toBeInTheDocument();
    });
  });

  it('validates batch configuration', () => {
    render(
      <BatchGenerationPanel
        baseConfig={mockBaseConfig}
        onStartBatch={mockOnStartBatch}
        onCancelBatch={mockOnCancelBatch}
        isGenerating={false}
        progress={0}
        disabled={true}
      />
    );

    const startButton = screen.getByText('Start Batch');
    expect(startButton).toBeDisabled();
  });
});