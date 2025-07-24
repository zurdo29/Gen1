// import React from 'react';
import { render, screen } from '@testing-library/react';
import { LoadingSpinner } from './LoadingSpinner';

describe('LoadingSpinner', () => {
  it('renders with default message', () => {
    render(<LoadingSpinner />);
    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('renders with custom message', () => {
    render(<LoadingSpinner message="Generating level..." />);
    expect(screen.getByText('Generating level...')).toBeInTheDocument();
  });

  it('renders circular progress by default', () => {
    render(<LoadingSpinner />);
    const progressBar = screen.getByRole('progressbar');
    expect(progressBar).toBeInTheDocument();
  });

  it('renders linear progress when variant is linear', () => {
    render(<LoadingSpinner variant="linear" />);
    const progressBar = screen.getByRole('progressbar');
    expect(progressBar).toBeInTheDocument();
  });

  it('shows progress percentage when progress is provided', () => {
    render(<LoadingSpinner progress={75} />);
    expect(screen.getByText('75%')).toBeInTheDocument();
  });

  it('shows progress percentage for linear variant', () => {
    render(<LoadingSpinner variant="linear" progress={50} />);
    expect(screen.getByText('50%')).toBeInTheDocument();
  });

  it('renders different sizes correctly', () => {
    const { rerender } = render(<LoadingSpinner size="small" />);
    let progressBar = screen.getByRole('progressbar');
    expect(progressBar).toBeInTheDocument();

    rerender(<LoadingSpinner size="large" />);
    progressBar = screen.getByRole('progressbar');
    expect(progressBar).toBeInTheDocument();
  });
});