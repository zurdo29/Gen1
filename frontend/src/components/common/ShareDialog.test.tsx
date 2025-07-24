import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { vi } from 'vitest';
import ShareDialog from './ShareDialog';
import { apiService } from '../../services/api';
import { GenerationConfig, ShareResult } from '../../types';

// Mock the API service
vi.mock('../../services/api', () => ({
  apiService: {
    createShareLink: vi.fn()
  }
}));

// Mock clipboard API
Object.assign(navigator, {
  clipboard: {
    writeText: vi.fn()
  }
});

const mockConfig: GenerationConfig = {
  width: 50,
  height: 50,
  seed: 12345,
  generationAlgorithm: 'perlin',
  algorithmParameters: {},
  terrainTypes: ['ground', 'wall'],
  entities: [],
  visualTheme: {
    themeName: 'default',
    colorPalette: {},
    tileSprites: {},
    entitySprites: {},
    effectSettings: {}
  },
  gameplay: {
    playerSpeed: 5.0,
    playerHealth: 100,
    difficulty: 'normal',
    timeLimit: 0,
    victoryConditions: ['reach_exit'],
    mechanics: {}
  }
};

const mockShareResult: ShareResult = {
  shareId: 'abc123',
  shareUrl: 'http://localhost:5000/api/configuration/share/abc123',
  expiresAt: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000),
  description: 'Level configuration (50x50, perlin)',
  qrCodeDataUrl: 'data:image/png;base64,mockqrcode',
  previewImageUrl: 'data:image/png;base64,mockpreview',
  thumbnailUrl: 'data:image/png;base64,mockthumbnail',
  metadata: {}
};

describe('ShareDialog', () => {
  const defaultProps = {
    open: true,
    onClose: vi.fn(),
    config: mockConfig
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders dialog when open', () => {
    render(<ShareDialog {...defaultProps} />);
    
    expect(screen.getByText('Share Level Configuration')).toBeInTheDocument();
  });

  it('does not render dialog when closed', () => {
    render(<ShareDialog {...defaultProps} open={false} />);
    
    expect(screen.queryByText('Share Level Configuration')).not.toBeInTheDocument();
  });

  it('generates share link on open', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockResolvedValue(mockShareResult);

    render(<ShareDialog {...defaultProps} />);

    await waitFor(() => {
      expect(mockCreateShareLink).toHaveBeenCalledWith(mockConfig);
    });
  });

  it('displays loading state while generating share link', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockImplementation(() => new Promise(resolve => setTimeout(() => resolve(mockShareResult), 100)));

    render(<ShareDialog {...defaultProps} />);

    expect(screen.getByText('Generating share link...')).toBeInTheDocument();
    
    await waitFor(() => {
      expect(screen.queryByText('Generating share link...')).not.toBeInTheDocument();
    });
  });

  it('displays error when share link generation fails', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockRejectedValue(new Error('Failed to create share link'));

    render(<ShareDialog {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByText('Failed to create share link')).toBeInTheDocument();
    });
  });

  it('displays share URL when generated successfully', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockResolvedValue(mockShareResult);

    render(<ShareDialog {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue(mockShareResult.shareUrl)).toBeInTheDocument();
    });
  });

  it('displays configuration summary', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockResolvedValue(mockShareResult);

    render(<ShareDialog {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByText('Configuration: 50×50 level using perlin')).toBeInTheDocument();
    });
  });

  it('displays expiry date', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockResolvedValue(mockShareResult);

    render(<ShareDialog {...defaultProps} />);

    await waitFor(() => {
      const expiryText = screen.getByText(/Expires:/);
      expect(expiryText).toBeInTheDocument();
    });
  });

  it('copies share URL to clipboard when copy button is clicked', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockResolvedValue(mockShareResult);
    const mockWriteText = vi.mocked(navigator.clipboard.writeText);

    render(<ShareDialog {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue(mockShareResult.shareUrl)).toBeInTheDocument();
    });

    const copyButton = screen.getByRole('button', { name: /copy to clipboard/i });
    fireEvent.click(copyButton);

    expect(mockWriteText).toHaveBeenCalledWith(mockShareResult.shareUrl);
  });

  it('displays social media sharing buttons', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockResolvedValue(mockShareResult);

    render(<ShareDialog {...defaultProps} />);

    // Switch to social media tab
    const socialTab = screen.getByText('Social Media');
    fireEvent.click(socialTab);

    await waitFor(() => {
      expect(screen.getByText('Facebook')).toBeInTheDocument();
      expect(screen.getByText('Twitter')).toBeInTheDocument();
      expect(screen.getByText('LinkedIn')).toBeInTheDocument();
      expect(screen.getByText('WhatsApp')).toBeInTheDocument();
    });
  });

  it('displays QR code when available', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockResolvedValue(mockShareResult);

    render(<ShareDialog {...defaultProps} />);

    // Switch to QR code tab
    const qrTab = screen.getByText('QR Code');
    fireEvent.click(qrTab);

    await waitFor(() => {
      const qrImage = screen.getByAltText('QR Code for share link');
      expect(qrImage).toBeInTheDocument();
      expect(qrImage).toHaveAttribute('src', mockShareResult.qrCodeDataUrl);
    });
  });

  it('displays preview image when available', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockResolvedValue(mockShareResult);

    render(<ShareDialog {...defaultProps} />);

    // Switch to social media tab
    const socialTab = screen.getByText('Social Media');
    fireEvent.click(socialTab);

    await waitFor(() => {
      const previewImage = screen.getByAltText('Social media preview');
      expect(previewImage).toBeInTheDocument();
      expect(previewImage).toHaveAttribute('src', mockShareResult.previewImageUrl);
    });
  });

  it('opens social media sharing windows', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockResolvedValue(mockShareResult);
    const mockOpen = vi.spyOn(window, 'open').mockImplementation(() => null);

    render(<ShareDialog {...defaultProps} />);

    // Switch to social media tab
    const socialTab = screen.getByText('Social Media');
    fireEvent.click(socialTab);

    await waitFor(() => {
      expect(screen.getByText('Facebook')).toBeInTheDocument();
    });

    const facebookButton = screen.getByText('Facebook');
    fireEvent.click(facebookButton);

    expect(mockOpen).toHaveBeenCalledWith(
      expect.stringContaining('facebook.com'),
      '_blank',
      'width=600,height=400'
    );

    mockOpen.mockRestore();
  });

  it('calls onClose when close button is clicked', () => {
    const mockOnClose = vi.fn();
    render(<ShareDialog {...defaultProps} onClose={mockOnClose} />);

    const closeButton = screen.getByRole('button', { name: /close/i });
    fireEvent.click(closeButton);

    expect(mockOnClose).toHaveBeenCalled();
  });

  it('handles tab switching correctly', async () => {
    const mockCreateShareLink = vi.mocked(apiService.createShareLink);
    mockCreateShareLink.mockResolvedValue(mockShareResult);

    render(<ShareDialog {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue(mockShareResult.shareUrl)).toBeInTheDocument();
    });

    // Initially on Share Link tab
    expect(screen.getByText('Share URL')).toBeInTheDocument();

    // Switch to Social Media tab
    const socialTab = screen.getByText('Social Media');
    fireEvent.click(socialTab);
    expect(screen.getByText('Share on Social Media')).toBeInTheDocument();

    // Switch to QR Code tab
    const qrTab = screen.getByText('QR Code');
    fireEvent.click(qrTab);
    expect(screen.getByText('QR Code for Mobile Sharing')).toBeInTheDocument();
  });
});