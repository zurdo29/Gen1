import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import {
  OfflineIndicator,
  OfflineFallback,
  NetworkErrorBoundary,
  LoadingWithOfflineFallback,
  OfflineCapableButton
} from './OfflineFallback';

// Mock the service worker hooks
vi.mock('../../services/serviceWorker', () => ({
  useOfflineStatus: vi.fn(),
  useNetworkQuality: vi.fn()
}));

// Mock the localStorage services
vi.mock('../../services/localStorage', () => ({
  configStorage: {
    getStats: vi.fn(() => ({ entryCount: 5 }))
  },
  levelStorage: {
    getStats: vi.fn(() => ({ entryCount: 10 }))
  },
  userStorage: {
    getStats: vi.fn(() => ({ entryCount: 3 }))
  }
}));

const theme = createTheme();

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={theme}>
      {component}
    </ThemeProvider>
  );
};

describe('OfflineIndicator', () => {
  const { useOfflineStatus } = await import('../../services/serviceWorker');
  const { useNetworkQuality } = await import('../../services/serviceWorker');
  const mockUseOfflineStatus = vi.mocked(useOfflineStatus);
  const mockUseNetworkQuality = vi.mocked(useNetworkQuality);

  beforeEach(() => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: true,
      isOffline: false,
      wasOffline: false
    });
    
    mockUseNetworkQuality.mockReturnValue({
      networkQuality: 'fast',
      connectionType: '4g',
      isFast: true,
      isSlow: false,
      isOffline: false
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('should not render when online and no details requested', () => {
    renderWithTheme(<OfflineIndicator />);
    
    expect(screen.queryByText(/offline/i)).not.toBeInTheDocument();
  });

  it('should render offline banner when offline', () => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: false,
      isOffline: true,
      wasOffline: false
    });

    renderWithTheme(<OfflineIndicator />);
    
    expect(screen.getByText(/you're offline/i)).toBeInTheDocument();
    expect(screen.getByText(/some features may be limited/i)).toBeInTheDocument();
  });

  it('should show reconnected notification', async () => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: true,
      isOffline: false,
      wasOffline: true
    });

    renderWithTheme(<OfflineIndicator />);
    
    await waitFor(() => {
      expect(screen.getByText(/back online/i)).toBeInTheDocument();
    });
  });

  it('should show network quality when details enabled', () => {
    renderWithTheme(<OfflineIndicator showDetails />);
    
    expect(screen.getByText(/4g - fast/i)).toBeInTheDocument();
  });

  it('should show warning for slow network', () => {
    mockUseNetworkQuality.mockReturnValue({
      networkQuality: 'slow',
      connectionType: '3g',
      isFast: false,
      isSlow: true,
      isOffline: false
    });

    renderWithTheme(<OfflineIndicator showDetails />);
    
    const chip = screen.getByText(/3g - slow/i);
    expect(chip).toBeInTheDocument();
  });
});

describe('OfflineFallback', () => {
  const { useOfflineStatus: useOfflineStatusImport } = await import('../../services/serviceWorker');
  const mockUseOfflineStatus = vi.mocked(useOfflineStatusImport);

  beforeEach(() => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: true,
      isOffline: false,
      wasOffline: false
    });
  });

  it('should render offline message when offline', () => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: false,
      isOffline: true,
      wasOffline: false
    });

    renderWithTheme(<OfflineFallback />);
    
    expect(screen.getByText(/you're offline/i)).toBeInTheDocument();
    expect(screen.getByText(/don't worry/i)).toBeInTheDocument();
  });

  it('should render connection failed message when online but error', () => {
    const error = new Error('Network request failed');
    
    renderWithTheme(<OfflineFallback error={error} />);
    
    expect(screen.getByText(/connection failed/i)).toBeInTheDocument();
    expect(screen.getByText(/network request failed/i)).toBeInTheDocument();
  });

  it('should show cached data stats when requested', () => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: false,
      isOffline: true,
      wasOffline: false
    });

    renderWithTheme(<OfflineFallback showCachedData />);
    
    expect(screen.getByText(/5 saved configurations/i)).toBeInTheDocument();
    expect(screen.getByText(/10 cached levels/i)).toBeInTheDocument();
    expect(screen.getByText(/3 user preferences/i)).toBeInTheDocument();
  });

  it('should call onRetry when retry button clicked', () => {
    const onRetry = vi.fn();
    
    renderWithTheme(<OfflineFallback onRetry={onRetry} />);
    
    const retryButton = screen.getByText(/try again/i);
    fireEvent.click(retryButton);
    
    expect(onRetry).toHaveBeenCalled();
  });

  it('should disable retry button when offline', () => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: false,
      isOffline: true,
      wasOffline: false
    });

    const onRetry = vi.fn();
    
    renderWithTheme(<OfflineFallback onRetry={onRetry} />);
    
    const retryButton = screen.getByText(/try again/i);
    expect(retryButton).toBeDisabled();
  });

  it('should render custom children', () => {
    renderWithTheme(
      <OfflineFallback>
        <div>Custom content</div>
      </OfflineFallback>
    );
    
    expect(screen.getByText(/custom content/i)).toBeInTheDocument();
  });
});

describe('NetworkErrorBoundary', () => {
  const ThrowError = ({ shouldThrow }: { shouldThrow: boolean }) => {
    if (shouldThrow) {
      throw new Error('Network fetch failed');
    }
    return <div>No error</div>;
  };

  it('should render children when no error', () => {
    renderWithTheme(
      <NetworkErrorBoundary>
        <ThrowError shouldThrow={false} />
      </NetworkErrorBoundary>
    );
    
    expect(screen.getByText(/no error/i)).toBeInTheDocument();
  });

  it('should catch and render network errors', () => {
    renderWithTheme(
      <NetworkErrorBoundary>
        <ThrowError shouldThrow={true} />
      </NetworkErrorBoundary>
    );
    
    expect(screen.getByText(/connection failed/i)).toBeInTheDocument();
  });

  it('should call onError callback when error occurs', () => {
    const onError = vi.fn();
    
    renderWithTheme(
      <NetworkErrorBoundary onError={onError}>
        <ThrowError shouldThrow={true} />
      </NetworkErrorBoundary>
    );
    
    expect(onError).toHaveBeenCalledWith(expect.any(Error));
  });

  it('should use custom fallback component', () => {
    const CustomFallback = ({ error, retry }: { error: Error; retry: () => void }) => (
      <div>
        <span>Custom error: {error.message}</span>
        <button onClick={retry}>Custom retry</button>
      </div>
    );

    renderWithTheme(
      <NetworkErrorBoundary fallback={CustomFallback}>
        <ThrowError shouldThrow={true} />
      </NetworkErrorBoundary>
    );
    
    expect(screen.getByText(/custom error/i)).toBeInTheDocument();
    expect(screen.getByText(/custom retry/i)).toBeInTheDocument();
  });

  it('should retry and recover from error', () => {
    const { rerender } = renderWithTheme(
      <NetworkErrorBoundary>
        <ThrowError shouldThrow={true} />
      </NetworkErrorBoundary>
    );
    
    expect(screen.getByText(/connection failed/i)).toBeInTheDocument();
    
    const retryButton = screen.getByText(/try again/i);
    fireEvent.click(retryButton);
    
    // Re-render with no error
    rerender(
      <ThemeProvider theme={theme}>
        <NetworkErrorBoundary>
          <ThrowError shouldThrow={false} />
        </NetworkErrorBoundary>
      </ThemeProvider>
    );
    
    expect(screen.getByText(/no error/i)).toBeInTheDocument();
  });
});

describe('LoadingWithOfflineFallback', () => {
  const { useOfflineStatus: useOfflineStatusImport2 } = await import('../../services/serviceWorker');
  const mockUseOfflineStatus = vi.mocked(useOfflineStatusImport2);

  beforeEach(() => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: true,
      isOffline: false,
      wasOffline: false
    });
  });

  it('should show loading state', () => {
    renderWithTheme(
      <LoadingWithOfflineFallback loading={true}>
        <div>Content</div>
      </LoadingWithOfflineFallback>
    );
    
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
    expect(screen.queryByText(/content/i)).not.toBeInTheDocument();
  });

  it('should show offline loading message when offline', () => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: false,
      isOffline: true,
      wasOffline: false
    });

    renderWithTheme(
      <LoadingWithOfflineFallback loading={true}>
        <div>Content</div>
      </LoadingWithOfflineFallback>
    );
    
    expect(screen.getByText(/loading from cache/i)).toBeInTheDocument();
  });

  it('should show error fallback when error occurs', () => {
    const error = new Error('Failed to load');
    
    renderWithTheme(
      <LoadingWithOfflineFallback loading={false} error={error}>
        <div>Content</div>
      </LoadingWithOfflineFallback>
    );
    
    expect(screen.getByText(/connection failed/i)).toBeInTheDocument();
  });

  it('should show empty state when no data', () => {
    renderWithTheme(
      <LoadingWithOfflineFallback loading={false} data={null}>
        <div>Content</div>
      </LoadingWithOfflineFallback>
    );
    
    expect(screen.getByText(/no data available/i)).toBeInTheDocument();
  });

  it('should show empty state for empty array', () => {
    renderWithTheme(
      <LoadingWithOfflineFallback loading={false} data={[]}>
        <div>Content</div>
      </LoadingWithOfflineFallback>
    );
    
    expect(screen.getByText(/no data available/i)).toBeInTheDocument();
  });

  it('should render children when data is available', () => {
    renderWithTheme(
      <LoadingWithOfflineFallback loading={false} data={['item1', 'item2']}>
        <div>Content loaded</div>
      </LoadingWithOfflineFallback>
    );
    
    expect(screen.getByText(/content loaded/i)).toBeInTheDocument();
  });

  it('should use custom loading component', () => {
    const CustomLoading = () => <div>Custom loading...</div>;
    
    renderWithTheme(
      <LoadingWithOfflineFallback 
        loading={true} 
        loadingComponent={<CustomLoading />}
      >
        <div>Content</div>
      </LoadingWithOfflineFallback>
    );
    
    expect(screen.getByText(/custom loading/i)).toBeInTheDocument();
  });

  it('should use custom empty component', () => {
    const CustomEmpty = () => <div>Custom empty state</div>;
    
    renderWithTheme(
      <LoadingWithOfflineFallback 
        loading={false} 
        data={null}
        emptyComponent={<CustomEmpty />}
      >
        <div>Content</div>
      </LoadingWithOfflineFallback>
    );
    
    expect(screen.getByText(/custom empty state/i)).toBeInTheDocument();
  });
});

describe('OfflineCapableButton', () => {
  const { useOfflineStatus: useOfflineStatusImport3 } = await import('../../services/serviceWorker');
  const mockUseOfflineStatus = vi.mocked(useOfflineStatusImport3);

  beforeEach(() => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: true,
      isOffline: false,
      wasOffline: false
    });
  });

  it('should work normally when online', () => {
    const onClick = vi.fn();
    
    renderWithTheme(
      <OfflineCapableButton onClick={onClick}>
        Click me
      </OfflineCapableButton>
    );
    
    const button = screen.getByText(/click me/i);
    fireEvent.click(button);
    
    expect(onClick).toHaveBeenCalled();
  });

  it('should work offline when not onlineOnly', () => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: false,
      isOffline: true,
      wasOffline: false
    });

    const onClick = vi.fn();
    
    renderWithTheme(
      <OfflineCapableButton onClick={onClick}>
        Click me
      </OfflineCapableButton>
    );
    
    const button = screen.getByText(/click me/i);
    expect(button).not.toBeDisabled();
    
    fireEvent.click(button);
    expect(onClick).toHaveBeenCalled();
  });

  it('should be disabled offline when onlineOnly', () => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: false,
      isOffline: true,
      wasOffline: false
    });

    const onClick = vi.fn();
    
    renderWithTheme(
      <OfflineCapableButton onClick={onClick} onlineOnly>
        Click me
      </OfflineCapableButton>
    );
    
    const button = screen.getByText(/click me/i);
    expect(button).toBeDisabled();
  });

  it('should show offline message when clicked offline and onlineOnly', async () => {
    mockUseOfflineStatus.mockReturnValue({
      isOnline: false,
      isOffline: true,
      wasOffline: false
    });

    const onClick = vi.fn();
    
    renderWithTheme(
      <OfflineCapableButton 
        onClick={onClick} 
        onlineOnly
        offlineMessage="Custom offline message"
      >
        Click me
      </OfflineCapableButton>
    );
    
    const button = screen.getByText(/click me/i);
    fireEvent.click(button);
    
    expect(onClick).not.toHaveBeenCalled();
    
    await waitFor(() => {
      expect(screen.getByText(/custom offline message/i)).toBeInTheDocument();
    });
  });

  it('should pass through other button props', () => {
    renderWithTheme(
      <OfflineCapableButton 
        onClick={() => {
          // Mock click handler
        }} 
        variant="outlined"
        color="secondary"
        data-testid="custom-button"
      >
        Click me
      </OfflineCapableButton>
    );
    
    const button = screen.getByTestId('custom-button');
    expect(button).toBeInTheDocument();
  });
});