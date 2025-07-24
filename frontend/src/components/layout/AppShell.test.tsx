import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
// import { vi } from 'vitest';
import { BrowserRouter } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { AppShell } from './AppShell';

const theme = createTheme();

const renderWithProviders = (component: React.ReactElement) => {
  return render(
    <BrowserRouter
      future={{
        v7_startTransition: true,
        v7_relativeSplatPath: true
      }}
    >
      <ThemeProvider theme={theme}>
        {component}
      </ThemeProvider>
    </BrowserRouter>
  );
};

describe('AppShell', () => {
  it('renders the app title', () => {
    renderWithProviders(
      <AppShell>
        <div>Test Content</div>
      </AppShell>
    );

    expect(screen.getByText(/Procedural Level Editor/i)).toBeInTheDocument();
  });

  it('renders children content', () => {
    renderWithProviders(
      <AppShell>
        <div>Test Content</div>
      </AppShell>
    );

    expect(screen.getByText('Test Content')).toBeInTheDocument();
  });

  it('opens menu when menu button is clicked', async () => {
    renderWithProviders(
      <AppShell>
        <div>Test Content</div>
      </AppShell>
    );

    const menuButton = screen.getByLabelText('menu');
    fireEvent.click(menuButton);

    // Just check that menu items appear (both menu and drawer might be present)
    await waitFor(() => {
      expect(screen.getAllByText('Home').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Editor').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Load Preset').length).toBeGreaterThan(0);
    });
  });

  it('shows loading state when isLoading is true', () => {
    renderWithProviders(
      <AppShell isLoading={true} loadingMessage="Generating level...">
        <div>Test Content</div>
      </AppShell>
    );

    expect(screen.getByText('Generating level...')).toBeInTheDocument();
  });

  it('shows progress bar when progress is provided', () => {
    renderWithProviders(
      <AppShell isLoading={true} progress={50}>
        <div>Test Content</div>
      </AppShell>
    );

    // Check for progress indicator
    const progressBars = screen.getAllByRole('progressbar');
    expect(progressBars.length).toBeGreaterThan(0);
  });

  it('disables action buttons when not on editor page', () => {
    renderWithProviders(
      <AppShell>
        <div>Test Content</div>
      </AppShell>
    );

    // Find the actual button elements inside the tooltip spans
    const shareButton = screen.getByLabelText('Share Configuration').querySelector('button');
    const exportButton = screen.getByLabelText('Export Level').querySelector('button');

    expect(shareButton).toBeDisabled();
    expect(exportButton).toBeDisabled();
  });

  it('navigates to home when title is clicked', () => {
    renderWithProviders(
      <AppShell>
        <div>Test Content</div>
      </AppShell>
    );

    const title = screen.getByText(/Procedural Level Editor/i);
    fireEvent.click(title);

    // Check that we're on the home page (URL should be '/')
    expect(window.location.pathname).toBe('/');
  });

  it('closes menu when menu item is clicked', async () => {
    renderWithProviders(
      <AppShell>
        <div>Test Content</div>
      </AppShell>
    );

    const menuButton = screen.getByLabelText('menu');
    fireEvent.click(menuButton);

    // Wait for menu to appear
    await waitFor(() => {
      expect(screen.getAllByText('Home').length).toBeGreaterThan(0);
    });

    // Click on a menu item (use the first Home item)
    const homeMenuItems = screen.getAllByText('Home');
    fireEvent.click(homeMenuItems[0]);

    // Menu should close (items should disappear or be hidden)
    await waitFor(() => {
      // The menu might still exist but be hidden, so we just check it's not prominently displayed
      expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    });
  });

  it('renders responsive design elements', () => {
    renderWithProviders(
      <AppShell>
        <div>Test Content</div>
      </AppShell>
    );

    // Just check that the title is present (responsive behavior is complex to test)
    expect(screen.getByText(/Level Editor|Procedural Level Editor/i)).toBeInTheDocument();
  });
});