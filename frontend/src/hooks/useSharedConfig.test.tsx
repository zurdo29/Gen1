import { renderHook, act } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { BrowserRouter } from 'react-router-dom';
import { useSharedConfig } from './useSharedConfig';
import { apiService } from '../services/api';
import { GenerationConfig, ShareResult } from '../types';

// Mock the API service
vi.mock('../services/api', () => ({
  apiService: {
    getSharedConfiguration: vi.fn(),
    createShareLink: vi.fn()
  }
}));

// Mock react-router-dom
const mockSetSearchParams = vi.fn();
const mockSearchParams = new URLSearchParams();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useSearchParams: () => [mockSearchParams, mockSetSearchParams],
    useNavigate: () => vi.fn()
  };
});

const mockConfig: GenerationConfig = {
  width: 50,
  height: 50,
  seed: 12345,
  generationAlgorithm: 'PerlinNoise',
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
};

const mockShareResult: ShareResult = {
  shareId: 'test-share-id',
  shareUrl: 'https://example.com/share/test-share-id',
  expiresAt: new Date('2024-12-31T23:59:59Z')
};

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <BrowserRouter>{children}</BrowserRouter>
);

describe('useSharedConfig', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockSearchParams.delete('share');
  });

  it('initializes with default values', () => {
    const { result } = renderHook(() => useSharedConfig(), { wrapper });

    expect(result.current.isPendingShared).toBe(false);
    expect(result.current.sharedConfig).toBe(null);
    expect(result.current.shareError).toBe(null);
  });

  it('loads shared configuration from URL parameter on mount', async () => {
    mockSearchParams.set('share', 'test-share-id');
    vi.mocked(apiService.getSharedConfiguration).mockResolvedValue(mockConfig);

    const { result } = renderHook(() => useSharedConfig(), { wrapper });

    expect(result.current.isPendingShared).toBe(true);

    await act(async () => {
      await new Promise(resolve => setTimeout(resolve, 0));
    });

    expect(apiService.getSharedConfiguration).toHaveBeenCalledWith('test-share-id');
    expect(result.current.sharedConfig).toEqual(mockConfig);
    expect(result.current.isPendingShared).toBe(false);
    expect(mockSetSearchParams).toHaveBeenCalled();
  });

  it('handles error when loading shared configuration fails', async () => {
    mockSearchParams.set('share', 'invalid-share-id');
    const errorMessage = 'Share not found';
    vi.mocked(apiService.getSharedConfiguration).mockRejectedValue({
      response: { data: { error: errorMessage } }
    });

    const { result } = renderHook(() => useSharedConfig(), { wrapper });

    await act(async () => {
      await new Promise(resolve => setTimeout(resolve, 0));
    });

    expect(result.current.shareError).toBe(errorMessage);
    expect(result.current.sharedConfig).toBe(null);
    expect(result.current.isPendingShared).toBe(false);
  });

  it('creates share link successfully', async () => {
    vi.mocked(apiService.createShareLink).mockResolvedValue(mockShareResult);

    const { result } = renderHook(() => useSharedConfig(), { wrapper });

    let shareResult: ShareResult | null = null;
    await act(async () => {
      shareResult = await result.current.createShareLink(mockConfig);
    });

    expect(apiService.createShareLink).toHaveBeenCalledWith(mockConfig);
    expect(shareResult).toEqual(mockShareResult);
    expect(result.current.shareError).toBe(null);
  });

  it('handles error when creating share link fails', async () => {
    const errorMessage = 'Failed to create share link';
    vi.mocked(apiService.createShareLink).mockRejectedValue({
      response: { data: { error: errorMessage } }
    });

    const { result } = renderHook(() => useSharedConfig(), { wrapper });

    let shareResult: ShareResult | null = null;
    await act(async () => {
      shareResult = await result.current.createShareLink(mockConfig);
    });

    expect(shareResult).toBe(null);
    expect(result.current.shareError).toBe(errorMessage);
  });

  it('imports configuration from share ID', async () => {
    vi.mocked(apiService.getSharedConfiguration).mockResolvedValue(mockConfig);

    const { result } = renderHook(() => useSharedConfig(), { wrapper });

    let importedConfig: GenerationConfig | null = null;
    await act(async () => {
      importedConfig = await result.current.importFromUrl('test-share-id');
    });

    expect(apiService.getSharedConfiguration).toHaveBeenCalledWith('test-share-id');
    expect(importedConfig).toEqual(mockConfig);
  });

  it('clears shared configuration', () => {
    const { result } = renderHook(() => useSharedConfig(), { wrapper });

    act(() => {
      result.current.clearSharedConfig();
    });

    expect(result.current.sharedConfig).toBe(null);
    expect(result.current.shareError).toBe(null);
  });
});