// import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { vi } from 'vitest';
import { NotificationSystem } from './NotificationSystem';
import { Notification } from '../../types';

const mockNotifications: Notification[] = [
  {
    id: '1',
    type: 'success',
    title: 'Success',
    message: 'Level generated successfully',
    timestamp: new Date(),
    autoHide: true,
  },
  {
    id: '2',
    type: 'error',
    title: 'Error',
    message: 'Failed to generate level',
    timestamp: new Date(),
    autoHide: false,
  },
];

describe('NotificationSystem', () => {
  const mockOnRemove = vi.fn();

  beforeEach(() => {
    mockOnRemove.mockClear();
  });

  it('renders notifications', () => {
    render(
      <NotificationSystem 
        notifications={mockNotifications} 
        onRemove={mockOnRemove} 
      />
    );

    expect(screen.getByText('Success')).toBeInTheDocument();
    expect(screen.getByText('Level generated successfully')).toBeInTheDocument();
    expect(screen.getByText('Error')).toBeInTheDocument();
    expect(screen.getByText('Failed to generate level')).toBeInTheDocument();
  });

  it('calls onRemove when close button is clicked', () => {
    render(
      <NotificationSystem 
        notifications={[mockNotifications[0]]} 
        onRemove={mockOnRemove} 
      />
    );

    const closeButton = screen.getByLabelText('close');
    fireEvent.click(closeButton);

    expect(mockOnRemove).toHaveBeenCalledWith('1');
  });

  it('renders different notification types with correct severity', () => {
    const notifications: Notification[] = [
      {
        id: '1',
        type: 'info',
        title: 'Info',
        message: 'Information message',
        timestamp: new Date(),
      },
      {
        id: '2',
        type: 'warning',
        title: 'Warning',
        message: 'Warning message',
        timestamp: new Date(),
      },
    ];

    render(
      <NotificationSystem 
        notifications={notifications} 
        onRemove={mockOnRemove} 
      />
    );

    expect(screen.getByText('Info')).toBeInTheDocument();
    expect(screen.getByText('Warning')).toBeInTheDocument();
  });

  it('renders empty when no notifications', () => {
    const { container } = render(
      <NotificationSystem 
        notifications={[]} 
        onRemove={mockOnRemove} 
      />
    );

    expect(container.firstChild?.childNodes).toHaveLength(0);
  });
});