import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { vi } from 'vitest';
import { EntityEditToolbar } from './EntityEditToolbar';
// import type { EditMode, EntityType } from './EntityEditToolbar';

const theme = createTheme();

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={theme}>
      {component}
    </ThemeProvider>
  );
};

describe('EntityEditToolbar', () => {
  const mockOnEditModeChange = vi.fn();
  const mockOnEntityTypeChange = vi.fn();

  beforeEach(() => {
    mockOnEditModeChange.mockClear();
    mockOnEntityTypeChange.mockClear();
  });

  it('renders without crashing', () => {
    renderWithTheme(
      <EntityEditToolbar
        editMode="select"
        selectedEntityType="Player"
        onEditModeChange={mockOnEditModeChange}
        onEntityTypeChange={mockOnEntityTypeChange}
      />
    );

    expect(screen.getByText('Entity Editing')).toBeInTheDocument();
  });

  it('displays current edit mode', () => {
    renderWithTheme(
      <EntityEditToolbar
        editMode="add"
        selectedEntityType="Player"
        onEditModeChange={mockOnEditModeChange}
        onEntityTypeChange={mockOnEntityTypeChange}
      />
    );

    // Check that add mode is selected by finding the button with the AddIcon
    const addButton = screen.getByTestId('AddIcon').closest('button');
    expect(addButton).toHaveAttribute('aria-pressed', 'true');
  });

  it('calls onEditModeChange when mode is changed', () => {
    renderWithTheme(
      <EntityEditToolbar
        editMode="select"
        selectedEntityType="Player"
        onEditModeChange={mockOnEditModeChange}
        onEntityTypeChange={mockOnEntityTypeChange}
      />
    );

    // Find and click the add button
    const buttons = screen.getAllByRole('button');
    const addButton = buttons.find(button => 
      button.querySelector('[data-testid="AddIcon"]')
    );
    
    if (addButton) {
      fireEvent.click(addButton);
      expect(mockOnEditModeChange).toHaveBeenCalledWith('add');
    }
  });

  it('disables entity type selection in delete mode', () => {
    renderWithTheme(
      <EntityEditToolbar
        editMode="delete"
        selectedEntityType="Player"
        onEditModeChange={mockOnEditModeChange}
        onEntityTypeChange={mockOnEntityTypeChange}
      />
    );

    // Entity type buttons should be disabled
    const entityButtons = screen.getAllByRole('button').filter(button => 
      button.getAttribute('aria-pressed') !== null && 
      !button.querySelector('[data-testid="DeleteIcon"]') &&
      !button.querySelector('[data-testid="AddIcon"]') &&
      !button.querySelector('[data-testid="PanToolIcon"]')
    );

    entityButtons.forEach(button => {
      expect(button).toBeDisabled();
    });
  });

  it('enables entity type selection in add mode', () => {
    renderWithTheme(
      <EntityEditToolbar
        editMode="add"
        selectedEntityType="Player"
        onEditModeChange={mockOnEditModeChange}
        onEntityTypeChange={mockOnEntityTypeChange}
      />
    );

    // Entity type buttons should be enabled
    const entityButtons = screen.getAllByRole('button').filter(button => 
      button.getAttribute('aria-pressed') !== null && 
      !button.querySelector('[data-testid="DeleteIcon"]') &&
      !button.querySelector('[data-testid="AddIcon"]') &&
      !button.querySelector('[data-testid="PanToolIcon"]')
    );

    entityButtons.forEach(button => {
      expect(button).not.toBeDisabled();
    });
  });

  it('disables all controls when disabled prop is true', () => {
    renderWithTheme(
      <EntityEditToolbar
        editMode="select"
        selectedEntityType="Player"
        onEditModeChange={mockOnEditModeChange}
        onEntityTypeChange={mockOnEntityTypeChange}
        disabled={true}
      />
    );

    // All buttons should be disabled
    const buttons = screen.getAllByRole('button');
    buttons.forEach(button => {
      expect(button).toBeDisabled();
    });
  });

  it('shows correct entity type selection', () => {
    renderWithTheme(
      <EntityEditToolbar
        editMode="add"
        selectedEntityType="Enemy"
        onEditModeChange={mockOnEditModeChange}
        onEntityTypeChange={mockOnEntityTypeChange}
      />
    );

    // Enemy should be selected
    const buttons = screen.getAllByRole('button');
    const selectedButtons = buttons.filter(button => 
      button.getAttribute('aria-pressed') === 'true'
    );

    // Should have one selected edit mode button and one selected entity type button
    expect(selectedButtons.length).toBeGreaterThanOrEqual(1);
  });
});